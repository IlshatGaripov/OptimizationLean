using System;
using Newtonsoft.Json;

namespace Optimization
{
    /// <summary>
    /// Contains variables to define algorithm performance filtering
    /// </summary>
    [Serializable]
    public class FitnessFilterConfiguration
    {
        /// <summary>
        /// Minimum number of trades algorithm must 
        /// </summary>
        [JsonProperty("min-trades")]
        public int? MinimumTrades;

        /// <summary>
        /// Maximum allowed Drawdown performance
        /// </summary>
        [JsonProperty("max-drawdown")]
        public decimal? MaxDrawdown;

        /// <summary>
        /// Minimum allowed Sharp Ratio
        /// </summary>
        [JsonProperty("min-sharpe-ratio")]
        public decimal? MinSharpeRatio;
    }
}
