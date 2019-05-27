using System;
using NLog;

namespace Optimization
{
    public static class Program
    {
        // The logger
        public static Logger Logger = LogManager.GetLogger("optimizer");

        // Program wide config file object.
        public static OptimizerConfiguration Config;

        /// <summary>
        /// Main program entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            // Load configuration settings from json to an object
            Config = Exstensions.LoadConfigFromFile("optimization_local.json");

            // TODO: revise 2!
            GeneFactory.Initialize(Config.Genes);

            // Configure App Domain settings that are used if computing is made on the local machine
            OptimizerAppDomainManager.Initialize();    

            try
            {
                // GA manager
                var manager = new GeneManager();
                manager.Start();

                // Shutdown the logger in the end
                LogManager.Shutdown();   

                Console.WriteLine("Press ENTER to exit the program");
                Console.ReadKey(); 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

    }
}