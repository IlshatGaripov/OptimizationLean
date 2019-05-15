using System;
using System.Threading;
using System.Threading.Tasks;
using GeneticSharp.Infrastructure.Framework.Threading;

namespace Optimization
{
    /// <summary>
    /// An ITaskExecutor's implementation that executes the tasks that evaluate fitness  Azure Batch.
    /// </summary>
    public sealed class TaskExecutorAzure : TaskExecutorBase
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:GeneticSharp.Infrastructure.Framework.Threading.ParallelTaskExecutor"/> class.
        /// </summary>
        public TaskExecutorAzure()
        {
            MinThreads = 200;
            MaxThreads = 200;
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
        /// Gets or sets the cancellation token source.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; }

        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>True if successfully finished. False otherwise.</returns>
        public override bool Start()
        {
            // Set Thread Pool to guarantee the required number of threads
            SetThreadPoolConfig(out int minWorker, out int minIOC, out int maxWorker, out int maxIOC);

            CancellationTokenSource = new CancellationTokenSource();

            try
            {
                base.Start();

                var parallelOptions = new ParallelOptions
                {
                    // Set limit of parallel actions to the number of available azure nodes in a pool.
                    MaxDegreeOfParallelism = Program.Config.DedicatedNodeCount + Program.Config.LowPriorityNodeCount,

                    CancellationToken = CancellationTokenSource.Token
                };

                Parallel.ForEach(Tasks, parallelOptions, action => action());
                
                return true;
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            finally
            {
                ResetThreadPoolConfig(minWorker, minIOC, maxWorker, maxIOC);
                IsRunning = false;
            }
        }

        /// <summary>
        /// Stops the tasks execution.
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            IsRunning = false;

            //  Cancel and Dispose on CancellationTokenSource
            CancellationTokenSource?.Cancel();
            CancellationTokenSource?.Dispose();

        }

        /// <summary>
        /// Configure the ThreadPool min and max threads number to the define on this instance properties.
        /// </summary>
        /// <param name="minWorker">Minimum worker.</param>
        /// <param name="minIOC">Minimum ioc.</param>
        /// <param name="maxWorker">Max worker.</param>
        /// <param name="maxIOC">Max ioc.</param>
        public void SetThreadPoolConfig(out int minWorker, out int minIOC, out int maxWorker, out int maxIOC)
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
        public static void ResetThreadPoolConfig(int minWorker, int minIOC, int maxWorker, int maxIOC)
        {
            ThreadPool.SetMinThreads(minWorker, minIOC);
            ThreadPool.SetMaxThreads(maxWorker, maxIOC);
        }
    }
}
