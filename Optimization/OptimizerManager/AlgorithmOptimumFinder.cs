using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using System;
using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    /// <summary>
    /// The one that manages the whole genetic optimization process. 
    /// </summary>
    public class AlgorithmOptimumFinder : IOptimizerManager
    {
        /// <summary>
        /// Optimization start date
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Fitness Score to sort the parameters obtained by optimization
        /// </summary>
        public FitnessScore? SortCriteria { get; set; }

        /// <summary>
        /// Genetic algorithm itself!
        /// using https://stackoverflow.com/questions/34743533/automated-property-with-getter-only-can-be-set-why/34743568
        /// </summary>
        public GeneticAlgorithmCustom GenAlgorithm { get; }

        /// <summary>
        /// Init class variables. Algorithm start and end dates and sorting method
        /// are to be set explicitely when declaring a class. Default sortCriteria is Sharpe Ratio.
        /// </summary>
        /// <param name="start">Algorithm start date</param>
        /// <param name="end">Algorithm end date</param>
        /// <param name="sortCriteria">Argument of <see cref="FitnessScore"/> type.
        /// Defines a criteria to sort the backtest results and choose best parameters</param>
        public AlgorithmOptimumFinder(DateTime start, DateTime end, FitnessScore sortCriteria)
        {
            // Assign Dates and Criteria to sort the results
            StartDate = start;
            EndDate = end;
            SortCriteria = sortCriteria;

            // Max number of threads
            var maxThreads = Program.Config.MaxThreads > 0 ? Program.Config.MaxThreads : 8;

            // Common properties ->
            var selection = new RouletteWheelSelection();
            var crossover = new TwoPointCrossover();
            var mutation = new UniformMutation(true);

            // Properties specific to optimization modes ->
            IFitness fitness;
            PopulationBase population;
            ITaskExecutor executor;
            ITermination termination;

            switch (Program.Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Linear:
                    executor = new LinearTaskExecutor();
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                case TaskExecutionMode.Parallel:
                    executor = new ParallelTaskExecutor { MaxThreads = maxThreads };
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                case TaskExecutionMode.Azure:
                    executor = new TaskExecutorAzure { MaxThreads = maxThreads };
                    fitness = new AzureFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                default:
                    throw new Exception("Executor initialization failed");
            }

            // Optimization mode
            switch (Program.Config.OptimizationMode)
            {
                case OptimizationMode.BruteForce:
                    {
                        // create cartesian population
                        population = new PopulationCartesian();
                        termination = new GenerationNumberTermination(1);

                        break;
                    }

                case OptimizationMode.Genetic:
                    {
                        // create random population
                        population = new PopulationRandom();
                        termination = new OrTermination(
                            new FitnessStagnationTermination(Program.Config.StagnationGenerations),
                            new GenerationNumberTermination(Program.Config.Generations));

                        break;
                    }

                default:
                    throw new Exception("Optimization mode specific objects were not initialized");
            }

            // Create the GA itself
            // It's important to initialize GA in constructor as we would
            // like to declare event handlers from outside the class before calling Start()
            GenAlgorithm = new GeneticAlgorithmCustom(population, fitness, executor)
            {
                Selection = selection,
                Mutation = mutation,
                Termination = termination,
                MutationProbability = Program.Config.MutationProbability,
                CrossoverProbability = Program.Config.CrossoverProbability
            };
        }

        /// <summary>
        /// Starts an optimization. 
        /// </summary>
        public void Start()
        {

            // Run the GA 
            GenAlgorithm.Start();
        }

    }
}
