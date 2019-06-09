﻿using GeneticSharp.Domain;
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
        /// Task execution mode:
        /// linear or parallel using local computing powers or compute in parallel in azure cloud
        /// </summary>
        [JsonProperty("execution-mode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TaskExecutionMode TaskExecutionMode { get; set; }

        /// <summary>
        /// Number of parallel backtests
        /// </summary>
        [JsonProperty("max-threads")]
        public int MaxThreads { get; set; } = 8;

        /// <summary>
        /// Metric to evaluate the algorithm performance
        /// </summary>
        [JsonProperty("fitness-score")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FitnessScore? FitnessScore { get; set; }

        /// <summary>
        /// Object contains configuration to sort out algorithms with good performance
        /// </summary>
        [JsonProperty("fitness-filter")]
        public FitnessFilterConfiguration FitnessFilter { get; set; }

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
        /// Walk-forward optimizaion mode config object
        /// </summary>
        [JsonProperty("walk-forward")]
        public WalkForwardConfiguration WalkForwardConfiguration { get; set; }

        /// <summary>
        /// The settings to generate gene values
        /// </summary>
        [JsonProperty("genes")]
        public GeneConfiguration[] GeneConfigArray { get; set; }

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("algorithm-type-name")]
        public string AlgorithmTypeName { get; set; }

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("algorithm-location")]
        public string AlgorithmLocation { get; set; }

        /// <summary>
        /// Override config.json setting
        /// </summary>
        [JsonProperty("data-folder")]
        public string DataFolder { get; set; }

        /// <summary>
        /// Azure Batch acc name.
        /// </summary>
        [JsonProperty("batch-account-name")]
        public string BatchAccountName { get; set; }

        /// <summary>
        /// Azure Batch acc key.
        /// </summary>
        [JsonProperty("batch-account-key")]
        public string BatchAccountKey { get; set; }

        /// <summary>
        /// Azure Batch acc url.
        /// </summary>
        [JsonProperty("batch-account-url")]
        public string BatchAccountUrl { get; set; }

        /// <summary>
        /// Azure Storage acc name.
        /// </summary>
        [JsonProperty("storage-account-name")]
        public string StorageAccountName { get; set; }

        /// <summary>
        /// Azure Storage acc key.
        /// </summary>
        [JsonProperty("storage-account-key")]
        public string StorageAccountKey { get; set; }

        /// <summary>
        /// Dedicated compute nodes.
        /// </summary>
        [JsonProperty("dedicated-nodes")]
        public int DedicatedNodeCount { get; set; }

        /// <summary>
        /// Low-priority compute nodes.
        /// </summary>
        [JsonProperty("low-priority-nodes")]
        public int LowPriorityNodeCount { get; set; }

        /// <summary>
        /// The initial size of the population
        /// </summary>
        [JsonProperty("population-size")]
        public int PopulationSize { get; set; } = 12;

        /// <summary>
        /// The maximum generations
        /// </summary>
        [JsonProperty("generations")]
        public int Generations { get; set; } = 1000;

        /// <summary>
        /// Quit if fitness does not improve for generations
        /// </summary>
        [JsonProperty("stagnation-generations")]
        public int StagnationGenerations { get; set; } = 10;

        /// <summary>
        /// 1 or 2 point crossover
        /// </summary>
        [JsonProperty("one-point-crossover")]
        public bool OnePointCrossover { get; set; } = false;

        /// <summary>
        /// Likeliness of mutation
        /// </summary>
        [JsonProperty("mutation-probability")]
        public float MutationProbability { get; set; } = GeneticAlgorithm.DefaultMutationProbability;

        /// <summary>
        /// Likeliness of crossover
        /// </summary>
        [JsonProperty("crossover-probability")]
        public float CrossoverProbability { get; set; } = GeneticAlgorithm.DefaultCrossoverProbability;
    }
}
