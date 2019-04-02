using System;
using System.Reflection;
using NLog;

namespace Optimization
{
    public static class Program
    {
        // logger
        public static Logger Logger = LogManager.GetLogger("optimizer");

        // optimizer manager
        public static IOptimizerManager Manager;

        // program wide config file.
        public static OptimizerConfiguration Config;

        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            // init global and gene config files.
            Config = OptimizerInitializer.LoadConfigFromFile();

            // debug
            Console.WriteLine(Config.Mode);

            // TODO : this init is to be revised. 
            GeneFactory.Initialize(Config.Genes);

            // App Domain features used launching the lean runner.
            OptimizerAppDomainManager.Initialize();
            
            
            // TODO: Should be easier to use Activator.CreateInstance? look into documentation ..
            // create a new instance of a OptimizerFitness object itself or its descendant.
            var fitness = (OptimizerFitness)Assembly.GetExecutingAssembly().CreateInstance(
                Program.Config.FitnessTypeName,false, BindingFlags.Default, null,
                new object[] { new FitnessFilter() }, 
                null, null);
            
            // GA manager
            
            /*
            Manager = new GeneManager();
            Manager.Initialize(fitness);
            Manager.Start();
            */

            Console.ReadKey();


            /*
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
            */
        }

    }
}