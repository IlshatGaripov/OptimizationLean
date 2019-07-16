using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <summary>
    /// An object that orchestrates the process of walk forward optimization
    /// </summary>
    public class WalkForwardOptimizationManager: IOptimizerManager
    {
        /// <summary>
        /// Optimization start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Fitness Score to sort the parameters obtained by optimization
        /// </summary>
        public FitnessScore FitnessScore { get; set; }

        /// <summary>
        /// Walk forward optimization settings object
        /// </summary>
        public WalkForwardConfiguration WalkForwardConfiguration { get; set; }

        /// <summary>
        /// Event fired as one stage of optimization on in-sample and
        /// verification on out-of-sample data is completed
        /// </summary>
        public event EventHandler<WalkForwardValidationEventArgs> ValidationCompleted;

        /// <summary>
        /// Starts and continues the process till the end
        /// </summary>
        public void Start()
        {
            // Make sure all properties are correctly assigned ->
            СheckСonsistencyOfTheInputParameters();

            // Init datetime variables will be used in first iteration ->
            var insampleStartDate = StartDate.Value;
            var insampleEndDate = insampleStartDate.AddDays(WalkForwardConfiguration.InSamplePeriod.Value - 1);
            var validationStartDate = insampleEndDate.AddDays(1);
            var validationEndDate = insampleEndDate.AddDays(WalkForwardConfiguration.Step.Value);
            var step = WalkForwardConfiguration.Step.Value;
            
            // While insampleEndDate is less then fixed optimization EndDate we may crank one more iteration -> 
            while (insampleEndDate < EndDate.Value)
            {
                // Find optimum solutions ->
                var optimumFinder = new AlgorithmOptimumFinder(insampleStartDate, insampleEndDate, FitnessScore);
                optimumFinder.Start();

                // Once completed retrieve N best results ->
                var n = 10;
                var take = optimumFinder.ProfitableChromosomes.Count > n
                    ? n
                    : optimumFinder.ProfitableChromosomes.Count;
                var bestResults = optimumFinder.ProfitableChromosomes.Take(take).ToList();

                // Validate the chosen best results ->
                var validationTasks = new List<Task>();
                var startDate = validationStartDate;
                var endDate = validationEndDate;

                Program.Logger.Trace($"Taking {take} best solutions and launching the validation tasks");
                Program.Logger.Trace($"Validation period: {startDate:M/d/yy} to {endDate:M/d/yy}");

                // For each good chromosome add the task to collection ->
                foreach (var c in bestResults)
                {
                    validationTasks.Add(       
                        Task.Run( () => 
                            ValidateOutOfSample(c.FitnessResult, FitnessScore, startDate, endDate)));
                }

                // Wait for all tasks to complete before to continue ->
                Task.WaitAll(validationTasks.ToArray());


                // Increment the variables and step to the next iteration.
                // If anchored do not increment insample Start Date ->
                if (!WalkForwardConfiguration.Anchored.Value)
                {
                    insampleStartDate = insampleStartDate.AddDays(step);
                }
                insampleEndDate = insampleEndDate.AddDays(step);
                validationStartDate = validationStartDate.AddDays(step);
                validationEndDate = validationEndDate.AddDays(step);
            }
        }

        /// <summary>
        /// Method called in the backgound to validate the chromosome on out-of-sample data
        /// </summary>
        protected void ValidateOutOfSample(
            FitnessResult insampeResult,
            FitnessScore fitScore,
            DateTime validationStartDate,
            DateTime validationEndDate)
        {
            // Create a deep copy ->
            var copy = (Chromosome)insampeResult.Chromosome.CreateNew();

            // Run a backtest.
            // Do not apply fitness filtering to validation results - let it expose a true picture ->
            IFitness fitness;
            if (Program.Config.TaskExecutionMode == TaskExecutionMode.Azure)
            {
                fitness = new AzureFitness(validationStartDate, validationEndDate, fitScore, false);
                copy.Fitness = fitness.Evaluate(copy);
            }
            else
            {
                fitness = new OptimizerFitness(validationStartDate, validationEndDate, fitScore, false);
                copy.Fitness = fitness.Evaluate(copy);
            }

            // Raise an event ->
            ValidationCompleted?.Invoke(this, e: 
                new WalkForwardValidationEventArgs(insampeResult, copy.FitnessResult));
        }

        /// <summary>
        /// Validates the consistensy of all optimiation parameters and data.
        /// </summary>
        private void СheckСonsistencyOfTheInputParameters()
        {
            // All property values must be assigned before calling the method ->
            if (!StartDate.HasValue ||
                !EndDate.HasValue ||
                FitnessScore == 0 ||
                WalkForwardConfiguration == null)
            {
                throw new ApplicationException("СheckСonsistencyOfTheInputParameters(): WalkForwardManager public properties are not fully initialized");
            }

            // Check for required config values presence ->
            if (!WalkForwardConfiguration.InSamplePeriod.HasValue ||
                !WalkForwardConfiguration.Step.HasValue ||
                !WalkForwardConfiguration.Anchored.HasValue)
            {
                throw new ApplicationException("СheckСonsistencyOfTheInputParameters: InSamplePeriod, Step, Anchored values must be assigned");
            }

            // Make sure that number of days between beginning and end is enough for at least one iteration.
            // We substract 1 as Lean includes includes boundary date values in the experiment span ->
            var minDaysBtwStartEnd = WalkForwardConfiguration.InSamplePeriod.Value + WalkForwardConfiguration.Step.Value - 1;
            if (StartDate.Value.AddDays(minDaysBtwStartEnd) > EndDate.Value)
            {
                throw new ArgumentOutOfRangeException(
                    $"The range between {StartDate.Value} and {EndDate.Value} " +
                    "is short for walk forward configuration values specified");
            }
        }

    }
}
