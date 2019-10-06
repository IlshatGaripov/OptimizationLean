using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QuantConnect;
using QuantConnect.Interfaces;
using QuantConnect.Lean.Engine.Results;
using QuantConnect.Lean.Engine.TransactionHandlers;
using QuantConnect.Orders;
using QuantConnect.Packets;
using QuantConnect.Statistics;

namespace Optimization.RunnerAppAzure
{
    public class OptimizerResultHandler : BaseResultsHandler, IResultHandler
    {
        private readonly IResultHandler _shadow;

        public Dictionary<string, decimal> FullResults { get; set; }

        public ConcurrentQueue<Packet> Messages
        {
            get => _shadow.Messages;
            set => _shadow.Messages = value;
        }

        public ConcurrentDictionary<string, Chart> Charts
        {
            get => _shadow.Charts;
            set => _shadow.Charts = value;
        }

        public TimeSpan ResamplePeriod => _shadow.ResamplePeriod;

        public TimeSpan NotificationPeriod => _shadow.NotificationPeriod;

        public bool IsActive => _shadow.IsActive;

        private bool _hasError;

        public OptimizerResultHandler()
        {
            _shadow = new BacktestingResultHandler();
        }

        public void SendFinalResult()
        {
            _shadow.SendFinalResult();

            if (_hasError)
            {
                FullResults = null;
                return;
            }

            // generate statistics
            var charts = new Dictionary<string, Chart>(Charts);
            var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);

            // need to invoke that again
            var statisticsResults = (StatisticsResults)_shadow.GetType().InvokeMember("GenerateStatisticsResults",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, _shadow,
                new object[] { charts, profitLoss });

            var performance = statisticsResults.TotalPerformance;
            var summary = statisticsResults.Summary;

            // convert to dictionary
            var dictionary = performance.PortfolioStatistics.GetType()
                .GetProperties()
                .ToDictionary(k => k.Name,
                    v => (decimal) v.GetValue(performance.PortfolioStatistics));

            // additional performance indicators
            dictionary.Add("TotalNumberOfTrades", int.Parse(summary["Total Trades"]));
            dictionary.Add("TotalFees", decimal.Parse(summary["Total Fees"].Substring(1)));

            // assign full results
            FullResults = dictionary;
        }

        #region Shadow Methods
        public void Initialize(AlgorithmNodePacket job, IMessagingHandler messagingHandler, IApi api, ITransactionHandler transactionHandler)
        {
            _shadow.Initialize(job, messagingHandler, api, transactionHandler);
        }

        public void Run()
        {
            _hasError = false;
            _shadow.Run();
        }

        public void DebugMessage(string message)
        {
            _shadow.DebugMessage(message);
        }

        public void SystemDebugMessage(string message)
        {
            _shadow.SystemDebugMessage(message);
        }

        public void SecurityType(List<SecurityType> types)
        {
            _shadow.SecurityType(types);
        }

        public void LogMessage(string message)
        {
            _shadow.LogMessage(message);
        }

        public void ErrorMessage(string error, string stacktrace = "")
        {
            _shadow.ErrorMessage(error, stacktrace);
        }

        public void RuntimeError(string message, string stacktrace = "")
        {
            _shadow.ErrorMessage(message, stacktrace);
            _hasError = true;
        }

        public void Sample(string chartName, string seriesName, int seriesIndex, SeriesType seriesType, DateTime time, decimal value, string unit = "$")
        {
            _shadow.Sample(chartName, seriesName, seriesIndex, seriesType, time, value, unit);
        }

        public void SampleEquity(DateTime time, decimal value)
        {
            _shadow.SampleEquity(time, value);
        }

        public void SamplePerformance(DateTime time, decimal value)
        {
            _shadow.SamplePerformance(time, value);
        }

        public void SampleBenchmark(DateTime time, decimal value)
        {
            _shadow.SampleBenchmark(time, value);
        }

        public void SampleAssetPrices(Symbol symbol, DateTime time, decimal value)
        {
            _shadow.SampleAssetPrices(symbol, time, value);
        }

        public void SampleRange(List<Chart> samples)
        {
            _shadow.SampleRange(samples);
        }

        public void SetAlgorithm(IAlgorithm algorithm, decimal startingPortfolioValue)
        {
            Algorithm = algorithm;
            _shadow.SetAlgorithm(algorithm, startingPortfolioValue);
        }

        public void StoreResult(Packet packet, bool async = false)
        {
            _shadow.StoreResult(packet, async);
        }

        public void SendStatusUpdate(AlgorithmStatus status, string message = "")
        {
            _shadow.SendStatusUpdate(status, message);
        }

        public void SetChartSubscription(string symbol)
        {
            _shadow.SetChartSubscription(symbol);
        }

        public void RuntimeStatistic(string key, string value)
        {
            _shadow.RuntimeStatistic(key, value);
        }

        public void OrderEvent(OrderEvent newEvent)
        {
            _shadow.OrderEvent(newEvent);
        }

        public void Exit()
        {
            _shadow.Exit();
        }

        public void PurgeQueue()
        {
            _shadow.PurgeQueue();
        }

        public void ProcessSynchronousEvents(bool forceProcess = false)
        {
            _shadow.ProcessSynchronousEvents(forceProcess);
        }
        #endregion
    }
}
