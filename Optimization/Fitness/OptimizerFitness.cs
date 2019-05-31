using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    /// <summary>
    /// Default optimizer for computation on local machine
    /// </summary>
    public class OptimizerFitness : LeanFitness
    {
        private static readonly object Obj = new object();

        public OptimizerFitness(DateTime start, DateTime end) : base(start, end)
        { }

        /// <summary>
        /// Evaluates the chromosome's fitness.
        /// </summary>
        public override double Evaluate(IChromosome chromosome)
        {
            try
            {
                // - OUTPUT 1 -
                var outputBeforeRun = string.Empty;
                var chromosomeBase = (Chromosome)chromosome;

                // convert to dictionary and add "id" item
                var list = chromosomeBase.ToDictionary();
                var paramsString = "~ " + list.Aggregate(outputBeforeRun, (current, item) =>
                    current + item.Key + ": " + item.Value + " |");

                list.Add("Id", chromosomeBase.Id);
                outputBeforeRun += $"Send for backtest Chromosome Id: {chromosomeBase.Id} w.params:{Environment.NewLine}";
                outputBeforeRun += paramsString;

                // set algorithm start and end dates
                list.Add("startDate", StartDate);
                list.Add("endDate", EndDate);

                lock (Obj)
                {
                    // log the result before an experiment (backtest)
                    Program.Logger.Info(outputBeforeRun);
                }

                // Additional setting to the list ->
                list.Add("algorithm-type-name", Program.Config.AlgorithmTypeName);
                list.Add("algorithm-location", Program.Config.AlgorithmLocation);
                list.Add("data-folder", Program.Config.DataFolder);

                // Obtain full results -> 
                var result = OptimizerAppDomainManager.RunAlgorithm(list);

                // Save full results ->
                chromosomeBase.FullResults = result;

                // - OUTPUT 2 -
                var outputResult = $"chromosome #: {chromosomeBase.Id} results:{Environment.NewLine}";
                outputResult += paramsString + Environment.NewLine;
                
                // calculate fitness and concat the results to an output string
                var fitness = StatisticsAdapter.CalculateFitness(result, Program.Config.FitnessScore);

                outputResult +=
                    $"-> Fitness = {fitness} Drawdown = {Math.Round(result["Drawdown"], 2)} " +
                    $"TotalNumberOfTrades = {result["TotalNumberOfTrades"]}";

                lock (Obj)
                {
                    // log final output and return the result of evalution
                    Program.Logger.Info(outputResult);
                }

                return fitness;
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex);
                return 0;
            }
        }

    }
}
