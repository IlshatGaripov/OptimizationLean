using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;

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
            GenerationsNumber = 0;
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
            // Generate chromosomes and define a first generation ->
            var chromosomesList = GenerateChromosomes();
            CreateNewGeneration(chromosomesList);
        }

        /// <summary>
        /// Generates a list of chromosomes to make up the inital generation.
        /// See <see cref="CreateInitialGeneration"/>
        /// </summary>
        protected abstract IList<IChromosome> GenerateChromosomes();

        /// <summary>
        /// Creates a new generation.
        /// </summary>
        /// <param name="chromosomes">The chromosomes for new generation.</param>
        public virtual void CreateNewGeneration(IList<IChromosome> chromosomes)
        {
            chromosomes.ValidateGenes();   // validate 

            var distinct = chromosomes.SelectDistinct();  // select distinct

            // Make sure that chromosomes without fitness are unique in terms of
            // same gene values sequence has not been encountered in previous generations ->
            if (GenerationsNumber > 0)
            {
                distinct.RemoveRepeating(Generations);
            }
            
            // Create new Generation ->
            CurrentGeneration = new Generation(++GenerationsNumber, chromosomes);

            // Add to collection ->
            Generations.Add(CurrentGeneration);
        }

        /// <summary>
        /// Assigns the best chromosome.
        /// </summary>
        public virtual void RegisterTheBestChromosome()
        {
            // Select the generation's best chromosome ->
            CurrentGeneration.BestChromosome = CurrentGeneration.Chromosomes.First();

            // If there is no better chromosome rather then existing then return ->
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