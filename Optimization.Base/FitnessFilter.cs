using System;
using System.Collections.Generic;

namespace Optimization.Base
{
    public static class FitnessFilter
    {
        /// <summary>
        /// Error value to be assigned to positive results that haven't passed filter.
        /// </summary>
        public static double ErrorValue = -0.001;

        /// <summary>
        /// Static ctor
        /// </summary>
        static FitnessFilter()
        {
            // Throw an exception if no fitness filter configuration has been specified
            if (Shared.Config.FitnessFilter == null)
            {
                throw new ArgumentException("FitnessFilter static ctor(): FitnessFilter is null");
            }
        }

        /// <summary>
        /// Applies filters to eliminate some false positive optimizer results
        /// </summary>
        /// <param name="result">Results statistic dictionary</param>
        /// <returns>True if results are acceptable, False otherwise</returns>        
        public static bool IsSuccess(Dictionary<string, decimal> result)
        {
            // Define a local variable for syntactic convenience
            var filter = Shared.Config.FitnessFilter;

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
