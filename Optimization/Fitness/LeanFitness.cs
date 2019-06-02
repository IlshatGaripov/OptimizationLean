using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <summary>
    /// Base class for custom Fitness implementations
    /// </summary>
    public abstract class LeanFitness: IFitness
    {
        /// <summary>
        /// Algorithm start date
        /// </summary>
        protected DateTime StartDate { get; set; }

        /// <summary>
        /// Algorithm end date
        /// </summary>
        protected DateTime EndDate { get; set; }

        /// <summary>
        /// Fitness score
        /// </summary>
        protected FitnessScore FitnessScore { get; set; }

        protected LeanFitness(DateTime start, DateTime end, FitnessScore fitScore)
        {
            StartDate = start;
            EndDate = end;
            FitnessScore = fitScore;
        }

        public abstract double Evaluate(IChromosome chromosome);
    }
}
