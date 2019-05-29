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
        /// Unique identifier. Well it's actually not in use currently.
        /// </summary>
        private string _id;

        /// <summary>
        /// Method performs necessary initialization and starts and algorithm inside Lean Engine.
        /// </summary>
        public Dictionary<string, decimal> Run(Dictionary<string, object> alorithmInputs)
        {
            // take chromosome's GUID if specified to initialize id variable
            _id = (alorithmInputs.ContainsKey("Id") ? alorithmInputs["Id"] : Guid.NewGuid().ToString("N")).ToString();

            // set the algorithm input variables. 
            foreach (var pair in alorithmInputs.Where(i => i.Key != "Id"))
            {
                // represent datetime in lean-friendly format. example: 2009-06-15
                if (pair.Value is DateTime time)
                {
                    var cast = (DateTime?)time;
                    Config.Set(pair.Key, cast.Value.ToString("O"));
                }
                else
                {
                    Config.Set(pair.Key, pair.Value.ToString());
                }
            }

            // Lean general settings:
            Config.Set("environment", "backtesting");
            Config.Set("algorithm-language", "CSharp");     // omitted?
            Config.Set("result-handler", nameof(OptimizerResultHandler));   //override default result handler

            // Separate log uniquely named
            var dirPath = $"C:/Users/sterling/Desktop/logs/{DateTime.Now:yyyy-MM-dd}/leanLogs/";
            var logFileName = "log" + DateTime.Now.ToString("yyyyMMddssfffffff") + "_" + _id + ".txt";
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
