using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using System;
using System.Collections.Generic;

namespace Optimization
{
    /// <summary>
    /// The one that manages the whole genetic optimization process. 
    /// </summary>
    public class GeneManager : IOptimizerManager
    {
        public const string Termination = "Termination Reached.";

        // this is now made global (static)
        private IOptimizerConfiguration _config;

        private ParallelTaskExecutor _executor;
        private Population _population;
        private OptimizerFitness _fitness;
        private Chromosome _bestChromosome;

        /// <summary>
        /// Init the class variables. 
        /// </summary>
        public void Initialize(IOptimizerConfiguration config, OptimizerFitness fitness)
        {
            _config = config;
            _fitness = fitness;
            _executor = new ParallelTaskExecutor
            {
                MinThreads = 1, MaxThreads = _config.MaxThreads > 0 ? _config.MaxThreads : 8
            };
        }

        /// <summary>
        /// Start the optimization. The core method.
        /// </summary>
        public void Start()
        {           
            if (_executor == null)
            {
                throw new Exception("Executor was not initialized");
            }

            // list to store the chromosomes
            IList<IChromosome> list = new List<IChromosome>();

            // GeneFactory generates the chromosome genes.
            GeneFactory.Initialize(_config.Genes);

            // creates chromosomes 
            for (var i = 0; i < _config.PopulationSize; i++)
            {
                //first chromosome always use actuals. For others decide by config
                var isActual = i == 0 || _config.UseActualGenesForWholeGeneration;

                list.Add(new Chromosome(isActual, GeneFactory.GeneConfigArray));
            }

            var max = _config.PopulationSizeMaximum < _config.PopulationSize ? _config.PopulationSize * 2 : _config.PopulationSizeMaximum;

            // create the population
            _population = new PreloadPopulation(_config.PopulationSize, max, list)
            {
                GenerationStrategy = new PerformanceGenerationStrategy()
            };

            //create the GA itself 
            var ga = new GeneticAlgorithm(_population, _fitness, new TournamentSelection(),
                _config.OnePointCrossover ? new OnePointCrossover() : new TwoPointCrossover(), new UniformMutation(true))
            {
                TaskExecutor = _executor,
                Termination = new OrTermination(new FitnessStagnationTermination(_config.StagnationGenerations), new GenerationNumberTermination(_config.Generations)),
                Reinsertion = new ElitistReinsertion(),
                MutationProbability = _config.MutationProbability,
                CrossoverProbability = _config.CrossoverProbability
            };

            //subscribe to events
            ga.GenerationRan += GenerationRan;
            ga.TerminationReached += TerminationReached;

            //run the GA 
            ga.Start();
        }

        private void TerminationReached(object sender, EventArgs e)
        {
            Program.Logger.Info(Termination);

            GenerationRan(null, null);
        }

        private void GenerationRan(object sender, EventArgs e)
        {
            //keep first iteration of alpha to maintain id
            if (_bestChromosome == null || _population.BestChromosome.Fitness > _bestChromosome?.Fitness)
            {
                _bestChromosome = (Chromosome)_population.BestChromosome;
            }

            Program.Logger.Info("Algorithm: {0}, Generation: {1}, Fitness: {2}, {3}: {4}, Id: {5}", 
                _config.AlgorithmTypeName, _population.GenerationsNumber, _bestChromosome.Fitness,
                _fitness.Name, _bestChromosome.ToKeyValueString(), _bestChromosome.Id);
        }

    }
}
