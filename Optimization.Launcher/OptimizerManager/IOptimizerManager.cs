using System;
using Optimization.Base;

namespace Optimization.Launcher
{
    /// <summary>
    /// Interface to implement by the object to orchestrate the optimization process
    /// </summary>
    public interface IOptimizerManager
    {
        /// <summary>
        /// Optimization start date
        /// </summary>
        DateTime StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        DateTime EndDate { get; set; }

        /// <summary>
        /// Fitness score used to rank the backtest results
        /// </summary>
        FitnessScore FitnessScore { get; set; }

        /// <summary>
        /// Starts the process
        /// </summary>
        void Start();
    }
}