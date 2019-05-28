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
            // Load the optimizer settings from config json
            Config = Exstensions.LoadConfigFromFile("optimization_local.json");

            try
            {
                // GA manager
                var manager = new GeneManager(Config.StartDate, Config.EndDate);
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