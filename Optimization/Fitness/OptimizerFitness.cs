using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <inheritdoc />
    /// <summary>
    /// Default optimizer behaviour using Sharpe ratio.
    /// </summary>
    /// <remarks>Default behaviour will nullify fitness for negative return</remarks>
    public class OptimizerFitness : IFitness
    {
        private static readonly object Obj = new object();

        /// <summary>
        /// Name. Virtual.
        /// </summary>
        public string Name { get; set; } = "Sharpe";

        /// <summary>
        /// Filter used to sort out insignificant values.
        /// </summary>
        public IFitnessFilter Filter { get; set; }

        /// <summary>
        /// The scale used to calculate the normalized value of fitness. Can be overriden is child class.
        /// </summary>
        protected virtual double Scale { get; set; } = 0.02;

        /// <summary>
        /// Default value for insignificat result of evaluation.
        /// </summary>
        protected const decimal ErrorRatio = -10;

        /// <summary>
        /// Constructor.
        /// </summary>
        public OptimizerFitness(IFitnessFilter filter = null)
        {
            Filter = filter;
        }

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
                outputResult += $"~ Fitness.Value({Name}) = {fitness.Value} ";
                outputResult += $"Drawdown = {Math.Round(result["Drawdown"], 2)} TotalNumberOfTrades = {result["TotalNumberOfTrades"]}";

                lock (Obj)
                {
                    // log final output and return the result of evalution
                    Program.Logger.Info(outputResult);
                }

                return fitness.Fitness;
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex);
                return 0;
            }
        }

        /// <summary>
        /// Calculates the fitness by Sharp Ratio.
        /// </summary>
        protected virtual FitnessResult CalculateFitness(Dictionary<string, decimal> result)
        {
            var fitness = new FitnessResult();
            var ratio = result["SharpeRatio"];

            // if there is an isignificant result revealed by appying a filter
            if (Filter != null && !Filter.IsSuccess(result, this))
            {
                // then assign a ratio the default error value  
                ratio = ErrorRatio;
            }

            fitness.Value = ratio;
            fitness.Fitness = (double)(Math.Max(ratio, ErrorRatio) + 10) * Scale;

            return fitness;
        }

        /// <summary>
        /// ??
        /// </summary>
        public virtual double GetAdjustedFitness(double? fitness)
        {
            return fitness.HasValue ? fitness.Value / Scale - 10 : 0;

        }
        
        /// <summary>
        /// Nested class to contain the fintess function results.
        /// </summary>
        protected class FitnessResult
        {
            /// <summary>
            /// The value of the result
            /// </summary>
            public decimal Value { get; set; }

            /// <summary>
            /// The scaled or adjused measurement
            /// </summary>
            public double Fitness { get; set; }
        }


    }
}
