using System;
using System.Collections;
using System.Reflection;
using NLog;

namespace Optimization
{
    public class Program
    {
        // logger
        public static Logger Logger = LogManager.GetLogger("optimizer");

        // optimizer manager
        public static IOptimizerManager Manager;
        
        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            // intit configuration and app domain.
            OptimizerInitializer.Initialize();

            // Init those few variables used when joggling with App Domain features used launching the lean runner.
            OptimizerAppDomainManager.Initialize();

            // local alias for global config 
            var config = OptimizerInitializer.Configuration;


            // TODO: Should be easier to use Activator.CreateInstance? look into documentation ..

            var fitness = (OptimizerFitness)Assembly.GetExecutingAssembly().CreateInstance(config.FitnessTypeName,
                false, BindingFlags.Default, null,
                new object[] { config, new FitnessFilter() }, null, null);

            if (Manager == null)
            {
                if (((IList) new[]
                {
                    typeof(SharpeMaximizer), typeof(NFoldCrossReturnMaximizer), typeof(NestedCrossSharpeMaximizer),
                    typeof(NestedCrossSharpeMaximizer)
                }).Contains(fitness?.GetType()))
                {
                    Manager = new MaximizerManager();
                }
                else
                {
                    // this is default
                    Manager = new GeneManager();
                }
            }

            Manager.Initialize(config, fitness);
            Manager.Start();

            Console.ReadKey();
        }
    }
}