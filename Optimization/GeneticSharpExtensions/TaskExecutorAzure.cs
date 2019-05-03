using System;
using GeneticSharp.Infrastructure.Framework.Threading;

namespace Optimization
{
    /// <summary>
    /// An ITaskExecutor's implementation that executes the tasks in Azure Batch.
    /// </summary>
    class TaskExecutorAzure : ITaskExecutor
    {
        public TimeSpan Timeout { get; set; }
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Add the specified task to be executed.
        /// </summary>
        /// <param name="task">The task.</param>
        public void Add(Action task)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clear all the tasks.
        /// </summary>
        public void Clear()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>If has reach the timeout false, otherwise true.</returns>
        public bool Start()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Stops the tasks execution.
        /// </summary>
        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
