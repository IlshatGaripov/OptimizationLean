using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace Optimization.RunnerAppAzure
{ 
    /// <summary>
    /// Class that runs QC algorithm on a node in Azure batch.
    /// </summary>
    public static class AzureRunner
    {
        /// <summary>
        /// Custom Lean's result handler
        /// </summary>
        public static OptimizerResultHandler ResultHandler;

        /// <summary>
        /// Method performs necessary initialization and starts and algorithm inside Lean Engine.
        /// </summary>
        public static Dictionary<string, decimal> Run(Dictionary<string, string> inputs)
        {
            // Set the algorithm input variables
            foreach (var pair in inputs)
            {
                Config.Set(pair.Key, pair.Value);
            }

            // Common settings:
            Config.Set("environment", "backtesting");
            Config.Set("algorithm-language", "CSharp");     // omitted?
            Config.Set("result-handler", nameof(OptimizerResultHandler));   //override default result handler

            // Log file location
            var logFileName = Config.Get("log-file");
            Log.LogHandler = new FileLogHandler(logFileName);

            // LeanEngineSystemHandlers
            LeanEngineSystemHandlers leanEngineSystemHandlers;
            try
            {
                leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            leanEngineSystemHandlers.Initialize();   // can this be omitted?

            var job = leanEngineSystemHandlers.JobQueue.NextJob(out var assemblyPath);

            if (job == null)
            {
                throw new Exception("Engine.Main(): Job was null.");
            }

            // LeanEngineSystemHandlers
            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            // Engine
            try
            {
                var liveMode = Config.GetBool("live-mode");
                var algorithmManager = new AlgorithmManager(liveMode);
                // can this be omitted?
                leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);
                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);
                engine.Run(job, algorithmManager, assemblyPath);
            }
            finally
            {
                // do not Acknowledge Job, clean up resources
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }

            // Results
            ResultHandler = (OptimizerResultHandler)leanEngineAlgorithmHandlers.Results;
            return ResultHandler.FullResults;
        }
    }
}
