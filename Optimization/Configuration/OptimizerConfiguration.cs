using GeneticSharp.Domain;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Optimization
{
    /// <summary>
    /// The main program configuration object. Reflecting all those values that optimization.json contains.
    /// </summary>
    [Serializable]
    public class OptimizerConfiguration
    {
        /// <summary>
        /// Optimization mode - genetic/ brute-force
        /// </summary>
        [JsonProperty("optimization-mode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public OptimizationMode OptimizationMode { get; set; }

        /// <summary>
        /// Execution mode - linear/ parallel/ azure
        /// </summary>
        [JsonProperty("execution-mode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ExecutionMode ExecutionMode { get; set; }

        /// <summary>
        /// Number of parallel backtests
        /// </summary>
        [JsonProperty("max-threads")]
        public int MaxThreads { get; set; } = 8;

        /// <summary>
        /// The settings to generate gene values
        /// </summary>
        [JsonProperty("genes")]
        public GeneConfiguration[] Genes { get; set; }

        /// <summary>
        /// The initial size of the population
        /// </summary>
        [JsonProperty("populationSize")]
        public int PopulationSize { get; set; } = 12;

        /// <summary>
        /// The maximum generations
        /// </summary>
        [JsonProperty("generations")]
        public int Generations { get; set; } = 1000;

        /// <summary>
        /// Quit if fitness does not improve for generations
        /// </summary>
        [JsonProperty("stagnationGenerations")]
        public int StagnationGenerations { get; set; } = 10;

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("algorithmTypeName")]
        public string AlgorithmTypeName { get; set; }

        /// <summary>
        /// 1 or 2 point crossover
        /// </summary>
        [JsonProperty("onePointCrossover")]
        public bool OnePointCrossover { get; set; } = false;

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("algorithmLocation")]
        public string AlgorithmLocation { get; set; }

        /// <summary>
        /// By default results with negative Sharpe or CAR are ignored
        /// </summary>
        [JsonProperty("includeNegativeReturn")]
        public bool IncludeNegativeReturn { get; set; }

        /// <summary>
        /// Type name of fitness function. Defaults to fitness based on Sharpe Ratio
        /// </summary>
        [JsonProperty("fitnessTypeName")]
        public string FitnessTypeName { get; set; } = "Optimization.OptimizerFitness";

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("dataFolder")]
        public string DataFolder { get; set; }

        /// <summary>
        /// Settings for use with the ConfiguredFitness
        /// </summary>
        [JsonProperty("fitness")]
        public FitnessConfiguration Fitness { get; set; }

        /// <summary>
        /// Algorithm backtest start date
        /// </summary>
        [JsonProperty("startDate")]
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Algorithm backtest end date
        /// </summary>
        [JsonProperty("endDate")]
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Likeliness of mutation
        /// </summary>
        [JsonProperty("mutationProbability")]
        public float MutationProbability { get; set; } = GeneticAlgorithm.DefaultMutationProbability;

        /// <summary>
        /// Likeliness of crossover
        /// </summary>
        [JsonProperty("crossoverProbability")]
        public float CrossoverProbability { get; set; } = GeneticAlgorithm.DefaultCrossoverProbability;

        /// <summary>
        /// The minimum number of trades to consider the execution a non-failure
        /// </summary>
        [JsonProperty("minimumTrades")]
        public int MinimumTrades { get; set; }

        /// <summary>
        /// Enables the fitness filter that discards probable false positive executions
        /// </summary>
        [JsonProperty("enableFitnessFilter")]
        public bool EnableFitnessFilter { get; set; }

        /// <summary>
        /// File to store transaction log
        /// </summary>
        [JsonProperty("transactionLog")]
        public string TransactionLog { get; set; }

        /// <summary>
        /// Azure Batch acc name.
        /// </summary>
        [JsonProperty("batchAccountName")]
        public string BatchAccountName { get; set; }

        /// <summary>
        /// Azure Batch acc key.
        /// </summary>
        [JsonProperty("batchAccountKey")]
        public string BatchAccountKey { get; set; }

        /// <summary>
        /// Azure Batch acc url.
        /// </summary>
        [JsonProperty("batchAccountUrl")]
        public string BatchAccountUrl { get; set; }

        /// <summary>
        /// Azure Storage acc name.
        /// </summary>
        [JsonProperty("storageAccountName")]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Azure Storage acc key.
        /// </summary>
        [JsonProperty("storageAccountKey")]
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Dedicated compute nodes.
        /// </summary>
        [JsonProperty("dedicatedComputeNodes")]
        public int DedicatedNodeCount { get; set; }

        /// <summary>
        /// Low-priority compute nodes.
        /// </summary>
        [JsonProperty("lowPriorityComputeNodes")]
        public int LowPriorityNodeCount { get; set; }
    }
}
