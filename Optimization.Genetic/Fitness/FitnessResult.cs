using System;
using System.Collections.Generic;

namespace Optimization.Genetic
{
    /// <summary>
    /// Full backtest result are defined in simpliest by full statistics and start and end dates
    /// </summary>
    [Serializable]
    public class FitnessResult
    {
        /// <summary>
        /// Chromosome this fitness result belongs to
        /// </summary>
        public Chromosome Chromosome { get; set; }

        /// <summary>
        /// Experiment Start Date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Experiment End Date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Full backtest results dictionary returned by OptimizerResultHandler
        /// </summary>
        public Dictionary<string, decimal> FullResults { get; set; }
    }
}
