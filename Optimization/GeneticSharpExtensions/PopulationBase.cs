using System;
using System.Collections.Generic;
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
        /// Max number of chromosomes each generation to contain.
        /// </summary>
        public int GenerationMaxSize { get; set; } = 1000;

        /// <summary>
        /// Number of initial generations in a row without at least a single positive chromosome.
        /// </summary>
        public int FruitlessGenerationsCount { get; set; }

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
            Program.Logger.Trace("CreateInitialGeneration():");
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
            // same gene values sequence wasn't encountered in previous generations ->
            if (GenerationsNumber > 0)
            {
                distinct.RemoveEvaluatedPreviously(Generations);
            }
            
            // Create new Generation from distinct (!) ->
            CurrentGeneration = new Generation(++GenerationsNumber, distinct);

            // Add to collection ->
            Generations.Add(CurrentGeneration);
        }

        /// <summary>
        /// Runs when all generation chromosomes got evaluated
        /// </summary>
        public virtual void OnEvaluationCompleted()
        {
            // Leave only the values that have positive fitness and order by descending ->
            var chromosomes = CurrentGeneration.Chromosomes
                .Where(c => c.Fitness != null && c.Fitness.Value > 0)
                .OrderByDescending(c => c.Fitness.Value)
                .ToList();

            // If no any positive fitness chromosome in collection ->
            if (!chromosomes.Any())
            {
                Program.Logger.Trace(" <->");
                Program.Logger.Error("WARNING: Generation has no single acceptable solution!!");

                // CurrentGeneration is an empty list
                CurrentGeneration.Chromosomes = new List<IChromosome>();

                // Is fruiteless ->
                CurrentGeneration.IsFruitless = true;
                FruitlessGenerationsCount++;
                return;
            }

            // Truncate if amount is more than max allowed size ->
            if (chromosomes.Count > GenerationMaxSize)
            {
                chromosomes = chromosomes.Take(GenerationMaxSize).ToList();
            }

            CurrentGeneration.Chromosomes = chromosomes;

            // Select the generation's best chromosome ->
            CurrentGeneration.BestChromosome = CurrentGeneration.Chromosomes.First();

            // If there is no better chromosome then exist then return ->
            if (Equals(BestChromosome, CurrentGeneration.BestChromosome))
            {
                return;
            }

            // Otherwise assign new best and raise event ->
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