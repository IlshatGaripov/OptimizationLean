using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Infrastructure.Framework.Texts;
using System.Diagnostics;

namespace Optimization
{
    /// <summary>
    /// Represents a generation of a population.
    /// </summary>
    [DebuggerDisplay("{Number} = {BestChromosome.Fitness}")]
    public sealed class Generation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.Populations.Generation"/> class.
        /// </summary>
        /// <param name="number">The generation number.</param>
        /// <param name="chromosomes">The chromosomes of the generation..</param>
        public Generation(int number, IList<IChromosome> chromosomes)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(number),
                    "Generation number {0} is invalid. Generation number should be positive and start in 1.".With(number));
            }

            if (chromosomes == null || chromosomes.Count < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(chromosomes), "A generation should have at least 2 chromosomes.");
            }

            Number = number;
            CreationDate = DateTime.Now;
            Chromosomes = chromosomes;
        }

        /// <summary>
        /// Gets the number.
        /// </summary>
        /// <value>The number.</value>
        public int Number { get; private set; }

        /// <summary>
        /// Gets the creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }

        /// <summary>
        /// Gets the chromosomes.
        /// </summary>
        /// <value>The chromosomes.</value>
        public IList<IChromosome> Chromosomes { get; set; }

        /// <summary>
        /// Gets the best chromosome.
        /// </summary>
        /// <value>The best chromosome.</value>
        public IChromosome BestChromosome { get; set; }
       
    }
}
