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
            // Load JSON from configuration to class object
            Config = OptimizerInitializer.LoadConfigFromFile("optimization_local.json");

            // TODO: revise 1!
            OptimizerInitializer.Initialize();

            // TODO: revise 2!
            GeneFactory.Initialize(Config.Genes);

            // Configure App Domain settings which will be used if computing performed on the local machine
            OptimizerAppDomainManager.Initialize();    

            try
            {
                // TODO: Should be easier to use Activator.CreateInstance? look into documentation ..
                // create a new instance of a OptimizerFitness object itself or its descendant.
                var fitness = (OptimizerFitness)Assembly.GetExecutingAssembly().CreateInstance(
                    Program.Config.FitnessTypeName, false, BindingFlags.Default, null,
                    new object[] { new FitnessFilter() },
                    null, null);

                // GA manager
                Manager = new GeneManager();
                Manager.Initialize(fitness);
                Manager.Start();

                // In the end
                NLog.LogManager.Shutdown();   // shutdown the logger
                Console.ReadKey(); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }
}