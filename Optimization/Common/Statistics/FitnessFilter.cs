using System.Collections.Generic;

namespace Optimization
{
    public static class FitnessFilter
    {
        public static double ErrorValue = -1;

        /// <summary>
        /// Applies filters to eliminate some false positive optimizer results
        /// </summary>
        /// <param name="result">Results statistic dictionary</param>
        /// <returns>True if results are acceptable, False otherwise</returns>        
        public static bool IsSuccess(Dictionary<string, decimal> result)
        {
            // Define a local variable for syntactic convenience
            var filter = Program.Config.FitnessFilter;

            if (filter == null)
            {
                return true;
            }

            // Filter by min number of trades - null checking and do something with an object
            if (filter.MinimumTrades.HasValue && result["TotalNumberOfTrades"] < filter.MinimumTrades.Value)
                return false;

            // Filter by DrawDown
            if (filter.MaxDrawdown.HasValue && result["Drawdown"] > filter.MaxDrawdown.Value)
                return false;

            // Filter by Sharp Ratio
            if (filter.MinSharpeRatio.HasValue && result["SharpeRatio"] < filter.MinSharpeRatio.Value)
                return false;

            // If all filters passed return true
            return true;
        }

    }

}
