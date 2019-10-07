using System.Collections.Generic;
using Optimization.Base;

namespace Optimization.Genetic
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
        /// <param name="initialSize">Initial population size</param>
        public PopulationRandom(GeneConfiguration[] configArray, int initialSize)
        {
            _geneConfigurationArray = configArray;
            _initialSize = initialSize;
        }

        /// <summary>
        /// Generates a list of chromosomes to make up the inital generation.
        /// See <see cref="PopulationBase.CreateInitialGeneration"/>
        /// </summary>
        public override List<IChromosome> GenerateChromosomes()
        {
            var chromosomes = new List<IChromosome>();

            // create the list of chromosomes
            for (var i = 0; i < _initialSize; i++)
            {
                chromosomes.Add(new ChromosomeRandom(_geneConfigurationArray, _geneConfigurationArray.Length));
            }

            return chromosomes;
        }
    }
}
