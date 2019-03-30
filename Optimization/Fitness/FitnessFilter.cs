using System.Collections.Generic;

namespace Optimization
{
    public class FitnessFilter : IFitnessFilter
    {
        /// <summary>
        /// Applies standard filters to eliminate some false positive optimizer results
        /// </summary>
        /// <param name="result">The statistic results</param>
        /// <param name="fitness">The calling fitness</param>
        /// <returns></returns>        
        public bool IsSuccess(Dictionary<string, decimal> result, OptimizerFitness fitness)
        {
            if (!Program.Config.EnableFitnessFilter)
            {
                return true;
            }

            /*
            //using config ignore a result with negative return or disable this single filter and still apply others
            if (fitness.GetType() != typeof(CompoundingAnnualReturnFitness) && !Program.Config.IncludeNegativeReturn && result["CompoundingAnnualReturn"] < 0)
            {
                return false;
            }
            */

            if (!Program.Config.IncludeNegativeReturn && result["CompoundingAnnualReturn"] < 0)
            {
                return false;
            }

            //must meet minimum trading activity if configured
            if (Program.Config.MinimumTrades > 0 && result["TotalNumberOfTrades"] < Program.Config.MinimumTrades)
            {
                return false;
            }

            //Consider not trading a failure
            if (result["TotalNumberOfTrades"] == 0)
            {
                return false;
            }

            //Consider 100% loss rate a failure
            return result["LossRate"] != 1;
        }

    }

}
