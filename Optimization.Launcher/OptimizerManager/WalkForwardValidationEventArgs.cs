using System;
using Optimization.Genetic;

namespace Optimization.Launcher
{
    /// <summary>
    /// Event args wrapper for the variables to be passed to OneEvaluationStepCompleted event
    /// at <see cref="WalkForwardOptimizationManager"/>
    /// </summary>
    public class WalkForwardValidationEventArgs : EventArgs
    {
        /// <summary>
        /// Collection of best in sample results
        /// </summary>
        public FitnessResult InsampleResults { get; set; }

        /// <summary>
        /// Corresponding collection of full results on validation data
        /// </summary>
        public FitnessResult ValidationResults { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="insamp">In sample fitness result</param>
        /// <param name="outsamp">Out of sample fitness result</param>
        public WalkForwardValidationEventArgs(FitnessResult insamp, FitnessResult outsamp)
        {
            InsampleResults = insamp;
            ValidationResults = outsamp;
        }
    }

    
}
