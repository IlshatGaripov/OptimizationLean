using GeneticSharp.Domain.Chromosomes;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Population initialized with pre-defined list of chromosomes.
    /// </summary>
    public sealed class PopulationRandom : PopulationBase
    {
        private readonly GeneConfiguration[] _geneConfigurationArray;
        private readonly int _initialSize;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configArray">Array with gene configuration</param>
        /// <param name="initialSize">Array with gene configuration</param>
        public PopulationRandom(GeneConfiguration[] configArray, int initialSize)
        {
            _geneConfigurationArray = configArray;
            _initialSize = initialSize;
        }

        /// <summary>
        /// Generates a list of chromosomes to make up the inital generation.
        /// See <see cref="PopulationBase.CreateInitialGeneration"/>
        /// </summary>
        protected override IList<IChromosome> GenerateChromosomes()
        {
            var chromosomes = new List<IChromosome>();
            var length = _geneConfigurationArray.Length;

            // create the list of chromosomes
            for (var i = 0; i < _initialSize; i++)
            {
                chromosomes.Add(new ChromosomeRandom(length));
            }

            return chromosomes;
        }
    }
}
