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
    public class Chromosome : ChromosomeBase
    {
        private readonly GeneConfiguration[] _config;
        private readonly bool _isActual;
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public Chromosome(bool isActual, GeneConfiguration[] config) : base(config.Length)
        {
            _isActual = isActual;
            _config = config;

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
        public sealed override Gene GenerateGene(int geneIndex)
        {
            var item = _config[geneIndex];
            return GeneFactory.Generate(item, _isActual);
        }

        public override IChromosome CreateNew()
        {
            return new Chromosome(false, GeneFactory.Config);
        }

        public override IChromosome Clone()
        {
            var clone = base.Clone() as Chromosome;
            return clone;
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
