using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Infrastructure.Framework.Commons;

namespace Optimization
{
    /// <summary>
    /// Represents a base abstract class for all custom population object implementations.
    /// </summary>
    public abstract class PopulationBase
    {
        /// <summary>
        /// Constructor. General for all derived class. Performing general (common for inheritors) init behavior.
        /// </summary>
        protected PopulationBase()
        {
            CreationDate = DateTime.Now;
            Generations = new List<Generation>();
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
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Parent classes need to set it.")]
        public IList<Generation> Generations { get; protected set; }

        /// <summary>
        /// Gets or sets the current generation.
        /// </summary>
        /// <value>The current generation.</value>
        public Generation CurrentGeneration { get; protected set; }

        /// <summary>
        /// Gets or sets the total number of generations executed.
        /// </summary>
        public int GenerationsNumber { get; protected set; }

        /// <summary>
        /// Gets or sets the best chromosome.
        /// </summary>
        /// <value>The best chromosome.</value>
        public IChromosome BestChromosome { get; protected set; }

        /// <summary>
        /// Creates the initial generation.
        /// </summary>
        public virtual void CreateInitialGeneration()
        {
            GenerationsNumber = 0;

            // Generate chromosomes and define a first generation ->
            var chromosomesList = GenerateChromosomes();
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
        }

        /// <summary>
        /// Fixes the best chromosome.
        /// </summary>
        public virtual void RegisterTheBestChromosome()
        {
            // If there is no better chromosome than already have just return ->
            if (Equals(BestChromosome, CurrentGeneration.BestChromosome))
            {
                return;
            }

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