using System;

namespace Optimization.Launcher
{
    public static class Program
    {
        // -- MAIN --
        public static void Main()
        {
            // Make sure that start and end dates are specified ->
            if (Shared.Config.StartDate == null ||
                Shared.Config.EndDate == null ||
                Shared.Config.FitnessScore == 0 ||
                Shared.Config.WalkForwardConfiguration == null)
            {
                throw new ArgumentException("Please check that all required config variables are defined ..");
            }

            // initialize resources depending on task execution mode
            DeployResources();

            if (Shared.Config.WalkForwardConfiguration.Enabled == true)
            {
                var wfoManager = new WalkForwardOptimizationManager
                {
                    StartDate = Shared.Config.StartDate,
                    EndDate = Shared.Config.EndDate,
                    FitnessScore = Shared.Config.FitnessScore,
                    WalkForwardConfiguration = Shared.Config.WalkForwardConfiguration
                };

                // register event and start
                wfoManager.ValidationCompleted += CompareResults;
                wfoManager.Start();
            }
            else
            {
                // otherwise create regular optimizator
                var easyManager = new AlgorithmOptimumFinder(Shared.Config.StartDate.Value, 
                    Shared.Config.EndDate.Value, Shared.Config.FitnessScore);
                easyManager.Start();
            }

            // release earlier deployed execution resources
            ReleaseDeployedResources();


            Console.WriteLine();
            Console.WriteLine("Press any key to exit .. ");
            Console.ReadLine();
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
                    AppDomainManager.Initialize();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Releases computation resources at the end of optimization routine
        /// </summary>
        public static void ReleaseDeployedResources()
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
                    AppDomainManager.Release();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Called at the end of iterative step of walk forward optimization
        /// </summary>
        public static void CompareResults(object sender, WalkForwardValidationEventArgs e)
        {
            Shared.Logger.Trace("Validation Comparsion");
            Shared.Logger.Trace($"{e.InsampleResults.Chromosome.Fitness} / {e.ValidationResults.Chromosome.Fitness}");
            Shared.Logger.Trace(" <->");
        }

    }
}