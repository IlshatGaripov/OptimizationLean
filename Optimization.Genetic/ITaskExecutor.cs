using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Optimization.Genetic
{
    /// <summary>
    /// Defines a interface to a task executor.
    /// </summary>
    public interface ITaskExecutor
    {
        /// <summary>
        /// Gets or sets the timeout to execute the tasks.
        /// </summary>
        TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        bool IsRunning { get; }

        /// <summary>
        /// Add the specified task to be executed.
        /// </summary>
        /// <param name="task">The task.</param>
        void Add(Task task);

        /// <summary>
        /// Clear all the tasks.
        /// </summary>
        void Clear();

        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>If has reach the timeout false, otherwise true.</returns>
        void Start(IEnumerable<IChromosome> chromosomesWithNoFitness, IFitness fitnessFunction);

        /// <summary>
        /// Stops the tasks execution.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Stop", Justification = "there is no better name")]
        void Stop();
    }
}