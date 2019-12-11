using System;
using System.Collections.Generic;

namespace Optimization.Base
{
    public static class StatisticsAdapter
    {
        public static readonly Dictionary<string, string> Binding = new Dictionary<string, string>
        {
            {"Average Win","AverageWinRate"},
            {"Average Loss","AverageLossRate"},
            {"Profit-Loss Ratio","ProfitLossRatio"},
            {"Win Rate","WinRate"},
            {"Loss Rate","LossRate"},
            {"Expectancy","Expectancy"},
            {"Compounding Annual Return","CompoundingAnnualReturn"},
            {"Drawdown","Drawdown"},
            {"Net Profit","TotalNetProfit"},
            {"Sharpe Ratio","SharpeRatio"},
            {"Probabilistic Sharpe Ratio", "ProbabilisticSharpeRatio"},
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
        /// Calculates fitness by given metric (fitness score).
        /// </summary>
        /// <remarks></remarks>
        /// <param name="result">Full results dictionary</param>
        /// <param name="fitScore">Existing score of effectivness of an algorithm</param>
        /// <param name="filterEnabled">Indicates whether need to filter the results</param>
        /// <returns></returns>
        public static decimal CalculateFitness(Dictionary<string, decimal> result, FitnessScore fitScore, bool filterEnabled)
        {
            // Calculate fitness using the chosen fitness finction
            decimal fitness;
            switch (fitScore)
            {
                case FitnessScore.SharpeRatio:
                    fitness = result["SharpeRatio"];
                    break;
                case FitnessScore.TotalNetProfit:
                    fitness = result["TotalNetProfit"];
                    break;
                default:
                    throw new ArgumentOutOfRangeException(fitScore.ToString(),"StatisticsAdapter.CalculateFitness() : default");
            }

            // apply filter if enabled
            if (filterEnabled)
            {
                if (!FitnessFilter.IsSuccess(result) || fitness < 0) fitness = FitnessFilter.ErrorValue;
            }

            return fitness;
        }
    }
}
