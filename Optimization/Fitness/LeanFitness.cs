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
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Algorithm end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Fitness score
        /// </summary>
        protected FitnessScore FitnessScore { get; set; }

        /// <summary>
        /// Flag indicating whether or not enable filtering of results
        /// </summary>
        protected bool FilterEnabled { get; set; }

        /// <summary>
        /// Base class constructor for all custom fitness objects implementations
        /// </summary>
        /// <param name="start">Start date</param>
        /// <param name="end">End date</param>
        /// <param name="fitScore">Fitness value calculation method</param>
        /// <param name="enableFilter">Whether or not to apply filter to results of fitness evaluation</param>
        protected LeanFitness(DateTime start, DateTime end, FitnessScore fitScore, bool enableFilter)
        {
            StartDate = start;
            EndDate = end;
            FitnessScore = fitScore;
            FilterEnabled = enableFilter;
        }

        public abstract double Evaluate(IChromosome chromosome);
    }
}
