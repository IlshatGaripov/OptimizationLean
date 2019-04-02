﻿using Newtonsoft.Json;
using System;

namespace Optimization
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
        /// The decimal precision (rounding) for gene values
        /// </summary>
        public int? Scale { get; set; }

        /// <summary>
        /// Iterative step for brute force mode
        /// </summary>
        public decimal? Step { get; set; }

        /// <summary>
        /// When true, will randomly select a value between 0 to 10946 from the Fibonacci sequence instead of generating random values
        /// </summary>
        /// <remarks></remarks>
        public bool Fibonacci { get; set; }
        
    }
}
