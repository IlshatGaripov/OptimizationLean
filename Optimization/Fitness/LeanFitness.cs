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
        // Algorithm start and end dates
        protected DateTime StartDate;
        protected DateTime EndDate;

        protected LeanFitness(DateTime start, DateTime end)
        {
            StartDate = start;
            EndDate = end;
        }

        public abstract double Evaluate(IChromosome chromosome);
    }
}
