using System;
using System.Collections.Generic;
using Optimization.Base;

namespace Optimization.Genetic
{
    /// <summary>
    /// An ITaskExecutor's implementation that executes the tasks in a linear fashion.
    /// </summary>
    public class LinearTaskExecutor : TaskExecutorBase
    {
        /// <summary>
        /// Starts the tasks execution.
        /// </summary>
        /// <returns>If has reach the timeout false, otherwise true.</returns>
        public override void Start(IEnumerable<IChromosome> chromosomesWithNoFitness, IFitness fitnessFunction)
        {
            var startTime = DateTime.Now;

            // For each Tasks passed to excutor, 
            // run it one in linear way.
            foreach (var c in chromosomesWithNoFitness)
            {
                var task = fitnessFunction.EvaluateAsync(c);
                task.Wait();

                // If take more time expected on Timeout property,
                // tehn stop thre running.
                if (DateTime.Now - startTime > Timeout)
                {
                    Shared.Logger.Error("LinearTaskExecutor.Start: TimeOut Exceeded!");
                    return;
                }
            }

            IsRunning = false;
            Clear();
        }
    }
}