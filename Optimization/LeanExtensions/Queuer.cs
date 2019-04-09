using Newtonsoft.Json;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Packets;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;

namespace Optimization
{

    public static class EngineContext
    {
        public static LeanEngineSystemHandlers SystemHandlers;
        public static Engine Engine;
        public static LeanEngineAlgorithmHandlers AlgorithmHandlers;
        public static Queuer Queuer;
        public static AppDomain AppDomain;

    }

    public class Queuer : MarshalByRefObject
    {

        private OptimizerResultHandler _resultsHandler;
        private string _id;    

        public Dictionary<string, decimal> Run(Dictionary<string, object> items)
        {
            Dictionary<string, Dictionary<string, decimal>> results = OptimizerAppDomainManager.GetResults(AppDomain.CurrentDomain);

            _id = (items.ContainsKey("Id") ? items["Id"] : Guid.NewGuid()).ToString();

            if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
            {
                if (!items.ContainsKey("startDate")) { items.Add("startDate", Program.Config.StartDate); }
                if (!items.ContainsKey("endDate")) { items.Add("endDate", Program.Config.EndDate); }
            }

            string jsonKey = JsonConvert.SerializeObject(items);

            if (results.ContainsKey(jsonKey))
            {
                return results[jsonKey];
            }

            //just ignore id gene
            foreach (var pair in items.Where(i => i.Key != "Id"))
            {
                if (pair.Value is DateTime?)
                {
                    var cast = ((DateTime?)pair.Value);
                    if (cast.HasValue)
                    {
                        Config.Set(pair.Key, cast.Value.ToString("O"));
                    }
                }
                else
                {
                    Config.Set(pair.Key, pair.Value.ToString());
                }
            }

            if (EngineContext.SystemHandlers == null || EngineContext.Engine == null)
            {
                LaunchLean();
            }

            RunJob();

            if (_resultsHandler.FullResults != null && _resultsHandler.FullResults.Any())
            {
                results.Add(jsonKey, _resultsHandler.FullResults);
                OptimizerAppDomainManager.SetResults(AppDomain.CurrentDomain, results);
            }

            return _resultsHandler.FullResults;
        }

        private void LaunchLean()
        {
            Config.Set("environment", "backtesting");
            Config.Set("forward-console-messages", "false");

            if (!string.IsNullOrEmpty(Program.Config.AlgorithmTypeName))
            {
                Config.Set("algorithm-type-name", Program.Config.AlgorithmTypeName);
            }

            if (!string.IsNullOrEmpty(Program.Config.AlgorithmLocation))
            {
                Config.Set("algorithm-location", Path.GetFileName(Program.Config.AlgorithmLocation));
            }

            if (!string.IsNullOrEmpty(Program.Config.DataFolder))
            {
                Config.Set("data-folder", Program.Config.DataFolder);
            }

            Config.Set("api-handler", nameof(EmptyApiHandler));

            //override config to use custom result handler
            Config.Set("backtesting.result-handler", nameof(OptimizerResultHandler));

            //separate log uniquely named
            var logFileName = "log" + DateTime.Now.ToString("yyyyMMddssfffffff") + "_" + _id + ".txt";

            using (Log.LogHandler = new FileLogHandler(logFileName, true))
            {
                RunJob();
            }
        }

        public void RunJob()
        {
            try
            {
                EngineContext.SystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
                EngineContext.SystemHandlers.Initialize();              
                EngineContext.AlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
                _resultsHandler = (OptimizerResultHandler)EngineContext.AlgorithmHandlers.Results;
                EngineContext.Engine = new Engine(EngineContext.SystemHandlers, EngineContext.AlgorithmHandlers, false);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            AlgorithmNodePacket job = EngineContext.SystemHandlers.JobQueue.NextJob(out var algorithmPath);

            EngineContext.Engine.Run(job, new AlgorithmManager(false), algorithmPath);
            Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);
        }

    }
}
