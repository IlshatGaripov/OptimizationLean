using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.Logging;

namespace Optimization.Base
{
    /// <summary>
    /// Contains static program wide ojects that are commonly used in various parts
    /// </summary>
    public static class Shared
    {
        // Location of the configuration file.
        private const string ConfigurationFilePath = 
            "C:\\Users\\ilshat\\source\\repos\\OptimizationLean\\Optimization.Launcher\\optimization_local.json";

        /// <summary>
        /// Optimization configuration object
        /// </summary>
        public static OptimizerConfiguration Config = LoadConfigFromFile(ConfigurationFilePath);

        /// <summary>
        /// Global log handler
        /// </summary>
        public static ILogHandler Logger =
            new CompositeLogHandler(new ConsoleLogHandler(), new FileLogHandler(filepath: Config.LogFile));

        /// <summary>
        /// Loads values from JSON text file and converts to an object
        /// </summary>
        public static OptimizerConfiguration LoadConfigFromFile(string path)
        {
            try
            {
                // fulfill an object with values from configuration file 
                var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };
                var configuration = JsonConvert.DeserializeObject<OptimizerConfiguration>(File.ReadAllText(path), dateTimeConverter);

                // Make sure all the variables have been provided by the config
                if (configuration.StartDate == DateTime.MinValue ||
                    configuration.EndDate == DateTime.MinValue ||
                    configuration.FitnessScore == 0 ||
                    configuration.WalkingForward == null ||
                    configuration.FitnessFilter == null)
                {
                    throw new ArgumentException("Configuration file is not fully completed. Check the args!");
                }
                
                // if all the checks have been successfully passed return an object
                return configuration;
            }
            catch (Exception e)
            {
                // using console as Logger is yet not initialized here
                Console.WriteLine(e.Message);
                throw;
            }
        }
    }
}
