using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using GeneticSharp.Domain.Chromosomes;
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
        public FitnessScore? FitnessScore { get; set; }

        /// <summary>
        /// Genetic algorithm itself!
        /// using https://stackoverflow.com/questions/34743533/automated-property-with-getter-only-can-be-set-why/34743568
        /// </summary>
        public GeneticAlgorithmCustom GenAlgorithm { get; }

        /// <summary>
        /// Collection of all chromosomes that appeared in GA search that had positive fitness
        /// </summary>
        public IList<Chromosome> GoodChromosomes { get; set; }

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
            switch (Program.Config.TaskExecutionMode)
            {
                case TaskExecutionMode.Linear:
                    executor = new LinearTaskExecutor();
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                case TaskExecutionMode.Parallel:
                    executor = new ParallelTaskExecutor();
                    fitness = new OptimizerFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                case TaskExecutionMode.Azure:
                    executor = new TaskExecutorAzure();
                    fitness = new AzureFitness(StartDate.Value, EndDate.Value, sortCriteria);
                    break;

                default:
                    throw new Exception("Executor initialization failed");
            }

            // Optimization mode ->
            switch (Program.Config.OptimizationMode)
            {
                case OptimizationMode.BruteForce:
                    {
                        // Create cartesian population ->
                        population = new PopulationCartesian(Program.Config.GeneConfigArray);
                        termination = new GenerationNumberTermination(1);

                        break;
                    }

                case OptimizationMode.Genetic:
                    {
                        // Create random population ->
                        population = new PopulationRandom(Program.Config.GeneConfigArray, Program.Config.PopulationInitialSize)
                        {
                            GenerationMaxSize = Program.Config.GenerationMaxSize
                        };

                        // Logical terminaton ->
                        var localTerm = new LogicalOrTermination();

                        localTerm.AddTermination(new FruitlessGenerationsTermination(2));

                        if (Program.Config.Generations.HasValue)
                            localTerm.AddTermination(new GenerationNumberTermination(Program.Config.Generations.Value));

                        if (Program.Config.StagnationGenerations.HasValue)
                            localTerm.AddTermination(new FitnessStagnationTermination(Program.Config.StagnationGenerations.Value));

                        termination = localTerm;
                        break;
                    }

                default:
                    throw new Exception("Optimization mode specific objects were not initialized");
            }

            // Create the GA itself
            // It's important to initialize GA in a constructor as we would
            // like to declare event handlers from outside the class before calling Start()
            GenAlgorithm = new GeneticAlgorithmCustom(population, fitness, executor)
            {
                // Reference type ->
                Selection = selection,
                Termination = termination,

                // Numeric values ->
                CrossoverParentsNumber = Program.Config.CrossoverParentsNumber,
                CrossoverMixProbability = Program.Config.CrossoverMixProbability,
                MutationProbability = Program.Config.MutationProbability
            };
        }

        /// <summary>
        /// Starts an optimization. 
        /// </summary>
        public void Start()
        {
            // Subscribe to GA events ->
            GenAlgorithm.GenerationRan += GenerationRan;
            GenAlgorithm.TerminationReached += TerminationReached;

            // Run the GA 
            GenAlgorithm.Start();
        }

        /// <summary>
        /// Handler called at the end of work of genetic algorithm
        /// </summary>
        private void TerminationReached(object sender, TerminationReachedEventArgs e)
        {
            // Choose all good chromosomes ->
            GoodChromosomes = ChooseGoodChromosomes(e.Pupulation);

            Program.Logger.Trace(" <->");
            Program.Logger.Trace("Termination reached");
            Program.Logger.Trace($"Good chromosomes - Count {GoodChromosomes.Count} - printing :");

            foreach (var c in GoodChromosomes)
            {
                var chromBase = (Chromosome)c;
                Program.Logger.Trace($"{chromBase.Fitness} ## {chromBase.ToKeyValueString()}");
            }
            Program.Logger.Trace(" <->");
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        private void GenerationRan(object sender, GenerationRanEventArgs e)
        {
            Program.Logger.Trace(" <->");
            Program.Logger.Trace($"Generation Ran - Count {e.Generation.Chromosomes.Count} - printing :");

            foreach (var c in e.Generation.Chromosomes)
            {
                var chromBase = (Chromosome) c;
                Program.Logger.Trace($"{chromBase.Fitness} ## {chromBase.ToKeyValueString()}");
            }
            Program.Logger.Trace(" <->");
        }

        /// <summary>
        /// Selects all chromosomes with positive fitness after GA completes its work and ranges them by fitness
        /// </summary>
        /// <param name="population"></param>
        /// <returns></returns>
        private IList<Chromosome> ChooseGoodChromosomes(PopulationBase population)
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
