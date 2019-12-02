using System;
using System.Threading.Tasks;
using Optimization.Base;

namespace Optimization.Genetic
{
    /// <summary>
    /// Default optimizer for computation on local machine
    /// </summary>
    public class OptimizerFitness : LeanFitness
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="OptimizerFitness"/> class
        /// </summary>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <param name="fitScore">Fitness value calculation method</param>
        /// <param name="filterEnabled">Indicates whether to apply filter to backtest results</param>
        public OptimizerFitness(DateTime start, DateTime end, FitnessScore fitScore, bool filterEnabled) : 
            base(start, end, fitScore, filterEnabled)
        { }

        /// <summary>
        /// Evaluates the chromosome's fitness.
        /// </summary>
        public override async Task EvaluateAsync(IChromosome chromosome)
        {
            try
            {
                // cast to the base type
                var chromosomeBase = (Chromosome)chromosome;

                // Convert to dictionary and add "id" key-value pair
                var list = chromosomeBase.ToDictionary();
                list.Add("chromosome-id", chromosomeBase.Id);

                // Set algorithm start and end dates
                list.Add("start-date", StartDate.ToString("O"));
                list.Add("end-date", EndDate.ToString("O"));

                // Additional settings to the list
                list.Add("algorithm-type-name", Shared.Config.AlgorithmTypeName);
                list.Add("algorithm-location", Shared.Config.AlgorithmLocation);
                list.Add("data-folder", Shared.Config.DataFolder);

                // Obtain full results 
                var result = await Task.Run(() => AppDomainRunner.RunAlgorithm(list));

                // Calculate fitness and concat the results with an output string
                var fitness = StatisticsAdapter.CalculateFitness(result, FitnessScore, FilterEnabled);

                // Save full results
                chromosomeBase.FitnessResult = new FitnessResult
                {
                    Chromosome = chromosomeBase,
                    StartDate = this.StartDate,
                    EndDate = this.EndDate,
                    FullResults = result
                };

                // create an output string
                var theOutput = chromosomeBase.EvaluationToLogOutput(result, FitnessScore, fitness);

                // Display the output
                lock (_lock)
                {
                    Shared.Logger.Trace(theOutput + Environment.NewLine);
                }

                // assign a value to chromosome fitness
                chromosome.Fitness = fitness;
            }
            catch (Exception ex)
            {
                Shared.Logger.Error("OptimizerFitness.Evaluate: " + ex.Message);
            }
        }

    }
}
