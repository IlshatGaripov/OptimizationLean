using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <summary>
    /// Default optimizer behaviour using Sharpe ratio.
    /// </summary>
    /// <remarks>Default behaviour will nullify fitness for negative return</remarks>
    public class OptimizerFitness : IFitness
    {
        private static readonly object Obj = new object();

        public string Name { get; set; } = "Sharpe";

        /// <summary>
        /// Filter used to sort out insignificant values.
        /// </summary>
        public IFitnessFilter Filter { get; set; } = new FitnessFilter();

        /// <summary>
        /// Evaluates the chromosome's fitness.
        /// </summary>
        public virtual double Evaluate(IChromosome chromosome)
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

                // Algorithm start and end dates
                if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
                {
                    // set algorithm start and end dates
                    list.Add("startDate", Program.Config.StartDate);
                    list.Add("endDate", Program.Config.EndDate);
                }

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
                var fitness = CalculateFitness(result);

                outputResult += $"~ Fitness.Value({Name}) = {fitness} ";
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

        /// <summary>
        /// Calculates the fitness by full
        /// </summary>
        /// <param name="result">Full backtest results of<see cref="OptimizerResultHandler"/></param>
        protected virtual double CalculateFitness(Dictionary<string, decimal> result)
        {
            return (double)result["SharpeRatio"]; 
        }

    }
}
