using System;
using GeneticSharp.Domain.Fitnesses;
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

            // Configure App Domain settings that are used if computing is made on the local machine
            OptimizerAppDomainManager.Initialize();    

            try
            {
                var fitnessTypeString = Config.FitnessTypeName;
                var fitness = (IFitness)Activator.CreateInstance(Type.GetType(fitnessTypeString) ?? throw new InvalidOperationException());

                // GA manager
                Manager = new GeneManager();
                Manager.Initialize(fitness);
                Manager.Start();

                // In the end
                NLog.LogManager.Shutdown();   // shutdown the logger

                Console.WriteLine("Press ENTER to exit the program");
                Console.ReadKey(); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
            }
        }

    }
}