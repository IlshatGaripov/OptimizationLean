using System;
using System.Collections.Generic;
using Optimization.Base;
using Optimization.Genetic;

namespace Optimization.Launcher
{
    /// <summary>
    /// The one that manages the whole genetic optimization process. 
    /// </summary>
    public class AlgorithmOptimumFinder : IOptimizerManager
    {
        /// <summary>
        /// Optimization start date
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Fitness score used to rank the backtest results
        /// </summary>
        public FitnessScore FitnessScore { get; set; }

        /// <summary>
        /// Genetic algorithm itself!
        /// using https://stackoverflow.com/questions/34743533/automated-property-with-getter-only-can-be-set-why/34743568
        /// </summary>
        public GeneticAlgorithm GenAlgorithm { get; }

        /// <summary>
        /// Collection of all chromosomes that appeared in GA search that had positive fitness
        /// </summary>
        public IList<IChromosome> ProfitableChromosomes { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AlgorithmOptimumFinder"/> class
        /// </summary>
        /// <param name="start">Algorithm start date</param>
        /// <param name="end">Algorithm end date</param>
        /// <param name="fitScore">Argument of <see cref="FitnessScore"/> type. Fintess function to rank the backtest results</param>
        /// <param name="filterEnabled">Indicates whether to apply fitness filter to backtest results</param>
        public AlgorithmOptimumFinder(DateTime start, DateTime end, FitnessScore fitScore, bool filterEnabled)
        {
            // Assign Dates and Criteria to sort the results
            StartDate = start;
            EndDate = end;
            FitnessScore = fitScore;

            // Common properties
            var selection = new RouletteWheelSelection();

            // Properties specific to optimization modes
            IFitness fitness;
            PopulationBase population;
            ITaskExecutor executor;
            ITermination termination;

            // Task execution mode
            switch (Shared.Config.TaskExecutionMode)
            {
                // Enable fitness filtering while searching for optimum parameters
                case TaskExecutionMode.Linear:
                    executor = new LinearTaskExecutor();
                    fitness = new OptimizerFitness(StartDate, EndDate, fitScore, filterEnabled);
                    break;

                case TaskExecutionMode.Parallel:
                    executor = new ParallelTaskExecutor();
                    fitness = new OptimizerFitness(StartDate, EndDate, fitScore, filterEnabled);
                    break;

                case TaskExecutionMode.Azure:
                    executor = new ParallelTaskExecutor();
                    fitness = new AzureFitness(StartDate, EndDate, fitScore, filterEnabled);
                    break;

                default:
                    throw new Exception("Executor initialization failed");
            }

            // Optimization mode
            switch (Shared.Config.OptimizationMode)
            {
                case OptimizationMode.BruteForce:
                    {
                        // Create cartesian population
                        population = new PopulationCartesian(Shared.Config.GeneConfigArray);
                        termination = new GenerationNumberTermination(1);

                        break;
                    }

                case OptimizationMode.Genetic:
                    {
                        // Create random population
                        population = new PopulationRandom(Shared.Config.GeneConfigArray, Shared.Config.PopulationInitialSize)
                        {
                            GenerationMaxSize = Shared.Config.GenerationMaxSize
                        };

                        // Logical terminaton
                        var localTerm = new LogicalOrTermination();

                        localTerm.AddTermination(new FruitlessGenerationsTermination(3));

                        if (Shared.Config.Generations.HasValue)
                            localTerm.AddTermination(new GenerationNumberTermination(Shared.Config.Generations.Value));

                        if (Shared.Config.StagnationGenerations.HasValue)
                            localTerm.AddTermination(new FitnessStagnationTermination(Shared.Config.StagnationGenerations.Value));

                        termination = localTerm;
                        break;
                    }

                default:
                    throw new Exception("Optimization mode specific objects were not initialized");
            }

            // Create GA itself
            GenAlgorithm = new GeneticAlgorithm(population, fitness, executor)
            {
                // Reference types
                Selection = selection,
                Termination = termination,

                // Values types
                CrossoverParentsNumber = Shared.Config.CrossoverParentsNumber,
                CrossoverMixProbability = Shared.Config.CrossoverMixProbability,
                MutationProbability = Shared.Config.MutationProbability
            };
        }

        /// <summary>
        /// Runs an optimization
        /// </summary>
        public void Start()
        {
            // -- 1 -- raised when new generation formed 
            GenAlgorithm.GenerationRan += (sender, generation) => 
            {
                Shared.Logger.Trace(generation.Chromosomes.SolutionsToLogOutput("GENERATION RAN"));
            };

            // -- 2 -- raised when optimization completed
            GenAlgorithm.TerminationReached += (sender, population) => 
            {
                // save all good chromosomes and log the output
                ProfitableChromosomes = population.SelectProfitableChromosomes();
                Shared.Logger.Trace(ProfitableChromosomes.SolutionsToLogOutput("TERMINATION REACHED"));
            };

            // launches genetic
            GenAlgorithm.Start();
        }
    }
}
