using System;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// An object that orchestrates the process of walk forward optimization
    /// </summary>
    public class WalkForwardOptimizationManager: IOptimizerManager
    {
        /// <summary>
        /// Optimization start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Fitness Score to sort the parameters obtained by optimization
        /// </summary>
        public FitnessScore SortCriteria { get; set; }

        /// <summary>
        /// Walk forward optimization settings object
        /// </summary>
        public WalkForwardConfiguration WalkForwardConfiguration { get; set; }

        /// <summary>
        /// Event fired as one stage of optimization on in-sample and
        /// verification on out-of-sample data is completed
        /// </summary>
        public event EventHandler OneEvaluationStepCompleted;

        /// <summary>
        /// Full results dicrionary for best in sample chromosome backtest result 
        /// </summary>
        public IList<Dictionary<string, decimal>> BestInSampleFullResults = new List<Dictionary<string, decimal>>();

        /// <summary>
        /// Full result dictionary for backtest on out-of-sample data
        /// </summary>
        public IList<Dictionary<string, decimal>> OutOfSampleFullResults = new List<Dictionary<string, decimal>>();

        /// <summary>
        /// Starts the process
        /// </summary>
        public void Start()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// OneEvaluationStepCompleted wrapper
        /// </summary>
        protected virtual void OnOneEvaluationStepCompleted()
        {
            OneEvaluationStepCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
