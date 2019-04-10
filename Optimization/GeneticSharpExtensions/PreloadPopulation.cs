using GeneticSharp.Domain.Chromosomes;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Population initialized with pre-defined list of chromosomes.
    /// </summary>
    public sealed class PreloadPopulation : PopulationBase
    {
        /// <summary>
        /// Generates the chromosome list.
        /// Default behavior is just generate it randomly. of given size specificated in config.
        /// and using GeneFactory randomization methods and GeneConfig settings (max, min, scale).
        /// </summary>
        protected override IList<IChromosome> GenerateChromosomes()
        {
            var chromosomes = new List<IChromosome>();

            // create the pre defined list of chromosomes
            for (var i = 0; i < Program.Config.PopulationSize; i++)
            {
                chromosomes.Add(new ChromosomeRandom(Program.Config.Genes.Length));
            }

            return chromosomes;
        }
    }
}
