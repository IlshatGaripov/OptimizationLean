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

            // **
            // TODO: Check the Config object values consistency - VALIDATION() - for that all required information is present!
            // **

            // We may need to set up the computation resources
            DeployResources();

            try
            {
                // GA manager
                var manager = new AlgorithmOptimumFinder(Config.StartDate, Config.EndDate, Config.FitnessScore);

                // Subscribe to events
                manager.GenAlgorithm.GenerationRan += GenerationRan;
                manager.GenAlgorithm.TerminationReached += TerminationReached;

                // Start optimization
                manager.Start();

                // Release
                ReleaseDeployedResources();

                Console.WriteLine("Press ENTER to exit the program");
                Console.ReadKey(); 
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
            // -1- If Azure - Clean up Batch resources
            if (Program.Config.TaskExecutionMode == TaskExecutionMode.Azure)
            {
                AzureBatchManager.FinalizeAsync().Wait();
            }

            // -2- Shutdown the logger
            LogManager.Shutdown();
        }

        /// <summary>
        /// Handler called by the end of optimization algorithm
        /// </summary>
        public static void TerminationReached(object sender, EventArgs e)
        {
            GenerationRan(null, null);

            Program.Logger.Info("Termination Reached.");
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        public static void GenerationRan(object sender, EventArgs e)
        {

        }

    }
}