using System.Collections.Generic;

namespace Optimization.Base
{
    public static class FitnessFilter
    {
        /// <summary>
        /// Error value to be assigned to positive results that haven't passed filter.
        /// </summary>
        public static decimal ErrorValue = -10;

        /// <summary>
        /// Applies filters to eliminate some false positive optimizer results
        /// </summary>
        /// <param name="result">Results statistic dictionary</param>
        /// <returns>True if results are acceptable, False otherwise</returns>        
        public static bool IsSuccess(Dictionary<string, decimal> result)
        {
            // Define a local variable for syntactic convenience
            var filter = Shared.Config.FitnessFilter;

            if (filter.MinimumTrades.HasValue && result["TotalNumberOfTrades"] < filter.MinimumTrades.Value)
                return false;

            if (filter.MaxDrawdown.HasValue && result["Drawdown"] > filter.MaxDrawdown.Value)
                return false;

            if (filter.MinSharpeRatio.HasValue && result["SharpeRatio"] < filter.MinSharpeRatio.Value)
                return false;

            return true;
        }
    }
}
