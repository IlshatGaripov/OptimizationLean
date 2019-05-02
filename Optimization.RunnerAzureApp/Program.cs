using System;
using System.Collections.Generic;

namespace Optimization.RunnerAzureApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length % 2 != 0)
            {
                throw new ArgumentException("Must be even number of arguments = key & value");
            }

            // list to store values be passed to the runner
            var inputs = new Dictionary<string, string>();
;
            for (var i = 0; i < args.Length; i+=2)
            {
                inputs.Add(args[i], args[i+1]);
            }

            var result = AzureRunner.Run(inputs);

            // write final stats to an output log
        }
    }
}
