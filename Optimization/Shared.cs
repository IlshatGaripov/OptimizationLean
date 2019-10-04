using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using QuantConnect.Logging;

namespace Optimization
{
    public static class Shared
    {
        /// <summary>
        /// Global Optimization configuration object
        /// </summary>
        public static OptimizerConfiguration Config = LoadConfigFromFile("optimization_local.json");

        /// <summary>
        /// Global (program wise) logger object
        /// </summary>
        public static ILogHandler Logger =
            new CompositeLogHandler(
                new ConsoleLogHandler(),
                new FileLogHandler(filepath: Config.LogFile));

        /// <summary>
        /// Loads values from JSON text file to a special class holding the config values.
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
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
