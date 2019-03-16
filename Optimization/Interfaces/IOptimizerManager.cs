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
        void Initialize(IOptimizerConfiguration config, OptimizerFitness fitness);

        /// <summary>
        /// ...
        /// </summary>
        void Start();
    }
}