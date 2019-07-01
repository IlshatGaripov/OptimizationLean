using GeneticSharp.Domain.Chromosomes;
using System;

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
        /// Genes configuration array
        /// </summary>
        public GeneConfiguration[] GeneConfigurationArray { get; set; }

        /// <summary>
        /// Unique chromosome id.
        /// </summary>
        public string Id { get; set; } 

        /// <summary>
        /// Full backtest result for a certain period of time, from start till end date
        /// </summary>
        public FitnessResult FitnessResult { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="configArray">Gene configurations array</param>
        /// <param name="length">Chromosome length</param>
        public Chromosome(GeneConfiguration[] configArray, int length) : base(length)
        {
            GeneConfigurationArray = configArray;
            AssignUniqueId();
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
            // Deep clone ->
            var memStream = Exstensions.SerializeToStream(this);
            var copyObject = (ChromosomeRandom)Exstensions.DeserializeFromStream(memStream);

            // Assign new unique id ->
            copyObject.AssignUniqueId();

            return copyObject;
        }

        /// <summary>
        /// Assigns unique id to this object.
        /// </summary>
        public void AssignUniqueId()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }
}
