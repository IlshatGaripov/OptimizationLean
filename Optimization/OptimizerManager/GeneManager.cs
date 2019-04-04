﻿using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Threading;
using System;

namespace Optimization
{
    /// <summary>
    /// The one that manages the whole genetic optimization process. 
    /// </summary>
    public class GeneManager : IOptimizerManager
    {
        public const string Termination = "Termination Reached.";
        
        //executor
        private ITaskExecutor _executor;

        private IPopulation _population;
        private OptimizerFitness _fitness;
        private ITermination _termination;
        private ISelection _selection;
        private ICrossover _crossover;
        private IMutation _mutation;
        private IReinsertion _reinsertion;

        /// <summary>
        /// Init the class variables. 
        /// </summary>
        public void Initialize(OptimizerFitness fitness)
        {
            // fitness
            _fitness = fitness;

            // params to init GA common to different optimization modes
            _selection = new TournamentSelection();
            _crossover = Program.Config.OnePointCrossover ? new OnePointCrossover() : new TwoPointCrossover();
            _mutation = new UniformMutation(true);
            _reinsertion = new ElitistReinsertion();

            // params optimization method specific 
            switch (Program.Config.Mode)
            {
                case OptimizationMode.BruteForce:
                {
                    // TODO: implement population class for brute force optim.
                    _population = new PreloadPopulation();

                    _termination = new GenerationNumberTermination(1);
                    break;
                }

                case OptimizationMode.GeneticAlgorithm:
                {
                    // create population from randomly generated list
                    _population = new PreloadPopulation();

                    _termination = new OrTermination(new FitnessStagnationTermination(Program.Config.StagnationGenerations), 
                        new GenerationNumberTermination(Program.Config.Generations));
                    break;
                }
                    
                default:
                    throw new Exception("Termination method could not be initialized");
            }
            
            // executor
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
            
            // create the GA itself . Object of custom type (contained in GeneticSharpExtensions folder).
            var ga = new GeneticAlgorithmCustom(_population, _fitness, _selection, _crossover, _mutation)
            {
                TaskExecutor = _executor,
                Termination = _termination,
                Reinsertion = _reinsertion,
                MutationProbability = Program.Config.MutationProbability,
                CrossoverProbability = Program.Config.CrossoverProbability
            };

            //subscribe to events
            ga.GenerationRan += GenerationRan;
            ga.TerminationReached += TerminationReached;

            //run the GA 
            ga.Start();
        }

        /// <summary>
        /// Handler called by the end of optimization algorithm
        /// </summary>
        private void TerminationReached(object sender, EventArgs e)
        {
            Program.Logger.Info(Termination);

            GenerationRan(null, null);
        }

        /// <summary>
        /// Handler called at the end of next generation
        /// </summary>
        private void GenerationRan(object sender, EventArgs e)
        {
            /*
            //keep first iteration of alpha to maintain id
            if (_bestChromosome == null || _population.BestChromosome.Fitness > _bestChromosome?.Fitness)
            {
                _bestChromosome = (Chromosome)_population.BestChromosome;
            }
            */

            // we don't need _bestChromosome value as _population.BestChromosome must maintain the best solution according to the interface (!)
            // so that is a duplicate

            /*
            Program.Logger.Info("Algorithm: {0}, Generation: {1}, Fitness: {2}, {3}: {4}, Id: {5}",
                Program.Config.AlgorithmTypeName, _population.GenerationsNumber, _bestChromosome.Fitness,
                _fitness.Name, _bestChromosome.ToKeyValueString(), _bestChromosome.Id);
            */
        }

    }
}
