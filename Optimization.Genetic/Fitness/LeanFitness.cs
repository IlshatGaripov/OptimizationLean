using System;
using Optimization.Base;

namespace Optimization.Genetic
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
        /// <param name="filterEnabled">Indicates whether to apply filter to backtest results</param>
        protected LeanFitness(DateTime start, DateTime end, FitnessScore fitScore, bool filterEnabled)
        {
            StartDate = start;
            EndDate = end;
            FitnessScore = fitScore;
            FilterEnabled = filterEnabled;
        }

        public abstract double Evaluate(IChromosome chromosome);
    }
}
