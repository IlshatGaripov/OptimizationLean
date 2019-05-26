using GeneticSharp.Domain.Chromosomes;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace Optimization.Tests
{
    [TestFixture()]
    public class OptimizerFitnessTests
    {
        private readonly Wrapper _unit;

        public OptimizerFitnessTests()
        {
            _unit = new Wrapper();
        }

        [TestCase(1, 12, 0.22, 0.5)]
        [TestCase(-1, 12, 0, 0.5)]
        [TestCase(-1, 0, 0, 0.5)]
        [TestCase(1, 12, 0, 1)]
        public void CalculateFitnessTest(decimal car, int trades, double expected, decimal lossRate)
        {
            var actual = _unit.CalculateFitnessWrapper(new Dictionary<string, decimal> {
                { "SharpeRatio", 1 },
                { "CompoundingAnnualReturn", car },
                { "TotalNumberOfTrades", trades },
                { "LossRate", lossRate }
            });
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void EvaluateTest()
        {
            //todo:
            _unit.Evaluate(Mock.Of<IChromosome>());

        }

        private class Wrapper : OptimizerFitness
        {

            public Wrapper() : base()
            {
            }

            public double CalculateFitnessWrapper(Dictionary<string, decimal> result)
            {
                return StatisticsAdapter.CalculateFitness(result, FitnessScore.SharpeRatio);
            }
        }

    }
}