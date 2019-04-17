using System;
using System.Collections.Generic;
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
                var output = "";
                
                // convert to dictionary
                var list = ((Chromosome)chromosome).ToDictionary();
                
                // add one more item to the dictionary that will stand for chromosome id
                list.Add("Id", ((Chromosome)chromosome).Id);

                foreach (var item in list)
                {
                    output += item.Key + ": " + item.Value + ", ";
                }

                // Algorithm start and end dates
                if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
                {
                    output += $"Start: {Program.Config.StartDate}, End: {Program.Config.EndDate}, ";

                    // set algorithm start and end dates
                    list.Add("startDate", Program.Config.StartDate);
                    list.Add("endDate", Program.Config.EndDate);
                }

                // run the algorithm - core functionality.
                var result = OptimizerAppDomainManager.RunAlgorithm(list);

                if (result == null)
                {
                    // do we need additional logging when result is null ?? 
                    // Program.Logger ..

                    return 0;
                }

                var fitness = CalculateFitness(result);

                output += $"{Name}: {fitness.Value}";

                // log final output
                Program.Logger.Info(output);

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

            // if there is an isignificant result by applying filter
            if (Filter != null && !Filter.IsSuccess(result, this))
            {
                // then assign a ratio the default error value  
                ratio = ErrorRatio;
            }

            // otherwise fitness value is ratio
            fitness.Value = ratio;

            // what is a fitness fitness for? 
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
            /// The scaled or adjused fitness
            /// </summary>
            public double Fitness { get; set; }
        }


    }
}
