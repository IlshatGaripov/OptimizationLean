using GeneticSharp.Domain.Chromosomes;
using System;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Base class for all custom implementations of a chromosome class.
    /// Intermediate between ChromosomeBase (library implementation) and factual implementation.
    /// </summary>
    [Serializable]
    public class Chromosome : ChromosomeBase
    {
        /// <summary>
        /// Unique chromosome id.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Full backtest result for a certain period of time, from start till end date
        /// </summary>
        public FitnessResult FitnessResult { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Chromosome(int length) : base(length)
        {
            
        }
        
        /// <summary>
        /// Generates the gene for the specified index.
        /// </summary>
        /// <param name="geneIndex">Gene index.</param>
        /// <returns>The gene generated at the specified index.</returns>
        public override Gene GenerateGene(int geneIndex)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Returns a deep clone copy of a chromosome object.
        /// </summary>
        /// <returns>The new chromosome.</returns>
        public override IChromosome CreateNew()
        {
            throw new System.NotImplementedException();
        }
    }

    /// <summary>
    /// Full backtest result are defined in simpliest by full statistics and start and end dates
    /// </summary>
    [Serializable]
    public class FitnessResult
    {
        /// <summary>
        /// Experiment Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Experiment End Date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Full backtest results directionay returned by OptimizerResultHandler
        /// </summary>
        public Dictionary<string, decimal> FullResults { get; set; }
    }
}
