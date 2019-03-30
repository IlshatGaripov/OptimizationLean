using Moq;
using NUnit.Framework;
using Optimization;

namespace Optimization.Tests
{
    [TestFixture]
    public class DualPeriodSharpeFitnessTests
    {
        private MockRepository mockRepository;
        private Mock<IFitnessFilter> mockFitnessFilter;

        [SetUp]
        public void SetUp()
        {
            mockRepository = new MockRepository(MockBehavior.Strict);
            mockFitnessFilter = this.mockRepository.Create<IFitnessFilter>();
        }

        [TearDown]
        public void TearDown()
        {
            this.mockRepository.VerifyAll();
        }

        [Test]
        public void TestMethod1()
        {
            //todo:
            DualPeriodSharpeFitness unit = this.CreateDualPeriodSharpeFitness();
            
        }

        private DualPeriodSharpeFitness CreateDualPeriodSharpeFitness()
        {
            return new DualPeriodSharpeFitness();
        }


    }
}
