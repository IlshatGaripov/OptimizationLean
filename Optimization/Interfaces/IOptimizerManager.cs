using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <summary>
    /// ...
    /// </summary>
    public interface IOptimizerManager
    {
        /// <summary>
        /// ...
        /// </summary>
        void Initialize(IFitness fitness);

        /// <summary>
        /// ...
        /// </summary>
        void Start();
    }
}