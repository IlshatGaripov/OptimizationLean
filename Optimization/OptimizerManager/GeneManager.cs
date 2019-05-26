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
    public class GeneManager : IOptimizerManager
    {
        // Termination Reached! 
        public const string Termination = "Termination Reached.";

        // Genetic Sharp objects 
        private readonly ITaskExecutor _executor;
        private readonly IPopulation _population;
        private readonly IFitness _fitness;
        private readonly ITermination _termination;
        private readonly ISelection _selection;
        private readonly ICrossover _crossover;
        private readonly IMutation _mutation;
        private readonly IReinsertion _reinsertion;

        /// <summary>
        /// Init the class variables. 
        /// </summary>
        public GeneManager()
        {
            // params to init GA common to different optimization modes
            _selection = new TournamentSelection();
            _crossover = Program.Config.OnePointCrossover ? new OnePointCrossover() : new TwoPointCrossover();
            _mutation = new UniformMutation(true);
            _reinsertion = new ElitistReinsertion();

            // Max threads
            var maxThreads = Program.Config.MaxThreads > 0 ? Program.Config.MaxThreads : 8;

            switch (Program.Config.ExecutionMode)
            {
                case ExecutionMode.Linear:
                    _executor = new LinearTaskExecutor();
                    _fitness = new OptimizerFitness();
                    break;

                case ExecutionMode.Parallel:
                    _executor = new ParallelTaskExecutor { MaxThreads = maxThreads };
                    _fitness = new OptimizerFitness();
                    break;

                case ExecutionMode.Azure:
                    _executor = new TaskExecutorAzure { MaxThreads = maxThreads };
                    _fitness = new AzureFitness();
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
                        _population = new PopulationCartesian();
                        _termination = new GenerationNumberTermination(1);

                        break;
                    }

                case OptimizationMode.GeneticAlgorithm:
                    {
                        // create random population
                        _population = new PopulationRandom();
                        _termination = new OrTermination(
                            new FitnessStagnationTermination(Program.Config.StagnationGenerations),
                            new GenerationNumberTermination(Program.Config.Generations));

                        break;
                    }

                default:
                    throw new Exception("Optimization mode specific objects were not initialized");
            }
        }

        /// <summary>
        /// Starts an optimization. 
        /// </summary>
        public void Start()
        {
            switch (_executor)
            {
                case TaskExecutorAzure _:
                    // Deploy Batch resources
                    AzureBatchManager.DeployAsync().Wait();
                    break;
                case null:
                    throw new Exception("Executor was not initialized");
            }

            // Create the GA itself
            var ga = new GeneticAlgorithmCustom(_population, _fitness, _selection, _crossover, _mutation)
            {
                TaskExecutor = _executor,
                Termination = _termination,
                Reinsertion = _reinsertion,
                MutationProbability = Program.Config.MutationProbability,
                CrossoverProbability = Program.Config.CrossoverProbability
            };

            // Subscribe to events
            ga.GenerationRan += GenerationRan;
            ga.TerminationReached += TerminationReached;

            // Run the GA 
            ga.Start();
        }

        /// <summary>
        /// Handler called by the end of optimization algorithm
        /// </summary>
        private void TerminationReached(object sender, EventArgs e)
        {
            GenerationRan(null, null);

            Program.Logger.Info(Termination);

            // Clean up Batch resources
            if (_executor is TaskExecutorAzure)
            {
                AzureBatchManager.FinalizeAsync().Wait();
            }
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        private void GenerationRan(object sender, EventArgs e)
        {
            
        }
    }
}
