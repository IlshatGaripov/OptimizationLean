using System;
using QuantConnect.Statistics;
using System.Collections.Generic;
using System.Linq;

namespace Optimization
{
    public static class StatisticsAdapter
    {
        private static readonly Dictionary<string, string> Binding = new Dictionary<string, string>
        {
            {"Total Trades", "TotalNumberOfTrades"},
            {"Average Win","AverageWinRate"},
            {"Average Loss","AverageLossRate"},
            {"Compounding Annual Return ","CompoundingAnnualReturn"},
            {"Drawdown","Drawdown"},
            {"Expectancy","Expectancy"},
            {"Net Profit","TotalNetProfit"},
            {"Sharpe Ratio","SharpeRatio"},
            {"Loss Rate","LossRate"},
            {"Win Rate","WinRate"},
            {"Profit-Loss Ratio","ProfitLossRatio"},
            {"Alpha","Alpha"},
            {"Beta","Beta"},
            {"Annual Standard Deviation","AnnualStandardDeviation"},
            {"Annual Variance","AnnualVariance"},
            {"Information Ratio","InformationRatio"},
            {"Tracking Error","TrackingError"},
            {"Treynor Ratio","TreynorRatio"},
            {"Total Fees","TotalFees"}
        };

        public static decimal Translate(string key, Dictionary<string, decimal> list)
        {
            if (Binding.ContainsKey(key))
            {
                return list[Binding[key]];
            }

            return list[key];
        }

        /// <summary>
        /// Transforms <see cref="StatisticsResults"/> to a dictionary containing a custom performance summary 
        /// </summary>
        /// <param name="statisticsResults">StatisticsResults object returned by Lean Engine</param>
        /// <returns>Dictionary with informative statistics for the user</returns>
        public static Dictionary<string, decimal> Transform(StatisticsResults statisticsResults)
        {
            var performance = statisticsResults.TotalPerformance;
            var summary = statisticsResults.Summary;

            // Create a dictionary
            var dict = performance.PortfolioStatistics.GetType().GetProperties().ToDictionary(k => k.Name, v => (decimal)v.GetValue(performance.PortfolioStatistics));
            dict.Add("TotalNumberOfTrades", int.Parse(summary["Total Trades"]));
            dict.Add("TotalFees", decimal.Parse(summary["Total Fees"].Substring(1)));

            return dict;
        }

        /// <summary>
        /// Calculates fitness by build-in score key
        /// </summary>
        /// <param name="result">Full results directionary</param>
        /// <param name="scoreKey">Existing score of effectivness of an algorithm</param>
        /// <returns></returns>
        public static double CalculateFitness(Dictionary<string, decimal> result, FitnessScore scoreKey)
        {
            switch (scoreKey)
            {
                case FitnessScore.SharpeRatio:
                    return (double) result["SharpeRatio"];

                case FitnessScore.TotalNetProfit:
                    return (double) result["TotalNetProfit"];

                default:
                    throw new NotImplementedException();
            }
            
        }
    }
}
