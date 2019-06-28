using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Optimization.Tests
{
    [TestFixture]
    class ExtensionsTests
    {
        [Test]
        public void SelectDistinctTest()
        {
            // Config
            var config = JsonConvert.DeserializeObject<OptimizerConfiguration>(@"{
            ""genes"": [
            {
                ""key"": ""period"",
                ""min"": 60,
                ""max"": 300
            },
            {
                ""key"": ""mult"",
                ""min"": 1.5,
                ""max"": 2.9
            }
            ],
            ""population-initial-size"": 4}");

            // Population
            var population = new PopulationRandom(config.GeneConfigArray, config.PopulationInitialSize);
            population.CreateInitialGeneration();
            var chromosomes = population.CurrentGeneration.Chromosomes;

            // Assign fitness to all in first collection and assign to merge
            var merged = chromosomes.Select(c => { c.Fitness = 10; return c; }).ToList();

            // Clone first and add to merge
            var chromosomes2 = chromosomes.Select(c => c.CreateNew()).ToList();
            merged.AddRange(chromosomes2);

            // Obtain result
            var result = merged.SelectDistinct();

            // Assert
            Assert.AreEqual(4 ,result.Count);
            foreach (var r in result)
            {
                Assert.AreEqual(r.Fitness,10);
            }
        }
    }
}
