using System;

namespace Optimization
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
        /// Fitness Score to sort the parameters obtained by optimization
        /// </summary>
        FitnessScore SortCriteria { get; set; }

        /// <summary>
        /// Starts the process
        /// </summary>
        void Start();
    }
}