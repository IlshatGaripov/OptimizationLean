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
            if (Program.Config.FitnessFilter != null)
            {
                return true;
            }

            /*
            //must meet minimum trading activity if configured
            if (Program.Config.MinimumTrades > 0 && result["TotalNumberOfTrades"] < Program.Config.MinimumTrades)
            {
                return false;
            }
            */

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
