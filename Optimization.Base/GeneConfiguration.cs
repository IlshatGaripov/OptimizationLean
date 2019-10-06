using System;
using Newtonsoft.Json;

namespace Optimization.Base
{
    /// <summary>
    /// Class determines the values helpful to identify genes.
    /// </summary>
    [JsonConverter(typeof(GeneConverter))]
    [Serializable]
    public class GeneConfiguration
    {
        /// <summary>
        /// The unique key of the gene
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// The minimum value for an int value
        /// </summary>
        public int? MinInt { get; set; }

        /// <summary>
        /// The maximum value for an int value
        /// </summary>
        public int? MaxInt { get; set; }

        /// <summary>
        /// The minimum value for a decimal value
        /// </summary>
        public decimal? MinDecimal { get; set; }

        /// <summary>
        /// The maximum value for a decimal value
        /// </summary>
        public decimal? MaxDecimal { get; set; }

        /// <summary>
        /// Iterative step for brute force mode
        /// </summary>
        public decimal? Step { get; set; }
    }
}
