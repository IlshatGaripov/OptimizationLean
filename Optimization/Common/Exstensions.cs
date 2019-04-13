using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Optimization
{
    /// <summary>
    /// Class to contain varios static methods or extension methods helpful in various parts of the code.
    /// </summary>
    public static class Exstensions
    {
        /// <summary>
        /// Can be used to make a copy of an Configuration object.
        /// </summary>
        public static T Clone<T>(T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }

        /// <summary>
        /// Converts a collection of chromosome genes into string/object dictionary.
        /// </summary>
        public static Dictionary<string, object> ToDictionary(this Chromosome ch)
        {
            var resultingDictionary = new Dictionary<string, object>();

            for (var index = 0; index < ch.Length; index++)
            {
                // take the key from global config file
                resultingDictionary.Add(Program.Config.Genes[index].Key, ch.GetGene(index).Value);
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
                output.Append(item.Key).Append(": ").Append(item.Value.ToString()).Append(", ");
            }

            return output.ToString().TrimEnd(',', ' ');
        }
    }
}
