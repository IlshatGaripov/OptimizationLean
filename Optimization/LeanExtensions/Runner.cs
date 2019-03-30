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
    public interface IRunner
    {
        Dictionary<string, decimal> Run(Dictionary<string, object> items);
    }

    public class Runner : MarshalByRefObject, IRunner
    {

        private OptimizerResultHandler _resultsHandler;
        private string _id;

        public Dictionary<string, decimal> Run(Dictionary<string, object> items)
        {
            Dictionary<string, Dictionary<string, decimal>> results = OptimizerAppDomainManager.GetResults(AppDomain.CurrentDomain);

            _id = (items.ContainsKey("Id") ? items["Id"] : Guid.NewGuid().ToString("N")).ToString();

            if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
            {
                if (!items.ContainsKey("startDate")) { items.Add("startDate", Program.Config.StartDate); }
                if (!items.ContainsKey("endDate")) { items.Add("endDate", Program.Config.EndDate); }
            }

            string jsonKey = JsonConvert.SerializeObject(items.Where(i => i.Key != "Id"));

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

            LaunchLean();

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

            if (!string.IsNullOrEmpty(Program.Config.TransactionLog))
            {
                var filename = Program.Config.TransactionLog;
                filename = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, 
                    Path.GetFileNameWithoutExtension(filename) + _id + Path.GetExtension(filename));

                Config.Set("transaction-log", filename);
            }

            //transaction-log

            Config.Set("api-handler", nameof(EmptyApiHandler));
            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            systemHandlers.Initialize();

            //separate log uniquely named
            var logFileName = "log" + DateTime.Now.ToString("yyyyMMddssfffffff") + "_" + _id + ".txt";

            using (Log.LogHandler = new FileLogHandler(logFileName, true))
            {
                LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
                try
                {
                    //override config to use custom result handler
                    Config.Set("backtesting.result-handler", nameof(OptimizerResultHandler));
                    leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
                    _resultsHandler = (OptimizerResultHandler)leanEngineAlgorithmHandlers.Results;
                }
                catch (CompositionException compositionException)
                {
                    Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                    throw;
                }

                AlgorithmNodePacket job = systemHandlers.JobQueue.NextJob(out var algorithmPath);

                try
                {
                    var engine = new Engine(systemHandlers, leanEngineAlgorithmHandlers, false);
                    var algorithmManager = new AlgorithmManager(false);
                    engine.Run(job, algorithmManager, algorithmPath);
                }
                finally
                {
                    Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                    // clean up resources
                    systemHandlers.Dispose();
                    leanEngineAlgorithmHandlers.Dispose();
                }
            }
        }

    }
}
