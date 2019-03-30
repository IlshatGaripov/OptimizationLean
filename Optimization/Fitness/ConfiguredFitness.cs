using System;
using System.Collections.Generic;

namespace Optimization
{
    public class ConfiguredFitness : OptimizerFitness
    {
        private readonly FitnessConfiguration _fitnessConfig;

        public ConfiguredFitness(FitnessConfiguration fitnessConfiguration)
        {
            _fitnessConfig = fitnessConfiguration;

            if (!_fitnessConfig.Scale.HasValue)
            {
                _fitnessConfig.Scale = 1;
            }
            if (!_fitnessConfig.Modifier.HasValue)
            {
                _fitnessConfig.Modifier = 1;
            }

            Name = _fitnessConfig.Name;
        }

        //Fitness based on config settings
        protected override FitnessResult CalculateFitness(Dictionary<string, decimal> result)
        {
            var fitness = new FitnessResult();

            var raw = StatisticsAdapter.Translate(Program.Config.Fitness.ResultKey, result);

            fitness.Value = raw;

            fitness.Fitness = (double)raw * _fitnessConfig.Scale.Value * _fitnessConfig.Modifier.Value;

            return fitness;
        }

        public override double GetAdjustedFitness(double? fitness)
        {
            return (fitness.Value / _fitnessConfig.Scale.Value) / _fitnessConfig.Modifier.Value;
        }
    }
}
