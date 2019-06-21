﻿using System;
using QuantConnect.Logging;

namespace Optimization
{
    public static class Program
    {
        // The logger
        public static ILogHandler Logger = new ConsoleLogHandler();

        // Program wide config file object.
        public static OptimizerConfiguration Config;

        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            // Load the optimizer settings from config json
            Config = Exstensions.LoadConfigFromFile("optimization_local.json");

            // Make sure that start and end dates are existent
            if (Config.StartDate == null || 
                Config.EndDate == null ||
                Config.FitnessScore == null ||
                Config.WalkForwardConfiguration == null)
            {
                throw new ArgumentException("Check that all required config variables are not defined ..");
            }

            try
            {
                // Required pre-settings
                DeployResources();

                // Init and start an optimization manager ->
                if (Config.WalkForwardConfiguration.Enabled == true)
                {
                    var wfoManager = new WalkForwardOptimizationManager
                    {
                        StartDate = Config.StartDate,
                        EndDate = Config.EndDate,
                        SortCriteria = Config.FitnessScore,
                        WalkForwardConfiguration = Config.WalkForwardConfiguration
                    };

                    wfoManager.OneEvaluationStepCompleted += WfoStepCompleted;

                    // Start it ->
                    wfoManager.Start();
                }
                else
                {
                    var easyManager = new AlgorithmOptimumFinder(Config.StartDate.Value, Config.EndDate.Value, Config.FitnessScore.Value);

                    // Start an optimization ->
                    easyManager.Start();
                }
                
                // Сomplete the life cycle of objects have been created in deployment phase ->
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
            // Set up App Domain settings no matter what computation mode will be used - local PC or a cloud ->
            OptimizerAppDomainManager.Initialize();

            // Computation mode specific settings ->
            switch (Program.Config.TaskExecutionMode)
            {
                // Deploy Batch resources if calculations are made using cloud compute powers
                case TaskExecutionMode.Azure:
                    AzureBatchManager.DeployAsync().Wait();
                    break;
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
            }
            
            // -2- Release AppDomain
            OptimizerAppDomainManager.Release();
        }

        /// <summary>
        /// Called at the end of iterative step of walk forward optimization
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void WfoStepCompleted(object sender, WalkForwardEventArgs e)
        {
            Logger.Trace(" >> VALIDATION COMPLETED >> \n");

            Logger.Trace($"Sharpe in-sample: {e.InSampleBestResults["SharpeRatio"]} " +
                         $"Sharpe validation: {e.ValidationResults["SharpeRatio"]}");
        }

    }
}