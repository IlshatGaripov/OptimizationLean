using System;
using System.Collections.Generic;
using System.Linq;
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
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Optimization end date
        /// </summary>
        public DateTime? EndDate { get; set; }

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
        public IList<Chromosome> ProfitableChromosomes { get; set; }

        /// <summary>
        /// Init class variables. Algorithm start and end dates and sorting method
        /// are to be set explicitely when declaring a class. Default sortCriteria is Sharpe Ratio.
        /// </summary>
        /// <param name="start">Algorithm start date</param>
        /// <param name="end">Algorithm end date</param>
        /// <param name="sortCriteria">Argument of <see cref="FitnessScore"/> type. Fintess function to rank the backtest results</param>
        /// <param name="filterEnabled">Indicates whether to apply fitness filter to backtest results</param>
        public AlgorithmOptimumFinder(DateTime start, DateTime end, FitnessScore sortCriteria, bool filterEnabled)
        {
            // Assign Dates and Criteria to sort the results ->
            StartDate = start;
            EndDate = end;
            FitnessScore = sortCriteria;

            // Common properties ->
            var selection = new RouletteWheelSelection();

            // Properties specific to optimization modes ->
            IFitness fitness;
            PopulationBase population;
            ITaskExecutor executor;
            ITermination termination;

            // Task execution mode ->
            switch (Shared.Config.TaskExecutionMode)
            {
                // Enable fitness filtering while searching for optimum parameters ->
                case TaskExecutionMode.Linear:
                    executor = new LinearTaskExecutor();
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria, filterEnabled);
                    break;

                case TaskExecutionMode.Parallel:
                    executor = new ParallelTaskExecutor();
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria, filterEnabled);
                    break;

                case TaskExecutionMode.Azure:
                    executor = new TaskExecutorAzure();
                    fitness = new AzureFitness(StartDate.Value, EndDate.Value, sortCriteria, filterEnabled);
                    break;

                default:
                    throw new Exception("Executor initialization failed");
            }

            // Optimization mode ->
            switch (Shared.Config.OptimizationMode)
            {
                case OptimizationMode.BruteForce:
                    {
                        // Create cartesian population ->
                        population = new PopulationCartesian(Shared.Config.GeneConfigArray);
                        termination = new GenerationNumberTermination(1);

                        break;
                    }

                case OptimizationMode.Genetic:
                    {
                        // Create random population ->
                        population = new PopulationRandom(Shared.Config.GeneConfigArray, Shared.Config.PopulationInitialSize)
                        {
                            GenerationMaxSize = Shared.Config.GenerationMaxSize
                        };

                        // Logical terminaton ->
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

            // Create the GA itself
            // It's important to initialize GA in a constructor as we would
            // like to declare event handlers from outside the class before calling Start()
            GenAlgorithm = new GeneticAlgorithm(population, fitness, executor)
            {
                // Reference type ->
                Selection = selection,
                Termination = termination,

                // Numeric values ->
                CrossoverParentsNumber = Shared.Config.CrossoverParentsNumber,
                CrossoverMixProbability = Shared.Config.CrossoverMixProbability,
                MutationProbability = Shared.Config.MutationProbability
            };
        }

        /// <summary>
        /// Starts an optimization. 
        /// </summary>
        public void Start()
        {
            // register event handlers and run
            GenAlgorithm.GenerationRan += GenerationRan;
            GenAlgorithm.TerminationReached += TerminationReached;

            GenAlgorithm.Start();
        }

        /// <summary>
        /// Handler called at the end of work of genetic algorithm
        /// </summary>
        private void TerminationReached(object sender, TerminationReachedEventArgs e)
        {
            // Choose all good chromosomes ->
            ProfitableChromosomes = ChooseProfitableChromosomes(e.Pupulation);

            Shared.Logger.Trace("Termination reached");
            Shared.Logger.Trace($"Good chromosomes - Count {ProfitableChromosomes.Count} - printing :");

            foreach (var c in ProfitableChromosomes)
            {
                Shared.Logger.Trace($"{c.Fitness} ## {c.ToKeyValueString()}");
            }
            Shared.Logger.Trace(" <->");
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        private void GenerationRan(object sender, GenerationRanEventArgs e)
        {
            Shared.Logger.Trace(" <->");
            Shared.Logger.Trace($"Generation formed - profitable solutions count - {e.Generation.Chromosomes.Count} :");
            foreach (var c in e.Generation.Chromosomes)
            {
                var chromBase = (Chromosome) c;
                Shared.Logger.Trace($"{chromBase.Fitness} ## {chromBase.ToKeyValueString()}");
            }

            if (e.Generation.IsFruitless)
            {
                Shared.Logger.Error("WARNING: Generation is fruitless, i.e has zero or very few acceptable solutions");
            }
            Shared.Logger.Trace(" <->");
        }

        /// <summary>
        /// Selects all chromosomes with positive fitness after GA completes its work and ranges them by fitness
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        private static IList<Chromosome> ChooseProfitableChromosomes(PopulationBase population)
        {
            var completeList = new List<IChromosome>();
            foreach (var g in population.Generations)
            {
                completeList.AddRange(g.Chromosomes);
            }

            return completeList.SelectDistinct()
                .Where(c => c.Fitness != null && c.Fitness.Value > 0)
                .OrderByDescending(c => c.Fitness.Value)
                .Cast<Chromosome>()
                .ToList();
        }
    }
}
