using System;
using QuantConnect.Logging;

namespace Optimization
{
    public static class Program
    {
        // Program wide configuration object ->s
        public static OptimizerConfiguration Config = Exstensions.LoadConfigFromFile("optimization_local.json");

        // The logger ->
        public static ILogHandler Logger = 
            new CompositeLogHandler(
            new ConsoleLogHandler(), 
            new FileLogHandler(filepath: Config.LogFile)); 

        /// <summary>
        /// Main is main
        /// </summary>
        public static void Main()
        {
            // Make sure that start and end dates are specified ->
            if (Config.StartDate == null || 
                Config.EndDate == null ||
                Config.FitnessScore == 0 ||
                Config.WalkForwardConfiguration == null)
            {
                throw new ArgumentException("Please check that all required config variables are defined ..");
            }

            // Create resources ->
            DeployResources();

            // Init and start the optimization manager ->
            if (Config.WalkForwardConfiguration.Enabled == true)
            {
                var wfoManager = new WalkForwardOptimizationManager
                {
                    StartDate = Config.StartDate,
                    EndDate = Config.EndDate,
                    FitnessScore = Config.FitnessScore,
                    WalkForwardConfiguration = Config.WalkForwardConfiguration
                };

                wfoManager.ValidationCompleted += CompareResults;

                // Start it ->
                wfoManager.Start();
            }
            else
            {
                var easyManager = new AlgorithmOptimumFinder(Config.StartDate.Value, Config.EndDate.Value, Config.FitnessScore);

                // Start an optimization ->
                easyManager.Start();
            }

            // Сomplete the life cycle of objects have been created in deployment phase ->
            ReleaseDeployedResources();

            Console.WriteLine();
            Console.WriteLine("Press ENTER to exit the program");
            Console.ReadLine();
        }


        /// <summary>
        /// Inits computation resources
        /// </summary>
        public static void DeployResources()
        {
            try
            {
                // Set up App Domain settings no matter what computation mode will be used - local PC or a cloud ->
                AppDomainManager.Initialize();

                // Computation mode specific settings ->
                switch (Config.TaskExecutionMode)
                {
                    // Deploy Batch resources if calculations are made using cloud compute powers
                    case TaskExecutionMode.Azure:
                        AzureBatchManager.DeployAsync().Wait();
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        /// <summary>
        /// Releases computation resources at the end of optimization routine
        /// </summary>
        public static void ReleaseDeployedResources()
        {
            // -1- Clean up Task Execution resources
            switch (Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Azure:
                    AzureBatchManager.FinalizeAsync().Wait();
                    break;
            }

            // -2- Release AppDomain
            AppDomainManager.Release();
        }

        /// <summary>
        /// Called at the end of iterative step of walk forward optimization
        /// </summary>
        public static void CompareResults(object sender, WalkForwardValidationEventArgs e)
        {

        }

    }
}