using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optimization.Base;
using Optimization.Genetic;

namespace Optimization.Launcher
{
    /// <summary>
    /// An object that orchestrates the process of walk forward optimization
    /// </summary>
    public class WalkForwardOptimizationManager: IOptimizerManager
    {
        private readonly bool _filterEnabled;

        /// <summary>
        /// Optimization start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime EndDate { get; set; }

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
        /// Initializes a new instance of the <see cref="WalkForwardOptimizationManager"/> class
        /// </summary>
        /// <param name="start">Algorithm start date</param>
        /// <param name="end">Algorithm end date</param>
        /// <param name="fitScore">Argument of <see cref="FitnessScore"/> type. Fintess function to rank the backtest results</param>
        /// <param name="filterEnabled">Indicates whether to apply fitness filter to backtest results</param>
        public WalkForwardOptimizationManager(DateTime start, DateTime end, FitnessScore fitScore, bool filterEnabled)
        {
            StartDate = start;
            EndDate = end;
            FitnessScore = fitScore;
            _filterEnabled = filterEnabled;
        }

        /// <summary>
        /// Starts and continues the process till the end
        /// </summary>
        public void Start()
        {
            // Check for required config values presence
            if (!WalkForwardConfiguration.InSamplePeriod.HasValue ||
                !WalkForwardConfiguration.Step.HasValue ||
                !WalkForwardConfiguration.Anchored.HasValue)
            {
                throw new ApplicationException("WalkForwardOptimizationManager.Start(): InSamplePeriod, Step, Anchored values must be assigned");
            }

            // Make sure that provided time frame is long enough.
            // We substract 1 as Lean includes the date boundaries into the backtest period
            var minDaysBtwStartEndRequired = WalkForwardConfiguration.InSamplePeriod.Value + WalkForwardConfiguration.Step.Value - 1;

            if (StartDate.AddDays(minDaysBtwStartEndRequired) > EndDate)
            {
                throw new ArgumentOutOfRangeException($"WalkForwardOptimizationManager.Start(): Provided time period [{StartDate} - {EndDate}] is short.");
            }


            // Init datetime variables will be used in first iteration
            var insampleStartDate = StartDate;
            var insampleEndDate = insampleStartDate.AddDays(WalkForwardConfiguration.InSamplePeriod.Value - 1);
            var validationStartDate = insampleEndDate.AddDays(1);
            var validationEndDate = insampleEndDate.AddDays(WalkForwardConfiguration.Step.Value);
            var step = WalkForwardConfiguration.Step.Value;
            
            // While insampleEndDate is less then fixed optimization EndDate we may crank one more iteration
            while (insampleEndDate < EndDate)
            {
                // Find optimum solutions
                var optimumFinder = new AlgorithmOptimumFinder(insampleStartDate, insampleEndDate, FitnessScore, _filterEnabled);
                optimumFinder.Start();

                // Once completed retrieve N best results
                var n = 10;
                var take = optimumFinder.ProfitableChromosomes.Count > n
                    ? n
                    : optimumFinder.ProfitableChromosomes.Count;
                var bestResults = optimumFinder.ProfitableChromosomes.Take(take).ToList();

                // Validate the chosen best results
                var validationTasks = new List<Task>();
                var startDate = validationStartDate;
                var endDate = validationEndDate;

                Shared.Logger.Trace($"Taking {take} best solutions and launching the validation tasks");
                Shared.Logger.Trace($"Validation period: {startDate:M/d/yy} to {endDate:M/d/yy}");

                // For each good chromosome add the task to collection
                foreach (var c in bestResults)
                {
                    validationTasks.Add(       
                        Task.Run( () => 
                            ValidateOutOfSample(c.FitnessResult, FitnessScore, startDate, endDate)));
                }

                // Wait for all tasks to complete before to continue
                Task.WaitAll(validationTasks.ToArray());


                // Increment the variables and step to the next iteration.
                // If anchored do not increment insample Start Date
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
            // Create a deep copy
            var copy = (Chromosome)insampeResult.Chromosome.CreateNew();

            // Run a backtest.
            // Do not apply fitness filtering to validation results - let it expose a true picture ->
            IFitness fitness;
            if (Shared.Config.TaskExecutionMode == TaskExecutionMode.Azure)
            {
                fitness = new AzureFitness(validationStartDate, validationEndDate, fitScore, false);
                copy.Fitness = fitness.Evaluate(copy);
            }
            else
            {
                fitness = new OptimizerFitness(validationStartDate, validationEndDate, fitScore, false);
                copy.Fitness = fitness.Evaluate(copy);
            }

            // Raise an event
            ValidationCompleted?.Invoke(this, e: 
                new WalkForwardValidationEventArgs(insampeResult, copy.FitnessResult));
        }
    }
}
