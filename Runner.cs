﻿using GeneticSharp.Domain.Chromosomes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using QuantConnect.Api;
using QuantConnect.Configuration;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine;
using QuantConnect.Lean.Engine.DataFeeds;
using QuantConnect.Lean.Engine.HistoricalData;
using QuantConnect.Lean.Engine.RealTime;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.Setup;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Logging;
using QuantConnect.Messaging;
using QuantConnect.Packets;
using QuantConnect.Queues;
using QuantConnect.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Optimization
{

    public class Runner : MarshalByRefObject
    {

        private BacktestingResultHandler _resultsHandler;

        public decimal Run(Dictionary<string, object> items)
        {
            string plain = string.Join(",", items.Select(s => s.Value));

            Dictionary<string, decimal> results = (Dictionary<string, decimal>)AppDomain.CurrentDomain.GetData("Results");
   
            if (results.ContainsKey(plain))
            {
                return results[plain];
            }

            foreach (var pair in items)
            {
                Config.Set(pair.Key, pair.Value.ToString());
            }

            LaunchLean();

            var sharpe = -10m;
            var ratio = _resultsHandler.FinalStatistics["Sharpe Ratio"];
            Decimal.TryParse(ratio, out sharpe);
            var compound = _resultsHandler.FinalStatistics["Compounding Annual Return"];
            decimal parsed;
            Decimal.TryParse(compound.Trim('%'), out parsed);

            sharpe = System.Math.Max(sharpe == 0 || parsed < 0 ? -10 : sharpe, -10);

            results.Add(plain, sharpe);
            AppDomain.CurrentDomain.SetData("Results", results);

            return sharpe;
        }

        private void LaunchLean()
        {
            Config.Set("environment", "backtesting");
            string algorithm = (string)AppDomain.CurrentDomain.GetData("AlgorithmTypeName");

            Config.Set("algorithm-type-name", algorithm);

            var systemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            systemHandlers.Initialize();

            Log.LogHandler = Composer.Instance.GetExportedValueByTypeName<ILogHandler>(Config.Get("log-handler", "CompositeLogHandler"));
            Log.DebuggingEnabled = false;
            Log.DebuggingLevel = 1;

            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
                _resultsHandler = (BacktestingResultHandler)leanEngineAlgorithmHandlers.Results;
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }
            string algorithmPath;
            AlgorithmNodePacket job = systemHandlers.JobQueue.NextJob(out algorithmPath);
            try
            {
                var _engine = new Engine(systemHandlers, leanEngineAlgorithmHandlers, Config.GetBool("live-mode"));
                _engine.Run(job, algorithmPath);
            }
            finally
            {
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);

                // clean up resources
                systemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }
        }

    }
}
