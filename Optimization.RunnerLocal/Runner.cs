using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace Optimization.RunnerLocal
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
            // Chromosome id must be there ->
            var id = alorithmInputs["chromosome-id"];

            // Set algorithm input variables ->
            foreach (var pair in alorithmInputs.Where(i => i.Key != "chromosome-id"))
            {
                Config.Set(pair.Key, pair.Value);
            }

            // Lean general settings ->
            Config.Set("environment", "backtesting");
            Config.Set("algorithm-language", "CSharp");     // omitted?
            Config.Set("result-handler", nameof(OptimizerResultHandler));   //override default result handler

            // Separate log uniquely named
            var dirPath = $"C:/Users/ilshat/Desktop/logs/{DateTime.Now:yyyy-MM-dd}/leanLogs/";
            var logFileName = "log" + DateTime.Now.ToString("yyyyMMddssfffffff") + "_" + id + ".txt";
            var filePath = String.Concat(dirPath, logFileName);

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

            //  Results
            _resultsHandler = (OptimizerResultHandler)leanEngineAlgorithmHandlers.Results;
            return _resultsHandler.FullResults;
        }
    }
}
