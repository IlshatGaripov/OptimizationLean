using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace Optimization.Base
{
    /// <summary>
    /// Class responsible for running the algorithm with Lean Engine.
    /// </summary>
    public class Runner : MarshalByRefObject
    {
        /// <summary>
        /// Custom Lean's result handler
        /// </summary>
        private OptimizerResultHandler _resultsHandler;

        /// <summary>
        /// Method performs necessary initialization and starts and algorithm inside Lean Engine.
        /// </summary>
        public Dictionary<string, decimal> Run(Dictionary<string, string> alorithmInputs)
        {
            // Chromosome id must be there
            var id = alorithmInputs["chromosome-id"];

            // Set algorithm input variables
            foreach (var pair in alorithmInputs.Where(i => i.Key != "chromosome-id"))
            {
                Config.Set(pair.Key, pair.Value);
            }

            // Lean general settings
            Config.Set("environment", "backtesting");
            Config.Set("algorithm-language", "CSharp");
            Config.Set("result-handler", nameof(OptimizerResultHandler));   //override default result handler

            // Create uniquely named log file for the backtest
            var dirPath = Path.Combine(Directory.GetCurrentDirectory(), "OptimizationLogs");
            var logFileName = "log" + "_" + id + ".txt";
            var filePath = Path.Combine(dirPath, logFileName);

            // Create directory if not exist
            System.IO.Directory.CreateDirectory(dirPath);

            Log.LogHandler = new FileLogHandler(filePath);

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

            // Setup packeting, queue and controls system: These don't do much locally.
            leanEngineSystemHandlers.Initialize();

            // Pull job from QuantConnect job queue, or, pull local build:
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
                var algorithmManager = new AlgorithmManager(false, job);
                leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);
                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, false);
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

            //  Results
            _resultsHandler = (OptimizerResultHandler)leanEngineAlgorithmHandlers.Results;
            return _resultsHandler.FullResults;
        }
    }
}
