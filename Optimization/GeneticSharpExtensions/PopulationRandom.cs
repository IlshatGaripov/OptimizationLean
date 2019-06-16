using GeneticSharp.Domain.Chromosomes;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Population initialized with pre-defined list of chromosomes.
    /// </summary>
    public sealed class PopulationRandom : PopulationBase
    {
        /// <summary>
        /// Generates a list of chromosomes to make up the inital generation.
        /// See <see cref="PopulationBase.CreateInitialGeneration"/>
        /// </summary>
        protected override IList<IChromosome> GenerateChromosomes()
        {
            var chromosomes = new List<IChromosome>();
            var length = Program.Config.GeneConfigArray.Length;

            // create the list of chromosomes
            for (var i = 0; i < Program.Config.PopulationInitialSize; i++)
            {
                chromosomes.Add(new ChromosomeRandom(length));
            }

            return chromosomes;
        }
    }
}
