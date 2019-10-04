namespace Optimization.Genetic
{
    /// <summary>
    /// Cromosome representation for brute force algorithm mode.
    /// </summary>
    public sealed class ChromosomeCartesian: Chromosome
    {
        /// <summary>
        /// Array of gene values.
        /// </summary>
        private readonly object[] _geneValues;

        /// <summary>
        /// Constructor for the class. Pass a lenguth to constructor of a base class <see cref="ChromosomeBase"/>
        /// </summary>
        public ChromosomeCartesian(object[] geneValues, GeneConfiguration[] configArray) : base(configArray, geneValues.Length)
        {
            _geneValues = geneValues;

            // fill the gene array with generated values
            for (var i = 0; i < Length; i++)
            {
                ReplaceGene(i, GenerateGene(i));
            }
        }

        /// <summary>
        /// Generates the gene for the specified index.
        /// </summary>
        public override Gene GenerateGene(int geneIndex)
        {
            return new Gene(_geneValues[geneIndex]);
        }
    }
}
