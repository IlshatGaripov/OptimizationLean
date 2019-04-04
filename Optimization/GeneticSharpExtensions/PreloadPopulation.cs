using System;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Population initialized with pre-defined list of chromosomes.
    /// </summary>
    public class PreloadPopulation : PopulationBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PreloadPopulation"/> class.
        /// </summary>
        public PreloadPopulation()
        {
            // time
            CreationDate = DateTime.Now;

            // create generation list.
            Generations = new List<Generation>();

            // generation strategy - only a single (new) generation will be kept in population.
            GenerationStrategy = new PerformanceGenerationStrategy(1);
        }

        /// <summary>
        /// Creates the initial generation.
        /// </summary>
        public override void CreateInitialGeneration()
        {
            GenerationsNumber = 0;

            var chromosomesList = GenerateChromosomesRandomly();

            // calls the base class method. Chromosomes validation is held inside.
            CreateNewGeneration(chromosomesList);
        }

        /// <summary>
        /// Fills the chromosome list with appropriate values.
        /// Default behavior is just generate it randomly depending on given size (config)
        /// and GeneFactory randomization methods and GeneConfig settings
        /// </summary>
        public static IList<IChromosome> GenerateChromosomesRandomly()
        {
            var chromosomes = new List<IChromosome>();

            // create the pre defined list of chromosomes
            for (var i = 0; i < Program.Config.PopulationSize; i++)
            {
                chromosomes.Add(new Chromosome(GeneFactory.GeneConfigArray));
            }

            return chromosomes;
        }
    }
}
