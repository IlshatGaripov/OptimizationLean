using System;
using System.Collections.Generic;

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
        public FitnessScore? SortCriteria { get; set; }

        /// <summary>
        /// Walk forward optimization settings object
        /// </summary>
        public WalkForwardConfiguration WalkForwardConfiguration { get; set; }

        /// <summary>
        /// Event fired as one stage of optimization on in-sample and
        /// verification on out-of-sample data is completed
        /// </summary>
        public event EventHandler OneEvaluationStepCompleted;

        /// <summary>
        /// Full results dicrionary for best in sample chromosome backtest result 
        /// </summary>
        public IList<Dictionary<string, decimal>> BestInSampleFullResults = new List<Dictionary<string, decimal>>();

        /// <summary>
        /// Full result dictionary for backtest on out-of-sample data
        /// </summary>
        public IList<Dictionary<string, decimal>> ValidationFullResults = new List<Dictionary<string, decimal>>();

        /// <summary>
        /// Starts and continues the process till the end
        /// </summary>
        public void Start()
        {
            // All property values must be assigned before calling the method ->
            if (!StartDate.HasValue ||
                !EndDate.HasValue ||
                !SortCriteria.HasValue ||
                WalkForwardConfiguration == null
                )
            {
                throw new ApplicationException("Walk Forward Manager public properties must be initialized before Start()");
            }

            // Validate walk forward configuration values ->
            if (!WalkForwardConfiguration.InSamplePeriod.HasValue ||
                !WalkForwardConfiguration.Step.HasValue ||
                !WalkForwardConfiguration.Anchored.HasValue)
            {
                throw new ApplicationException("Walk forward configuration must have InSamplePeriod, Step, Anchored values assigned");
            }

            // Make sure that the specified date limits and configuration make sense ->
            var addDays = WalkForwardConfiguration.InSamplePeriod.Value + WalkForwardConfiguration.Step.Value;
            if (StartDate.Value.AddDays(addDays) < EndDate.Value)
                throw new ArgumentOutOfRangeException(
                    $"The range between {StartDate.Value} and {EndDate.Value} is short for walk forward configuration values specified");

            // Init datetime variables will be used in first iteration ->
            var insampleStartDate = StartDate.Value;
            var insampleEndDate = insampleStartDate.AddDays(WalkForwardConfiguration.InSamplePeriod.Value - 1);
            var validationStartDate = insampleEndDate.AddDays(1);
            var validationEndDate = validationStartDate.AddDays(WalkForwardConfiguration.Step.Value - 1);
            var step = WalkForwardConfiguration.Step.Value;

            // While insampleEndDate is less then fixed optimization EndDate we may crank one more iteration -> 
            while (insampleEndDate < EndDate.Value)
            {
                // create new optimum finder which will use optimization scheme filed in optimization.json ->
                var optimumFinder = new AlgorithmOptimumFinder(insampleStartDate, insampleEndDate, SortCriteria.Value);

                // Start and wat to complete ->
                optimumFinder.Start();

                // Once completed retrive best chromosome and cast it to the base class ->
                var bestChromosome = optimumFinder.GenAlgorithm.BestChromosome;
                var bestChromosomeBase = (Chromosome)optimumFinder.GenAlgorithm.BestChromosome;

                // Then save full result to inner list ->
                BestInSampleFullResults.Add(bestChromosomeBase.FullResults);

                // Using best parameters execute a validation experiment on local machine using best chromosome ->
                var fitness = new OptimizerFitness(validationStartDate, validationEndDate);
                fitness.Evaluate(bestChromosome);

                // Add full results to dictionary ->
                ValidationFullResults.Add(bestChromosomeBase.FullResults);

                // Raise an event informing a single step of evaluation is over ->
                OnOneEvaluationStepCompleted();

                // Increment the dates ->
                // If anchored do not increment insample Start Date ->
                if (!WalkForwardConfiguration.Anchored.Value)
                    insampleStartDate = insampleStartDate.AddDays(step);

                insampleEndDate = insampleEndDate.AddDays(step);
                validationStartDate = validationStartDate.AddDays(step);
                validationEndDate = validationEndDate.AddDays(step);
            }
        }

        /// <summary>
        /// OneEvaluationStepCompleted wrapper
        /// </summary>
        protected virtual void OnOneEvaluationStepCompleted()
        {
            OneEvaluationStepCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
