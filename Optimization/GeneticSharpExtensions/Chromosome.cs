using GeneticSharp.Domain.Chromosomes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        /// Flag indicating whether take actual value or generate it randomly.
        /// </summary>
        private readonly bool _isActual;

        /// <summary>
        /// Unique chromosome id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Constructor.
        /// </summary>
        public Chromosome(bool isActual, GeneConfiguration[] config) : base(config.Length)
        {
            _isActual = isActual;
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
            return GeneFactory.Generate(geneConfig, _isActual);
        }

        /// <summary>
        /// Creates a new chromosome using the same structure of this.
        /// </summary>
        /// <returns>The new chromosome.</returns>
        public override IChromosome CreateNew()
        {
            return new Chromosome(_isActual, _config);
        }

        /// <summary>
        /// Converts a collection of chromosome genes into string/object dictionary.
        /// </summary>
        public Dictionary<string, object> ToDictionary()
        {
            // TODO: wondering what would be logged after this expression been used?
            return GetGenes().ToDictionary(d => ((KeyValuePair<string, object>)d.Value).Key, d => ((KeyValuePair<string, object>)d.Value).Value);
        }

        public string ToKeyValueString()
        {
            var output = new StringBuilder();
            foreach (var item in this.ToDictionary())
            {
                output.Append(item.Key).Append(": ").Append(item.Value.ToString()).Append(", ");
            }

            return output.ToString().TrimEnd(',', ' ');
        }

    }

}
