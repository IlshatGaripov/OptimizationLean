using System;

namespace Optimization
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

    /// <summary>
    /// Event args class passed to <see cref="GeneticAlgorithmCustom"/> TerminationReached event
    /// </summary>
    public class TerminationReachedEventArgs : EventArgs
    {
        /// <summary>
        /// Population that contains information of all generations of GA.
        /// </summary>
        public PopulationBase Pupulation { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TerminationReachedEventArgs(PopulationBase population)
        {
            Pupulation = population;
        }
    }

    /// <summary>
    /// Event args class passed to <see cref="GeneticAlgorithmCustom"/> GenerationRan event
    /// </summary>
    public class GenerationRanEventArgs : EventArgs
    {
        /// <summary>
        /// Final Generation that ran.
        /// </summary>
        public Generation Generation;

        /// <summary>
        /// Constructor
        /// </summary>
        public GenerationRanEventArgs(Generation generation)
        {
            Generation = generation;
        }
    }
}
