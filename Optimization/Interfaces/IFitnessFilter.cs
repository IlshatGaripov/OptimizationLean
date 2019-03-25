using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// Construction to screen out the insignificant results of evaluation.
    /// </summary>
    public interface IFitnessFilter
    {
        /// <summary>
        /// Determines whether or not the fitness result is significant.
        /// </summary>
        bool IsSuccess(Dictionary<string, decimal> result, OptimizerFitness fitness);
    }
}