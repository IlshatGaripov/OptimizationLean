using System;
using System.Collections.Generic;

namespace Optimization
{
    public static class StatisticsAdapter
    {
        public static readonly Dictionary<string, string> Binding = new Dictionary<string, string>
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
        /// Calculates fitness by given metric (fitness score).
        /// </summary>
        /// <remarks></remarks>
        /// <param name="result">Full results dictionary</param>
        /// <param name="scoreKey">Existing score of effectivness of an algorithm</param>
        /// <param name="filterEnabled">Indicates whether need to filter the results</param>
        /// <returns></returns>
        public static double CalculateFitness(Dictionary<string, decimal> result, FitnessScore scoreKey, bool filterEnabled)
        {
            // Calculate fitness using the metric specified
            double fitness;
            switch (scoreKey)
            {
                case FitnessScore.SharpeRatio:
                    fitness = (double) result["SharpeRatio"];
                    break;

                case FitnessScore.TotalNetProfit:
                    fitness = (double) result["TotalNetProfit"];
                    break;

                default:
                    throw new NotImplementedException("StatisticsAdapter.CalculateFitness() : default");
            }

            // Filter positive results ->
            if (fitness > 0 && filterEnabled)
            {
                // If filter is not passed -> 
                if ( !FitnessFilter.IsSuccess(result))
                {
                    fitness = FitnessFilter.ErrorValue;
                }
            }

            return fitness;
        }
    }
}
