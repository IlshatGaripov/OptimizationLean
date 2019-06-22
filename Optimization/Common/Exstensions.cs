using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using GeneticSharp.Domain.Chromosomes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Optimization
{
    /// <summary>
    /// Class to contain varios static methods or extension methods helpful in various parts of the code.
    /// </summary>
    public static class Exstensions
    {
        /// <summary>
        /// Can be used to make a deep copy of Configuration object.
        /// </summary>
        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        /// Converts a collection of chromosome genes into string/object dictionary.
        /// </summary>
        public static Dictionary<string, string> ToDictionary(this Chromosome ch)
        {
            var resultingDictionary = new Dictionary<string, string>();

            for (var index = 0; index < ch.Length; index++)
            {
                // Take the key from Global config ->
                var key = ch.GeneConfigurationArray[index].Key;

                // Take value from the Gene ->
                var value = ch.GetGene(index).Value.ToString();

                // Add these to collection ->
                resultingDictionary.Add(key, value);
            }

            return resultingDictionary;
        }

        /// <summary>
        /// Returns as a string chromosome's key-value representation from ToDictionary().
        /// </summary>
        public static string ToKeyValueString(this Chromosome ch)
        {
            var output = new StringBuilder();
            foreach (var item in ch.ToDictionary())
            {
                output.Append(item.Key).Append(" ").Append(item.Value).Append(" ");
            }

            return output.ToString().TrimEnd(',', ' ');
        }

        /// <summary>
        /// Makes sure that given chromosomes contain only distinct gene value sequences.
        /// When selecting by two or more equal sequence priority is given to chromosomes with evaluated fitness.
        /// </summary>
        /// <param name="chromosomes">The list that may have chromosomes with repeating sequence of genes</param>
        /// <returns></returns>
        public static IList<IChromosome> SelectDistinct(this IList<IChromosome> chromosomes)
        {
            return chromosomes.OrderBy(c => c.Fitness.HasValue
                    ? 0
                    : 1)
                .GroupBy(c => c.GetGenes(),
                    new GeneArrayComparer())
                .Select(g => g.First())
                .ToList();
        }

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

        /// <summary>
        /// Serialize object to memory stream
        /// https://stackoverflow.com/questions/10390356/serializing-deserializing-with-memory-stream
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>MemoryStream</returns>
        public static MemoryStream SerializeToStream(object obj)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            return stream;
        }

        /// <summary>
        /// Deserialize object from memory stream
        /// </summary>
        /// <param name="stream">Memory stream</param>
        /// <returns>Resulting object</returns>
        public static object DeserializeFromStream(MemoryStream stream)
        {
            IFormatter formatter = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            object obj = formatter.Deserialize(stream);
            return obj;
        }
    }

}
