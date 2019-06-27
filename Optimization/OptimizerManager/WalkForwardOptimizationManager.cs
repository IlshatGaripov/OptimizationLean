using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GeneticSharp.Domain.Chromosomes;
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
        public FitnessScore? SortCriteria { get; set; }

        /// <summary>
        /// Walk forward optimization settings object
        /// </summary>
        public WalkForwardConfiguration WalkForwardConfiguration { get; set; }

        /// <summary>
        /// Event fired as one stage of optimization on in-sample and
        /// verification on out-of-sample data is completed
        /// </summary>
        public event EventHandler<WalkForwardEventArgs> WfoStepCompleted;

        /// <summary>
        /// Full results dicrionary for best in sample chromosome backtest result 
        /// </summary>
        private readonly IList<Dictionary<string, decimal>> _inSampleBestResultsList = new List<Dictionary<string, decimal>>();

        /// <summary>
        /// Full result dictionary for backtest on out-of-sample data
        /// </summary>
        private readonly IList<Dictionary<string, decimal>> _validationResultsList = new List<Dictionary<string, decimal>>();

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

            // Make sure that number of days between beginning and end is enough for at least one iteration ->
            // We substract 1 as Lean includes both start and end dates into backtest 
            var minDaysBtStartEnd = WalkForwardConfiguration.InSamplePeriod.Value + WalkForwardConfiguration.Step.Value - 1;
            if (StartDate.Value.AddDays(minDaysBtStartEnd) > EndDate.Value)
            {
                throw new ArgumentOutOfRangeException(
                    $"The range between {StartDate.Value} and {EndDate.Value} " +
                    $"is short for walk forward configuration values specified");
            }
                

            // Init datetime variables will be used in first iteration ->
            var insampleStartDate = StartDate.Value;
            var insampleEndDate = insampleStartDate.AddDays(WalkForwardConfiguration.InSamplePeriod.Value - 1);
            var validationStartDate = insampleEndDate.AddDays(1);
            var validationEndDate = insampleEndDate.AddDays(WalkForwardConfiguration.Step.Value);

            var step = WalkForwardConfiguration.Step.Value;

            // The list to store validation tasks, we'll perform validation in the background for not to block the calculations ->
            var validationTasks = new List<Task>();

            // While insampleEndDate is less then fixed optimization EndDate we may crank one more iteration -> 
            while (insampleEndDate < EndDate.Value)
            {
                // Create new optimum finder (wrapper for GA) ->
                var optimumFinder = new AlgorithmOptimumFinder(insampleStartDate, insampleEndDate, SortCriteria.Value);

                // Start an optimization and wait to complete ->
                optimumFinder.Start();

                // Once completed retrive best chromosome and cast it to base class object ->
                var bestChromosomeBase = (Chromosome)optimumFinder.GenAlgorithm.BestChromosome;

                // Save best results to the list ->
                var bestInSampleResults = bestChromosomeBase.FitnessResult.FullResults;
                _inSampleBestResultsList.Add(bestInSampleResults);

                // Create validation task ->
                var insmpStart = insampleStartDate;
                var insmpEnd = insampleEndDate;
                var outsmpStart = validationEndDate;
                var outsmpEnd = validationEndDate;
                
                validationTasks.Add(       // Add the task to collection ->
                    Task.Run(() => 
                        ValidateOutOfSample(bestChromosomeBase,
                            insmpStart,
                            insmpEnd,
                            outsmpStart,
                            outsmpEnd,
                            SortCriteria.Value,
                            bestInSampleResults))); 

                // Increment the variables and step to the next iteration ->
                // If anchored do not increment insample Start Date ->
                if (!WalkForwardConfiguration.Anchored.Value)
                {
                    insampleStartDate = insampleStartDate.AddDays(step);
                }
                insampleEndDate = insampleEndDate.AddDays(step);
                validationStartDate = validationStartDate.AddDays(step);
                validationEndDate = validationEndDate.AddDays(step);
            }

            // Make sure all out-of-sample experiment are completed before exit ->
            Task.WaitAll(validationTasks.ToArray());
        }

        /// <summary>
        /// Method called in the backgound to validate the best chromosome on out-of-sample data
        /// </summary>
        protected void ValidateOutOfSample(
            Chromosome chromosome,
            DateTime insampleStartDate,
            DateTime insampleEndDate,
            DateTime validationStartDate,
            DateTime validationEndDate,
            FitnessScore fitScore,
            Dictionary<string, decimal> bestInSampleResults)
        {
            Program.Logger.Trace($" Starting WFO validation from ({validationStartDate} to {validationEndDate})");

            // clone the best in-sample chromosome
            var validationSolution = chromosome.CreateNew();

            // Run a backtest ->
            IFitness fitness;
            if (Program.Config.TaskExecutionMode == TaskExecutionMode.Azure)
            {
                fitness = new AzureFitness(validationStartDate, validationEndDate, fitScore);
                fitness.Evaluate(validationSolution);
            }
            else
            {
                fitness = new OptimizerFitness(validationStartDate, validationEndDate, fitScore);
                fitness.Evaluate(validationSolution);
            }
            
            // Save full results to dictionary ->
            var validationResults = ((Chromosome)validationSolution).FitnessResult.FullResults;
            _validationResultsList.Add(validationResults);

            // Raise an event to inform a single step of evaluation was completed ->
            OnWfoStepCompleted(
                chromosome,
                insampleStartDate,
                insampleEndDate,
                validationStartDate,
                validationEndDate,
                bestInSampleResults, 
                validationResults);
        }

        /// <summary>
        /// Wrapper for OneEvaluationStepCompleted event
        /// </summary>
        protected virtual void OnWfoStepCompleted(
            IChromosome bestChromosome,
            DateTime insampleStartDate,
            DateTime insampleEndDate,
            DateTime validationStartDate,
            DateTime validationEndDate,
            Dictionary<string, decimal> bestInSampleResults,
            Dictionary<string, decimal> validationResults)
        {
            // Create event args object ->
            var eventArgs = new WalkForwardEventArgs
            {
                Chromosome = bestChromosome,
                InsampleStartDate = insampleStartDate,
                InsampleEndDate = insampleEndDate,
                ValidationStartDate = validationStartDate,
                ValidationEndDate = validationEndDate,
                InSampleBestResultsDict = bestInSampleResults,
                ValidationResultsDict = validationResults
            };

            // Invoke ->
            WfoStepCompleted?.Invoke(this, eventArgs);
        }
    }

    /// <summary>
    /// Event args wrapper for the variables to pass to OneEvaluationStepCompleted event
    /// </summary>
    public class WalkForwardEventArgs : EventArgs
    {
        public IChromosome Chromosome { get; set; }

        public DateTime InsampleStartDate { get; set; }

        public DateTime InsampleEndDate { get; set; }

        public DateTime ValidationStartDate { get; set; }

        public DateTime ValidationEndDate { get; set; }

        public Dictionary<string, decimal> InSampleBestResultsDict { get; set; }

        public Dictionary<string, decimal> ValidationResultsDict { get; set; }
    }
}
