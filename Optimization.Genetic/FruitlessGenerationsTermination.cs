using System;

namespace Optimization.Genetic
{
    /// <summary>
    /// If frutless generation have exceeded the limit returns a signal to terminate.
    /// </summary>
    public class FruitlessGenerationsTermination : TerminationBase
    {
        private readonly int _fruitlessGenerations;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fruitlessGen">Number of gruitless generation to terminate</param>
        public FruitlessGenerationsTermination(int fruitlessGen)
        {
            _fruitlessGenerations = fruitlessGen;
        }

        /// <summary>
        /// Determines whether the specified geneticAlgorithm reached the termination condition.
        /// </summary>
        /// <returns>True if termination has been reached, otherwise false.</returns>
        /// <param name="geneticAlgorithm">The genetic algorithm.</param>
        protected override bool PerformHasReached(IGeneticAlgorithm geneticAlgorithm)
        {
            var ga = geneticAlgorithm as GeneticAlgorithm;

            if (ga == null) throw new InvalidCastException("I am expecting genetic algorithm to be of Custom type!");

            return ga.Population.FruitlessGenerationsCount >= _fruitlessGenerations;
        }
    }
}
