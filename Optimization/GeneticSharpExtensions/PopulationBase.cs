using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Infrastructure.Framework.Commons;

namespace Optimization
{
    /// <summary>
    /// Represents a base abstract class for population of candidate solutions (chromosomes).
    /// </summary>
    public abstract class PopulationBase : IPopulation
    {
        /// <summary>
        /// Constructor. General for all derived class. Performing general (common for inherited) init behavior.
        /// </summary>
        protected PopulationBase()
        {
            // time
            CreationDate = DateTime.Now;

            // create generation list.
            Generations = new List<Generation>();

            // generation strategy - only a single (new) generation will be kept in population.
            GenerationStrategy = new PerformanceGenerationStrategy(1);
        }

        /// <summary>
        /// Occurs when best chromosome changed.
        /// </summary>
        public event EventHandler BestChromosomeChanged;
        
        /// <summary>
        /// Gets or sets the creation date.
        /// </summary>
        public DateTime CreationDate { get; protected set; }

        /// <summary>
        /// Gets or sets the generations.
        /// <remarks>
        /// The information of Generations can vary depending of the IGenerationStrategy used.
        /// </remarks>
        /// </summary>
        /// <value>The generations.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Parent classes need to set it.")]
        public IList<Generation> Generations { get; protected set; }

        /// <summary>
        /// Gets or sets the current generation.
        /// </summary>
        /// <value>The current generation.</value>
        public Generation CurrentGeneration { get; protected set; }

        /// <summary>
        /// Gets or sets the total number of generations executed.
        /// <remarks>
        /// Use this information to know how many generations have been executed, because Generations.Count can vary depending of the IGenerationStrategy used.
        /// </remarks>
        /// </summary>
        public int GenerationsNumber { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum size.
        /// </summary>
        /// <value>The minimum size.</value>
        public int MinSize { get; set; }

        /// <summary>
        /// Gets or sets the size of the max.
        /// </summary>
        /// <value>The size of the max.</value>
        public int MaxSize { get; set; }

        /// <summary>
        /// Gets or sets the best chromosome.
        /// </summary>
        /// <value>The best chromosome.</value>
        public IChromosome BestChromosome { get; protected set; }

        /// <summary>
        /// Gets or sets the generation strategy.
        /// </summary>
        public IGenerationStrategy GenerationStrategy { get; set; }

        /// <summary>
        /// Creates the initial generation.
        /// </summary>
        public virtual void CreateInitialGeneration()
        {
            GenerationsNumber = 0;

            // Initial generation
            var chromosomesList = GenerateChromosomes();

            // calls the base class method. Chromosomes validation is held inside.
            CreateNewGeneration(chromosomesList);
        }

        /// <summary>
        /// Generates a list of chromosomes to make up the inital generation.
        /// See <see cref="CreateInitialGeneration"/>
        /// </summary>
        protected virtual IList<IChromosome> GenerateChromosomes() => throw new System.NotImplementedException();

        /// <summary>
        /// Creates a new generation.
        /// </summary>
        /// <param name="chromosomes">The chromosomes for new generation.</param>
        public virtual void CreateNewGeneration(IList<IChromosome> chromosomes)
        {
            ExceptionHelper.ThrowIfNull("chromosomes", chromosomes);

            // Validate for not null values ->
            chromosomes.ValidateGenes();
            
            // Create new Generation and add to collection ->
            CurrentGeneration = new Generation(++GenerationsNumber, chromosomes);
            Generations.Add(CurrentGeneration);

            // Leaves only predetermined number of object in Generations list (default values is 1) ->
            GenerationStrategy.RegisterNewGeneration(this);
        }

        /// <summary>
        /// Ends the current generation.
        /// <remarks>
        /// Method evaluates the best chromosome. As well as reduce the population to a given number of best solutions. 
        /// </remarks>
        /// </summary>
        public virtual void EndCurrentGeneration()
        {
            // How many chromosomes to keep in list ->
            var size = CurrentGeneration.Chromosomes.Count;

            // Reduce the population to given number of best solutions ->
            CurrentGeneration.End(size);

            // If there is no better chromosome than already exist then return ->
            if (Equals(BestChromosome, CurrentGeneration.BestChromosome)) return;

            // Otherwise ->
            BestChromosome = CurrentGeneration.BestChromosome;
            OnBestChromosomeChanged(EventArgs.Empty);
        }
        
        /// <summary>
        /// Raises the best chromosome changed event.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        protected virtual void OnBestChromosomeChanged(EventArgs args)
        {
            BestChromosomeChanged?.Invoke(this, args);
        }
    }
}