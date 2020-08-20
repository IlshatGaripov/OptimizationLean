using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect;
using QuantConnect.Lean.Engine.Results;

namespace Optimization.Base
{
    /// <summary>
    /// Optimizer Result Handler derives from <see cref="BacktestingResultHandler"/>
    /// </summary>
    public class OptimizerResultHandler : BacktestingResultHandler
    {
        /// <summary>
        /// Contains final custom statistics results
        /// </summary>
        public Dictionary<string, decimal> FullResults { get; set; }

        /// <summary>
        /// Terminate the result thread and apply any required exit procedures like sending final results.
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            var charts = new Dictionary<string, Chart>(Charts);
            var profitLoss = new SortedDictionary<DateTime, decimal>(Algorithm.Transactions.TransactionRecord);

            // Need to run this one more time
            var statisticsResults = GenerateStatisticsResults(charts, profitLoss);

            var performance = statisticsResults.TotalPerformance;
            var summary = statisticsResults.Summary;

            // Make a dictionary
            var dictionary = performance.PortfolioStatistics.GetType().GetProperties()
                .ToDictionary(k => k.Name, v => (decimal) v.GetValue(performance.PortfolioStatistics));

            // Add additional performance indexes
            dictionary.Add("TotalNumberOfTrades", int.Parse(summary["Total Trades"]));
            dictionary.Add("TotalFees", decimal.Parse(summary["Total Fees"].Substring(1)));

            // Assign
            FullResults = dictionary;
        }

    }
}
