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
        
        /// <summary>
        /// An executor.
        /// </summary>
        private ParallelTaskExecutor _executor;

        /// <summary>
        /// Population.
        /// </summary>
        private IPopulation _population;

        /// <summary>
        /// GA fitness.
        /// </summary>
        private OptimizerFitness _fitness;

        /// <summary>
        /// Best chromosome.
        /// </summary>
        private Chromosome _bestChromosome;

        /// <summary>
        /// Init the class variables. 
        /// </summary>
        public void Initialize(OptimizerFitness fitness)
        {
            _fitness = fitness;
            
            var maxTreads = Program.Config.MaxThreads;
            _executor = new ParallelTaskExecutor
            {
                MinThreads = 1, MaxThreads = maxTreads > 0 ? maxTreads : 4
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
            IList<IChromosome> chromosomes = new List<IChromosome>();
            
            // create chromosomes and add them to list.
            for (var i = 0; i < Program.Config.PopulationSize; i++)
            {
                chromosomes.Add(new Chromosome(GeneFactory.GeneConfigArray));
            }

            // create a population from the pre-defined list of chromosomes.
            _population = new PreloadPopulation(chromosomes);

            // to init GA params
            var selection = new TournamentSelection();
            var crossover = Program.Config.OnePointCrossover ? new OnePointCrossover() : new TwoPointCrossover();
            var mutation = new UniformMutation(true);
            var termination = new OrTermination(new FitnessStagnationTermination(Program.Config.StagnationGenerations),
                new GenerationNumberTermination(Program.Config.Generations));

            // create the GA itself . Object of custom type contained in GeneticSharpExtensions.
            var ga = new GeneticAlgorithmCustom(_population, _fitness, selection, crossover, mutation)
            {
                TaskExecutor = _executor,
                Termination = termination,
                Reinsertion = new ElitistReinsertion(),
                MutationProbability = Program.Config.MutationProbability,
                CrossoverProbability = Program.Config.CrossoverProbability
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
                Program.Config.AlgorithmTypeName, _population.GenerationsNumber, _bestChromosome.Fitness,
                _fitness.Name, _bestChromosome.ToKeyValueString(), _bestChromosome.Id);
        }

    }
}
