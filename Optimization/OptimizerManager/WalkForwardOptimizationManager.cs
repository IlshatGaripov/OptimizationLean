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
        public event EventHandler<WalkForwardEventArgs> OneEvaluationStepCompleted;

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
            var days = WalkForwardConfiguration.InSamplePeriod.Value + WalkForwardConfiguration.Step.Value;
            if (StartDate.Value.AddDays(days - 1) > EndDate.Value)
                throw new ArgumentOutOfRangeException(
                    $"The range between {StartDate.Value} and {EndDate.Value} is short for walk forward configuration values specified");

            // Init datetime variables will be used in first iteration ->
            var insampleStartDate = StartDate.Value;
            var insampleEndDate = insampleStartDate.AddDays(WalkForwardConfiguration.InSamplePeriod.Value - 1);
            var validationStartDate = insampleEndDate.AddDays(1);
            var validationEndDate = validationStartDate.AddDays(WalkForwardConfiguration.Step.Value - 1);

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
                var bestChromosome = optimumFinder.GenAlgorithm.BestChromosome;
                var bestChromosomeBase = (Chromosome)optimumFinder.GenAlgorithm.BestChromosome;

                // Save best results to the list ->
                var bestInSampleResults = bestChromosomeBase.FitnessResult.FullResults;
                _inSampleBestResultsList.Add(bestInSampleResults);

                // Validate out of sample ->
                var date = validationStartDate;
                var endDate = validationEndDate;

                // Add task to the collection ->
                validationTasks.Add(Task.Run(() => ValidateOutOfSample(bestChromosome, 
                    date,
                    endDate,
                    SortCriteria.Value,
                    bestInSampleResults))); 

                // Increment the variables and step to the next iteration ->
                // If anchored do not increment insample Start Date ->
                if (!WalkForwardConfiguration.Anchored.Value)
                    insampleStartDate = insampleStartDate.AddDays(step);

                insampleEndDate = insampleEndDate.AddDays(step);
                validationStartDate = validationStartDate.AddDays(step);
                validationEndDate = validationEndDate.AddDays(step);
            }

            // Make sure all out-of-sample experiment are complete before exit ->
            Task.WaitAll(validationTasks.ToArray());
        }

        /// <summary>
        /// Method called in the backgound to validate the best chromosome on out-of-sample data
        /// </summary>
        /// <param name="chromosome"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="fitScore"></param>
        /// <param name="bestInSampleResults"></param>
        protected void ValidateOutOfSample(IChromosome chromosome,
            DateTime startDate,
            DateTime endDate,
            FitnessScore fitScore,
            Dictionary<string, decimal> bestInSampleResults)
        {
            Program.Logger.Trace($" >> WALK FORWARD VALIDATION ({startDate} to {endDate}) STARTED >> \n");

            // Cast to base class ->
            var bestChromosomeBase = (Chromosome) chromosome;

            // Save best chromosome's genes to dictionary ->
            var bestGenes = bestChromosomeBase.ToDictionary();

            // Run a backtest ->
            IFitness fitness;
            if (Program.Config.TaskExecutionMode == TaskExecutionMode.Azure)
            {
                fitness = new AzureFitness(startDate, endDate, fitScore);
                fitness.Evaluate(chromosome);
            }
            else
            {
                fitness = new OptimizerFitness(startDate, endDate, fitScore);
                fitness.Evaluate(chromosome);
            }
            
            // Save full results to dictionary ->
            var validationResults = bestChromosomeBase.FitnessResult.FullResults;
            _validationResultsList.Add(validationResults);

            // Raise an event informing a single step of evaluation is completed ->
            OnOneEvaluationStepCompleted(bestInSampleResults, validationResults, bestGenes);
        }

        /// <summary>
        /// Wrapper for OneEvaluationStepCompleted event
        /// </summary>
        /// <param name="bestInSampleResults"></param>
        /// <param name="validationResults"></param>
        /// <param name="bestGenes"></param>
        protected virtual void OnOneEvaluationStepCompleted(Dictionary<string, decimal> bestInSampleResults,
            Dictionary<string, decimal> validationResults,
            Dictionary<string, string> bestGenes)
        {
            // Create event args object and invoke a delegate ->
            var eventArgs = new WalkForwardEventArgs
            {
                InSampleBestResults = bestInSampleResults,
                ValidationResults = validationResults,
                BestGenes = bestGenes
            };
            OneEvaluationStepCompleted?.Invoke(this, eventArgs);
        }
    }

    /// <summary>
    /// Event args wrapper for the variables to pass to OneEvaluationStepCompleted event
    /// </summary>
    public class WalkForwardEventArgs : EventArgs
    {
        /// <summary>
        /// Dictionary with full backtest results for best in-sample chromosome 
        /// </summary>
        public Dictionary<string, decimal> InSampleBestResults { get; set; }

        /// <summary>
        /// Dictionary with full validation results on out-of-sample data
        /// </summary>
        public Dictionary<string, decimal> ValidationResults { get; set; }

        /// <summary>
        /// Genes that showed best performance on in sample data
        /// </summary>
        public Dictionary<string, string> BestGenes { get; set; }

    }
}
