using System.Collections.Generic;

namespace Optimization
{
    /*
    public class CompoundingAnnualReturnFitness : OptimizerFitness
    {
        public override string Name { get; set; } = "Return";
        protected override double Scale { get; set; } = 0.01;

        public CompoundingAnnualReturnFitness(IOptimizerConfiguration config, IFitnessFilter filter) : base(config, filter)
        {
        }
        
        //Fitness based on Compounding Annual Return
        protected override FitnessResult CalculateFitness(Dictionary<string, decimal> result)
        {
            var fitness = new FitnessResult();

            var car = result["CompoundingAnnualReturn"];

            if (!Filter.IsSuccess(result, this))
            {
                car = -100m;
            }

            fitness.Value = car;

            fitness.Fitness = (double)car * Scale;

            return fitness;
        }

        public override double GetAdjustedFitness(double? fitness)
        {
            return fitness.Value / Scale;
        }
    }
    */
}
