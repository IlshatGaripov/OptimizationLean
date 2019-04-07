using GeneticSharp.Domain.Chromosomes;
using System;

namespace Optimization
{
    /// <summary>
    /// Custom implementation of a chromosome class.
    /// </summary>
    public sealed class Chromosome : ChromosomeBase
    {
        /// <summary>
        /// Array of gene configurations.
        /// </summary>
        private readonly GeneConfiguration[] _config;

        /// <summary>
        /// Unique chromosome id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Constructor.
        /// </summary>
        public Chromosome( GeneConfiguration[] config) : base(config.Length)
        {
            _config = config;
            
            // fill the gene array with generated values
            for (var i = 0; i < _config.Length; i++)
            {
                ReplaceGene(i, GenerateGene(i));
            }
        }
        
        /// <summary>
        /// Generates the gene for the specified index.
        /// </summary>
        /// <param name="geneIndex">Gene index.</param>
        /// <returns>The gene generated at the specified index.</returns>
        public override Gene GenerateGene(int geneIndex)
        {
            var geneConfig = _config[geneIndex];
            return GeneFactory.GenerateRandom(geneConfig);
        }

        /// <summary>
        /// Creates a new chromosome using the same structure of this.
        /// </summary>
        /// <returns>The new chromosome.</returns>
        public override IChromosome CreateNew()
        {
            return new Chromosome( _config);
        }
    }
}
