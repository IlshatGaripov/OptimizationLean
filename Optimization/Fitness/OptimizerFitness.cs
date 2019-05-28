using System;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    /// <summary>
    /// Default optimizer behaviour.
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
                // == OUTPUT 1 ==
                var outputBeforeRun = string.Empty;
                var chromosomeCasted = (Chromosome)chromosome;

                // convert to dictionary and add "id" item
                var list = chromosomeCasted.ToDictionary();
                var paramsString = "~ " + list.Aggregate(outputBeforeRun, (current, item) =>
                    current + item.Key + ": " + item.Value + " |");

                list.Add("Id", chromosomeCasted.Id);
                outputBeforeRun += $"Send for backtest Chromosome Id: {chromosomeCasted.Id} w.params:{Environment.NewLine}";
                outputBeforeRun += paramsString;

                // set algorithm start and end dates
                list.Add("startDate", StartDate);
                list.Add("endDate", EndDate);

                lock (Obj)
                {
                    // log the result before an experiment (backtest)
                    Program.Logger.Info(outputBeforeRun);
                }
                
                // Calculate
                var result = OptimizerAppDomainManager.RunAlgorithm(list);   // run the algorithm

                // == OUTPUT 2 ==
                var outputResult = $"PRINT results for Chromosome Id: {chromosomeCasted.Id} w.params:{Environment.NewLine}";
                outputResult += paramsString + Environment.NewLine;
                
                // calculate fitness and concat the results to an output string
                var fitness = StatisticsAdapter.CalculateFitness(result, Program.Config.FitnessScore);

                outputResult += $"~ Fitness.Value = {fitness} ";
                outputResult += $"Drawdown = {Math.Round(result["Drawdown"], 2)} TotalNumberOfTrades = {result["TotalNumberOfTrades"]}";

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
