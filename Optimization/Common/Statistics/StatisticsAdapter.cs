using System;
using System.Collections.Generic;

namespace Optimization
{
    public static class StatisticsAdapter
    {
        private static readonly Dictionary<string, string> Binding = new Dictionary<string, string>
        {
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
            {"Total Trades", "TotalNumberOfTrades"},
            {"Total Fees","TotalFees"}
        };

        /// <summary>
        /// Calculates fitness by build-in score key
        /// </summary>
        /// <param name="result">Full results directionary</param>
        /// <param name="scoreKey">Existing score of effectivness of an algorithm</param>
        /// <returns></returns>
        public static double CalculateFitness(Dictionary<string, decimal> result, FitnessScore scoreKey)
        {
            // Apply a Fitness Filter to result
            if (Program.Config.FitnessFilter != null && !FitnessFilter.IsSuccess(result))
            {
                return FitnessFilter.ErrorValue;
            }
            
            // If filter successfully passed calculate the fitness using metric specified in config
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
