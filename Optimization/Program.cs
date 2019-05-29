using System;
using NLog;

namespace Optimization
{
    public static class Program
    {
        // The logger
        public static Logger Logger = LogManager.GetLogger("optimizer");

        // Program wide config file object.
        public static OptimizerConfiguration Config;

        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            // Load the optimizer settings from config json
            Config = Exstensions.LoadConfigFromFile("optimization_local.json");

            // Assert that start and end dates are speficied
            if (Config.StartDate == null || Config.EndDate == null)
            {
                throw new ArgumentException("Time limits for test are not defined");
            }

            try
            {
                // Required pre-settings
                DeployResources();

                // GA manager
                var manager = new AlgorithmOptimumFinder(Config.StartDate.Value, Config.EndDate.Value, Config.FitnessScore);

                // Subscribe to GA events
                manager.GenAlgorithm.GenerationRan += GenerationRan;
                manager.GenAlgorithm.TerminationReached += TerminationReached;

                // Start an optimization
                manager.Start();

                // Сomplete the life cycle of objects have been created in deployment phase
                ReleaseDeployedResources();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit the program");
            Console.ReadLine();
        }


        /// <summary>
        /// Inits computation resources
        /// </summary>
        public static void DeployResources()
        {
            switch (Program.Config.TaskExecutionMode)
            {
                // Deploy Batch resources if computations are to be made using cloud compute powers
                case TaskExecutionMode.Azure:
                    AzureBatchManager.DeployAsync().Wait();
                    break;

                // Configure App Domain settings if calculations are planned to be handled using local PC powers
                case TaskExecutionMode.Linear:
                case TaskExecutionMode.Parallel:
                    OptimizerAppDomainManager.Initialize();
                    break;

                // Otherwise
                default:
                    throw new Exception("Execution mode is not precise");
            }
        }

        /// <summary>
        /// Releases computation resources at the end of optimization routine
        /// </summary>
        public static void ReleaseDeployedResources()
        {
            // -1- Clean up Task Execution resources
            switch (Program.Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Azure:
                    AzureBatchManager.FinalizeAsync().Wait();
                    break;
                case TaskExecutionMode.Linear:
                case TaskExecutionMode.Parallel:
                    OptimizerAppDomainManager.Release();
                    break;
            }

            // -2- Shutdown the logger
            LogManager.Shutdown();
        }

        /// <summary>
        /// Handler called at the end of work of genetic algorithm
        /// </summary>
        public static void TerminationReached(object sender, EventArgs e)
        {
            Logger.Info("Termination Reached.");
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        public static void GenerationRan(object sender, EventArgs e)
        {

        }

    }
}