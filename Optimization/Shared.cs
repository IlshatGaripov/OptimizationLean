using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.Logging;

namespace Optimization
{
    /// <summary>
    /// Contains static program wide ojects that are commonly used in various parts
    /// </summary>
    public static class Shared
    {
        /// <summary>
        /// Global optimization configuration object
        /// </summary>
        public static OptimizerConfiguration Config = LoadConfigFromFile("optimization_local.json");

        /// <summary>
        /// Global logger object
        /// </summary>
        public static ILogHandler Logger =
            new CompositeLogHandler(
                new ConsoleLogHandler(),
                new FileLogHandler(filepath: Config.LogFile));

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
                return JsonConvert.DeserializeObject<OptimizerConfiguration>(File.ReadAllText(path), dateTimeConverter);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }
    }
}
