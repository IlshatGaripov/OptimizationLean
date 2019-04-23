using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    /// <summary>
    /// Population for brute force optimization.
    /// </summary>
    public sealed class PopulationCartesian : PopulationBase
    {
        /// <summary>
        /// Creates a population for brute force optimization mode.
        /// All possible combinations (within bounds of acceptable values specified in optimization config) are generated
        /// </summary>
        protected override IList<IChromosome> GenerateChromosomes()
        {
            // in this list we will store the sequences of possible gene values represented as an object array.
            var inputGeneValuesContainer = new List<object[]>();

            // iterate over the gene config objects
            foreach (var config in Program.Config.Genes)
            {
                // we need a step value for every gene to generate cartesian product 
                if (!config.Step.HasValue)
                    throw new Exception($"CartesianPopulation: Please specify the step value for {config.Key}");

                // if decimal values are defined - then create a list of decimal values
                if (config.MinDecimal.HasValue && config.MaxDecimal.HasValue)
                {
                    var sequenceDecimal = GenerateNumberSequence(config.MinDecimal.Value, config.MaxDecimal.Value,
                        config.Step.Value);
                    
                    inputGeneValuesContainer.Add(sequenceDecimal.ToArray());

                    continue;
                }

                // if no decimal nor int values are specified in config - there is a mistake
                if (!config.MinInt.HasValue || !config.MaxInt.HasValue)
                    throw new Exception($"CartesianPopulation: Please define max and min values in config for {config.Key}");
                
                // do the same for int if values we are working with are of int kind
                var stepIntegerCast = (int) config.Step.Value;
                var sequenceInteger = GenerateNumberSequence(config.MinInt.Value, config.MaxInt.Value, stepIntegerCast);
                inputGeneValuesContainer.Add( sequenceInteger.ToArray());
            }

            // now create a cartesian join of possible combinations of values contained in inputGeneValuesContainer. 
            /*
             * this article has nice deep description of how this can be performed:
             * http://www.interact-sw.co.uk/iangblog/2010/07/28/linq-cartesian-1
             */

            var cartesianProduct = CartesianProduct(inputGeneValuesContainer.ToArray());
            
            // LINQ type return - compact and nice.
            return cartesianProduct.Select(input => new ChromosomeCartesian(input)).Cast<IChromosome>().ToList();
        }

        /// <summary>
        /// Generates number sequence with step size. {Int.}
        /// </summary>
        public static IEnumerable<object> GenerateNumberSequence(int startingValue, int endValue, int increment)
        {
            for (var i = startingValue; i <= endValue; i += increment)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates number sequence with step size. {Decimal overload.}
        /// </summary>
        public static IEnumerable<object> GenerateNumberSequence(decimal startingValue, decimal endValue, decimal increment)
        {
            for (var i = startingValue; i <= endValue; i += increment)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Generates the Cartesian product any number of sets.
        /// </summary>
        /// <param name="inputs">Array of number of sets.</param>
        public static IEnumerable<object[]> CartesianProduct(params object[][] inputs)
        {
            IEnumerable<object[]> soFar = new[] { new object[0] };

            return inputs.Aggregate(soFar,
                (current, currentInput) => current.SelectMany(prevProductItem =>
                    from item in currentInput
                    select prevProductItem.Concat(new[] { item }).ToArray()));
        }
    }
}
