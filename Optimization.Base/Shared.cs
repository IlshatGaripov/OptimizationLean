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
        private static readonly string ConfigurationFilePath =
            "C:\\Users\\sterling\\source\\repos\\OptimizationLean\\Optimization.Launcher\\optimization_local.json";
        
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
            // DateTimeFormat for proper deserialize of start and end date string to DateTime
            var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };

            try
            {
                // Transform the text from json to an object that holds the values from a file
                var configuration = JsonConvert.DeserializeObject<OptimizerConfiguration>(File.ReadAllText(path), dateTimeConverter);

                // Make sure that start and end dates, fitness score
                // and walking forward and fitness filter configuration blocks are present
                if (configuration.StartDate == DateTime.MinValue ||
                    configuration.EndDate == DateTime.MinValue ||
                    configuration.FitnessScore == 0 ||
                    configuration.WalkingForward == null ||
                    configuration.FitnessFilter == null)
                {
                    throw new ArgumentException("Shared.LoadConfigFromFile(): Not enough varialbes in 'optimization.json'");
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
