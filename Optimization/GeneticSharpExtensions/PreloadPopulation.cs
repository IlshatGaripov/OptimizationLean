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
        /// List that stores the loaded chromosomes.
        /// </summary>
        private readonly IList<IChromosome> _chromosomes;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreloadPopulation"/> class.
        /// </summary>
        /// <param name="chromosomes">The list of preload chromosomes.</param>
        public PreloadPopulation(IList<IChromosome> chromosomes)
        {
            _chromosomes = chromosomes;

            // time
            CreationDate = DateTime.Now;

            // create generation list.
            Generations = new List<Generation>();

            // generation strategy - only a single generation will be kept in population.
            GenerationStrategy = new PerformanceGenerationStrategy();
        }

        /// <summary>
        /// Creates the initial generation.
        /// </summary>
        public override void CreateInitialGeneration()
        {
            GenerationsNumber = 0;
            
            // create the bast class method. Chromosome validation is held inside.
            CreateNewGeneration(_chromosomes);
        }
    }
}
