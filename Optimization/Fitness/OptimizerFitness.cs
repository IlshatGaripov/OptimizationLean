using System;
using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    /// <summary>
    /// Default optimizer for computation on local machine
    /// </summary>
    public class OptimizerFitness : LeanFitness
    {
        private static readonly object Obj = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        public OptimizerFitness(DateTime start, DateTime end, FitnessScore fitScore, bool enableFilter) : 
            base(start, end, fitScore, enableFilter)
        { }

        /// <summary>
        /// Evaluates the chromosome's fitness.
        /// </summary>
        public override double Evaluate(IChromosome chromosome)
        {
            try
            {
                // - OUTPUT 1 -
                var chromosomeBase = (Chromosome)chromosome;

                // Convert to dictionary and add "id" key-value pair ->
                var list = chromosomeBase.ToDictionary();
                list.Add("chromosome-id", chromosomeBase.Id);

                // Algorithm input parameters ->
                var paramsString = chromosomeBase.ToKeyValueString();

                // Write to the log information before an experiment ->
                lock (Obj)
                {
                    Program.Logger.Trace($"Chromosome Id: {chromosomeBase.Id} -> params: [{paramsString}]");
                }

                // Set algorithm start and end dates ->
                list.Add("start-date", StartDate.ToString("O"));
                list.Add("end-date", EndDate.ToString("O"));

                // Additional settings to the list ->
                list.Add("algorithm-type-name", Program.Config.AlgorithmTypeName);
                list.Add("algorithm-location", Program.Config.AlgorithmLocation);
                list.Add("data-folder", Program.Config.DataFolder);

                // Obtain full results -> 
                var result = AppDomainManager.RunAlgorithm(list);

                // Calculate fitness and concat the results with an output string ->
                var fitness = StatisticsAdapter.CalculateFitness(result, FitnessScore, FilterEnabled);

                // Save full results ->
                chromosomeBase.FitnessResult = new FitnessResult
                {
                    Chromosome = chromosomeBase,
                    StartDate = this.StartDate,
                    EndDate = this.EndDate,
                    FullResults = result
                };

                // - OUTPUT 2 -
                var output2 = $"chromosome #: {chromosomeBase.Id} results:{Environment.NewLine}";
                output2 += paramsString + Environment.NewLine;

                output2 +=
                    $"-> Fitness = {fitness} Drawdown = {Math.Round(result["Drawdown"], 2)} " +
                    $"TotalNumberOfTrades = {result["TotalNumberOfTrades"]}";

                lock (Obj)
                {
                    // log final output and return the result of evalution
                    Program.Logger.Trace(output2);
                }

                return fitness;
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex.Message);
                return 0;
            }
        }

    }
}
