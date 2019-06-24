using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace Optimization.RunnerAppAzure
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length % 2 != 0)
            {
                throw new ArgumentException("Must be even number of arguments = key & value");
            }

            // list to store values be passed to the runner
            var inputs = new Dictionary<string, string>();

            for (var i = 0; i < args.Length; i+=2)
            {
                inputs.Add(args[i], args[i+1]);
            }

            if (!inputs.ContainsKey("results-output") || !inputs.ContainsKey("log-file"))
            {
                throw new Exception("output or log File Name were not passed as input argument");
            }

            // Run an experiment and obtain the results
            var results = AzureRunner.Run(inputs);

            // write final statisticss to an output log file as json string
            string resultsAsJsonString = JsonConvert.SerializeObject(results);
            File.WriteAllText(inputs["results-output"], resultsAsJsonString);

        }

    }
}
