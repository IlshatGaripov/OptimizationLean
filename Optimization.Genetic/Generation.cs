﻿using System;
using System.Collections.Generic;

namespace Optimization.Genetic
{
    /// <summary>
    /// Represents a generation of a population.
    /// </summary>
    public sealed class Generation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Generation"/> class.
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

            if (chromosomes == null || chromosomes.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chromosomes), 
                    "A generation should have at least 1 chromosome.");
            }

            Number = number;
            CreationDate = DateTime.Now;
            Chromosomes = chromosomes;
        }

        /// <summary>
        /// Gets the number.
        /// </summary>
        public int Number { get; private set; }

        /// <summary>
        /// Gets the creation date.
        /// </summary>
        public DateTime CreationDate { get; private set; }

        /// <summary>
        /// Gets the chromosomes.
        /// </summary>
        public IList<IChromosome> Chromosomes { get; set; }

        /// <summary>
        /// Gets the best chromosome.
        /// </summary>
        public IChromosome BestChromosome { get; set; }
        
        /// <summary>
        /// Whether or not generation is frutless, i.e. no single positive fitness chromosome
        /// </summary>
        public bool IsFruitless { get; set; }
    }
}
