using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using Optimization.Base;

namespace Optimization.Genetic
{
    /// <summary>
    /// Class to contain varios static methods or extension methods helpful in various parts of the code.
    /// </summary>
    public static class Extensions
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
                // Take the key from Global config
                var key = ch.GeneConfigurationArray[index].Key;
                // Take value from the Gene
                var value = ch.GetGene(index).Value.ToString();
                // Add these to collection
                resultingDictionary.Add(key, value);
            }

            return resultingDictionary;
        }

        /// <summary>
        /// Returns as a string chromosome's key-value representation from ToDictionary().
        /// </summary>
        /// <param name="ch">The chromosome to represent as a string</param>
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
        /// Creates a special format string for illustrative logging. 
        /// </summary>
        /// <param name="ch">The chromosome to represent as a string</param>
        /// <param name="result">Dictionary with backtest statistics results</param>
        /// <param name="ftSc">Fitness Score enum value</param>
        /// <param name="fitness">Fitness value</param>
        /// <returns> Example:
        /// 2019-12-02T13:13:41.3568607Z TRACE:: [chromosome #id: 771bca555a6c42fca30affe9e77d8018]
        /// PARAMETERS: gold-fast-period 5
        ///             gold-slow-period 70
        ///             gold-drawdown-percent 0.2
        /// RESULTS: SharpeRatio = -0.32 Drawdown = 2.20 TotalNumberOfTrades = 67 AnnualReturn = -1.55
        /// </returns>
        public static string EvaluationToLogOutput(this Chromosome ch, Dictionary<string,decimal> result, FitnessScore ftSc, double fitness)
        {
            // beginning string
            var output = new StringBuilder($"[chromosome #id: {ch.Id}]{Environment.NewLine}PARAMETERS: ");
            // newline and 12 spaces
            string delimiter = Environment.NewLine + new string(' ', 12);
            var pairs = ch.ToDictionary();

            for (int i = 0; i < pairs.Count; i++)
            {
                var item = pairs.ElementAt(i);
                output.Append(item.Key).Append(" ").Append(item.Value);

                // if not the last item in collection append delimiter string
                if (i != (pairs.Count - 1))
                {
                    output.Append(delimiter);
                }
            }
            // finally
            output.Append(Environment.NewLine);
            output.Append($"RESULTS: {ftSc} = {fitness:f2} ");
            output.Append($"Drawdown = {result["Drawdown"] * 100:f2} ");
            output.Append($"TotalNumberOfTrades = {result["TotalNumberOfTrades"]} ");
            output.Append($"AnnualReturn = {result["CompoundingAnnualReturn"] * 100:f2}");
            return output.ToString();
        }

        /// <summary>
        /// Creates a formatted string for illustrative logging the collection of chromosomes
        /// </summary>
        /// <param name="chromosomes">Collection of chromosomes to display</param>
        /// <param name="headerMsg">The header message</param>
        /// <returns> Example:
        /// [GENERATION RAN] - profitable solutions : 3
        ///     	1.76 # (6, 40, 0.3)
        ///         0.90 # (8, 50, 0.3)
        ///         0.54 # (8, 40, 0.3)
        /// </returns>
        public static string SolutionsToLogOutput(this IList<IChromosome> chromosomes, string headerMsg)
        {
            // begins with new line
            var output = new StringBuilder(Environment.NewLine);
            output.Append($"[{headerMsg}] - profitable solutions : {chromosomes.Count}");
            output.Append(Environment.NewLine);
            foreach (var c in chromosomes)
            {
                var cBase = (Chromosome)c;
                output.Append($"\t{cBase.Fitness:f2} # ({string.Join(", ", cBase.ToDictionary().Values).TrimEnd(' ',',')})");
                output.Append(Environment.NewLine);
            }

            return output.ToString();
        }

        /// <summary>
        /// Selects all chromosomes with positive fitness from given population
        /// </summary>
        /// <param name="population">Population</param>
        /// <returns></returns>
        public static IList<IChromosome> SelectProfitableChromosomes(this PopulationBase population)
        {
            var completeList = new List<IChromosome>();
            foreach (var g in population.Generations)
            {
                completeList.AddRange(g.Chromosomes);
            }

            return completeList.SelectDistinct()
                .Where(c => c.Fitness != null && c.Fitness.Value > 0)
                .OrderByDescending(c => c.Fitness.Value)
                .ToList();
        }

        /// <summary>
        /// Makes sure that given chromosomes contain only distinct gene value sequences.
        /// When selecting between two or more equal value sequence chromosomes
        /// priority will be given to chromosomes that have evaluated fitness value.
        /// </summary>
        /// <param name="chromosomes">The list that may have chromosomes with repeating sequence of genes</param>
        /// <returns></returns>
        public static IList<IChromosome> SelectDistinct(this IList<IChromosome> chromosomes)
        {
            // Will first sort depending on whether or not gene has value
            // then group to objects depending on the uniqueness of gene values sequences
            // and will select the first item of a group. If there are two values that have
            // same gene values sequance but one has fitness values and second has not the
            // first chromosome will be chosen ->
            return chromosomes.OrderBy(c => c.Fitness.HasValue
                    ? 0
                    : 1)
                .GroupBy(c => c.GetGenes(),
                    new GeneArrayComparer())
                .Select(g => g.First())
                .ToList();
        }

        /// <summary>
        /// Removes from list the chromosomes that have gene sequence encountered in past generations 
        /// </summary>
        /// <param name="chromosomes">Chromosomes to check for duplicates</param>
        /// <param name="generations">Past generations</param>
        public static void RemoveEvaluatedPreviously(this IList<IChromosome> chromosomes, IList<Generation> generations)
        {
            if(generations == null || generations.Count == 0)
                throw new ArgumentException("Please make sure generations has value");

            var comparer = new GeneArrayComparer();
            var pastGenChromosomes = new List<IChromosome>();

            foreach (var g in generations)
            {
                pastGenChromosomes.AddRange(g.Chromosomes);
            }

            // Delete from list if gene sequence has been encountered before ->
            for (int i = chromosomes.Count - 1; i >= 0; i--)
            {
                if(chromosomes[i].Fitness.HasValue)
                    continue;   // if has value then it came from previos generation

                foreach (var p in pastGenChromosomes)
                {
                    if (comparer.Equals(chromosomes[i].GetGenes(), p.GetGenes()))
                    {
                        chromosomes.RemoveAt(i);
                    }
                }
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
