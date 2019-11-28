using System;
using Optimization.Base;

namespace Optimization.Launcher
{
    public static class Program
    {
        // -- MAIN --
        public static void Main()
        {
            try
            {
                // some initialization before the start, that will depend on task execution mode chosen
                DeployResources();

                if (Shared.Config.WalkingForward.Enabled)
                {
                    var wfoManager = new WalkForwardOptimizationManager(Shared.Config.StartDate,
                        Shared.Config.EndDate,
                        Shared.Config.FitnessScore,
                        Shared.Config.FitnessFilter.Enabled) { WalkForwardConfiguration = Shared.Config.WalkingForward};

                    // register event callback
                    wfoManager.ValidationCompleted +=
                        (sender, vdarg) => {
                            Shared.Logger.Trace("Validation Comparsion");
                            Shared.Logger.Trace($"{vdarg.InsampleResults.Chromosome.Fitness} / {vdarg.ValidationResults.Chromosome.Fitness}");
                            Shared.Logger.Trace(" <->");
                        };

                    // and launch
                    wfoManager.Start();
                }
                else
                {
                    // otherwise create regular optimizator
                    var easyManager = new AlgorithmOptimumFinder(
                        Shared.Config.StartDate,
                        Shared.Config.EndDate, 
                        Shared.Config.FitnessScore,
                        Shared.Config.FitnessFilter.Enabled);

                    easyManager.Start();
                }

                // release earlier deployed resources
                ReleaseResources();


                Console.WriteLine();
                Console.WriteLine("Press any key to exit .. ");
                Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Inits computation resources
        /// </summary>
        public static void DeployResources()
        {
            // Computation mode specific settings: azure or app domain ?
            switch (Shared.Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Azure:
                    AzureBatchManager.DeployAsync().Wait();
                    break;
                case TaskExecutionMode.Linear:
                case TaskExecutionMode.Parallel:
                    AppDomainRunner.Initialize();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Releases the computation resources at the end of optimization routine
        /// </summary>
        public static void ReleaseResources()
        {
            // -1- Clean up Task Execution resources
            switch (Shared.Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Azure:
                    AzureBatchManager.ReleaseAsync().Wait();
                    break;
                case TaskExecutionMode.Linear:
                case TaskExecutionMode.Parallel:
                    // -2- Release AppDomain
                    AppDomainRunner.Release();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}