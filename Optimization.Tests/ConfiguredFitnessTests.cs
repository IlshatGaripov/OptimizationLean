using Moq;
using NUnit.Framework;
using Optimization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Optimization.Tests
{
    [TestFixture()]
    public class ConfiguredFitnessTests
    {
        private Wrapper _unit;

        [TestCase("TotalNumberOfTrades")]
        [TestCase("Total Trades")]
        public void CalculateFitnessTest(string key)
        {
            _unit = new Wrapper(new FitnessConfiguration
                {
                    Scale = 0.1,
                    Modifier = -1,
                    Name = "TestName",
                    ResultKey = key
                });

            var actual = _unit.CalculateFitnessWrapper(new Dictionary<string, decimal> { { "TotalNumberOfTrades", 10 } });

            Assert.AreEqual(-1d, actual.Item2);
        }

        [Test()]
        public void GetValueFromFitnessTest()
        {
            _unit = new Wrapper(new FitnessConfiguration
            {
                Scale = 0.1,
                Modifier = -1,
                Name = "TestName",
                ResultKey = "TotalTrades"
            });

            var actual = _unit.GetAdjustedFitness(-1d);
            Assert.AreEqual(10, actual);
        }

        private class Wrapper : ConfiguredFitness
        {
            public Wrapper(FitnessConfiguration fitnessConfiguration) : base(fitnessConfiguration)
            {
            }

            public Tuple<decimal, double> CalculateFitnessWrapper(Dictionary<string, decimal> result)
            {
                var fitness = base.CalculateFitness(result);
                return new Tuple<decimal, double>(fitness.Value, fitness.Fitness);
            }
        }

    }
}