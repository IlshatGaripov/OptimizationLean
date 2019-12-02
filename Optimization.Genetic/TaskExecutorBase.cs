using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Optimization.Genetic
{
    /// <summary>
    /// Task executor base.
    /// </summary>
    public abstract class TaskExecutorBase : ITaskExecutor
    {
        private readonly object m_lock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskExecutorBase"/> class.
        /// </summary>
        protected TaskExecutorBase()
        {
            Tasks = new List<Task>();
            Timeout = TimeSpan.MaxValue;
        }

        /// <summary>
        /// Gets or sets the timeout to execute the tasks.
        /// </summary>
        /// <value>The timeout.</value>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is running.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is running; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Gets the tasks.
        /// </summary>
        protected List<Task> Tasks { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="TaskExecutorBase"/> stop requested.
        /// </summary>
        protected bool StopRequested { get; private set; }
  
        /// <summary>
        /// Add the specified task to be executed.
        /// </summary>
        /// <param name="task">The task.</param>
        public void Add(Task task)
        {
            Tasks.Add(task);
        }

        /// <summary>
        /// Clear all the tasks.
        /// </summary>
        public void Clear()
        {
            Tasks.Clear();
        }

        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>If has reach the timeout false, otherwise true.</returns>
        public abstract void Start(IEnumerable<IChromosome> chromosomesWithNoFitness, IFitness fitnessFunction);

        /// <summary>
        /// Stops the tasks execution.
        /// </summary>
        public virtual void Stop()
        {
            lock (m_lock)
            {
                StopRequested = true;
            }
        }
     }
}