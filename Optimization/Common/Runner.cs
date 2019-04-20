using System;
using System.Collections.Generic;
using System.IO;
using System.ComponentModel.Composition;
using System.Linq;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace Optimization
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

            // get current AppDomain for the Thread executing this
            var currentAppDomain = AppDomain.CurrentDomain;

            // obtain a global program config through the property
            var globalConfigCopy = (OptimizerConfiguration)currentAppDomain.GetData("Configuration");

            // set the algorithm input variables. 
            foreach (var pair in alorithmInputs.Where(i => i.Key != "Id"))
            {
                // represent datetime in lean-friendly format. example: 2009-06-15
                if (pair.Value is DateTime time)
                {
                    var cast = (DateTime?) time;
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

            // Algorithm name
            if (!string.IsNullOrEmpty(globalConfigCopy.AlgorithmTypeName))
            {
                Config.Set("algorithm-type-name", globalConfigCopy.AlgorithmTypeName);
            }

            // Physical location of dll with an algorithm.
            if (!string.IsNullOrEmpty(globalConfigCopy.AlgorithmLocation))
            {
                Config.Set("algorithm-location", Path.GetFileName(globalConfigCopy.AlgorithmLocation));
            }

            // Data folder
            if (!string.IsNullOrEmpty(globalConfigCopy.DataFolder))
            {
                Config.Set("data-folder", globalConfigCopy.DataFolder);
            }

            Log.LogHandler = new CompositeLogHandler(
                new ConsoleLogHandler(), 
                new FileLogHandler("C:/Users/sterling/Desktop/logLean.txt")
                );

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
