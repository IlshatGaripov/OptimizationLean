using System.Linq;

namespace Optimization.Genetic
{
    /// <summary>
    /// An termination where you can combine others ITerminations with a OR logical operator.
    /// </summary>
    public class LogicalOrTermination : LogicalOperatorTerminationBase
    {
        public LogicalOrTermination(int minOperands = 0)
            : base(minOperands)
        {
        }

        /// <summary>
        /// Determines whether the specified geneticAlgorithm reached the termination condition.
        /// </summary>
        /// <param name="geneticAlgorithm">The genetic algorithm.</param>
        protected override bool PerformHasReached(IGeneticAlgorithm geneticAlgorithm)
        {
            return Terminations.Any(t => t.HasReached(geneticAlgorithm));
        }
    }
}
