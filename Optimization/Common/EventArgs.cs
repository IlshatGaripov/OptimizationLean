using System;
using System.Collections.Generic;
using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    /// <summary>
    /// Event args wrapper for the variables to be passed to OneEvaluationStepCompleted event
    /// at <see cref="WalkForwardOptimizationManager"/>
    /// </summary>
    public class WalkForwardEventArgs : EventArgs
    {
        /// <summary>
        /// Best Chromosome
        /// </summary>
        public IChromosome Chromosome { get; set; }

        /// <summary>
        /// In sample start date
        /// </summary>
        public DateTime InsampleStartDate { get; set; }

        /// <summary>
        /// In sample end date
        /// </summary>
        public DateTime InsampleEndDate { get; set; }

        /// <summary>
        /// WFO validation start date
        /// </summary>
        public DateTime ValidationStartDate { get; set; }

        /// <summary>
        /// WFO validation end date
        /// </summary>
        public DateTime ValidationEndDate { get; set; }

        /// <summary>
        /// Dictionary contains full results of a chromosome that has best fitness results on in-sample
        /// </summary>
        public Dictionary<string, decimal> InSampleBestResults { get; set; }

        /// <summary>
        /// Dictionary contais fulls results of validation experiment
        /// </summary>
        public Dictionary<string, decimal> ValidationResults { get; set; }
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
