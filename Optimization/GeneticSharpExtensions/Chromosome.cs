using GeneticSharp.Domain.Chromosomes;
using System;

namespace Optimization
{
    /// <summary>
    /// Base class for all custom implementations of a chromosome class.
    /// Intermediate for ChromosomeBase (library implementation) and actual implementation.
    /// </summary>
    public class Chromosome : ChromosomeBase
    {
        /// <summary>
        /// Unique chromosome id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Constructor.
        /// </summary>
        public Chromosome(int length) : base(length)
        {
            
        }
        
        /// <summary>
        /// Generates the gene for the specified index.
        /// </summary>
        /// <param name="geneIndex">Gene index.</param>
        /// <returns>The gene generated at the specified index.</returns>
        public override Gene GenerateGene(int geneIndex)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Creates a new chromosome using the same structure of this.
        /// </summary>
        /// <returns>The new chromosome.</returns>
        public override IChromosome CreateNew()
        {
            throw new System.NotImplementedException();
        }
    }
}
