using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Optimization.Genetic
{
    /// <summary>
    /// An ITaskExecutor's implementation that executes the tasks in a parallel fashion.
    /// </summary>
    public class ParallelTaskExecutor : TaskExecutorBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelTaskExecutor"/> class.
        /// </summary>
        public ParallelTaskExecutor()
        {
            MinThreads = 2;
            MaxThreads = 10;
        }
   
        /// <summary>
        /// Gets or sets the minimum threads.
        /// </summary>
        /// <value>The minimum threads.</value>
        public int MinThreads { get; set; }

        /// <summary>
        /// Gets or sets the max threads.
        /// </summary>
        /// <value>The max threads.</value>
        public int MaxThreads { get; set; }

        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>If has reach the timeout false, otherwise true.</returns>
        public override void Start(IEnumerable<IChromosome> chromosomesWithNoFitness, IFitness fitnessFunction)
        {
            IsRunning = true;
            SetThreadPoolConfig(out int minWorker, out int minIOC, out int maxWorker, out int maxIOC);

            try
            {
                foreach (var c in chromosomesWithNoFitness)
                {
                    Add(fitnessFunction.EvaluateAsync(c));
                }

                // Need to verify, because TimeSpan.MaxValue passed to Task.WaitAll throws a System.ArgumentOutOfRangeException.
                if (Timeout == TimeSpan.MaxValue)
                {
                    Task.WaitAll(Tasks.ToArray());
                    return;
                }

                Task.WaitAll(Tasks.ToArray(), Timeout);
            }
            finally
            {
                // reset pool and clear the tasks
                IsRunning = false;
                Clear();
                ResetThreadPoolConfig(minWorker, minIOC, maxWorker, maxIOC);
            }
        }

        /// <summary>
        /// Configure the ThreadPool min and max threads number to the define on this instance properties.
        /// </summary>
        /// <param name="minWorker">Minimum worker.</param>
        /// <param name="minIOC">Minimum ioc.</param>
        /// <param name="maxWorker">Max worker.</param>
        /// <param name="maxIOC">Max ioc.</param>
        protected void SetThreadPoolConfig(out int minWorker, out int minIOC, out int maxWorker, out int maxIOC)
        {
            // Do not change values if the new values to min and max threads are lower than already configured on ThreadPool.
            ThreadPool.GetMinThreads(out minWorker, out minIOC);

            if (MinThreads > minWorker)
            {
                ThreadPool.SetMinThreads(MinThreads, minIOC);
            }

            ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);

            if (MaxThreads > maxWorker)
            {
                ThreadPool.SetMaxThreads(MaxThreads, maxIOC);
            }
        }

        /// <summary>
        /// Rollback ThreadPool previous min and max threads configuration.
        /// </summary>
        /// <param name="minWorker">Minimum worker.</param>
        /// <param name="minIOC">Minimum ioc.</param>
        /// <param name="maxWorker">Max worker.</param>
        /// <param name="maxIOC">Max ioc.</param>
        protected static void ResetThreadPoolConfig(int minWorker, int minIOC, int maxWorker, int maxIOC)
        {
            ThreadPool.SetMinThreads(minWorker, minIOC);
            ThreadPool.SetMaxThreads(maxWorker, maxIOC);
        }
    }
}