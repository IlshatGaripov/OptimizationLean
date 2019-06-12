using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Populations;
using GeneticSharp.Domain.Reinsertions;
using GeneticSharp.Domain.Selections;
using GeneticSharp.Domain.Terminations;
using GeneticSharp.Infrastructure.Framework.Commons;
using GeneticSharp.Infrastructure.Framework.Texts;
using GeneticSharp.Infrastructure.Framework.Threading;

namespace Optimization
{
    /// <summary>
    /// This is a custom implementation of genetic algorithm from Genetic Sharp library
    /// As the library file (GeneticAlgorithm) needed minor modification to solve our problem. 
    /// </summary>
    public sealed class GeneticAlgorithmCustom : IGeneticAlgorithm
    {
        /// <summary>
        /// The default crossover probability.
        /// </summary>
        public const float DefaultCrossoverProbability = 0.75f;

        /// <summary>
        /// The default mutation probability.
        /// </summary>
        public const float DefaultMutationProbability = 0.1f;
        
        private bool m_stopRequested;
        private readonly object m_lock = new object();
        private GeneticAlgorithmState m_state;
        private Stopwatch m_stopwatch;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticSharp.Domain.GeneticAlgorithm"/> class.
        /// </summary>
        /// <param name="population">The chromosomes population.</param>
        /// <param name="fitness">The fitness evaluation function.</param>
        /// <param name="taskExecutor">Task executor.</param>
        public GeneticAlgorithmCustom(
                          PopulationBase population,
                          IFitness fitness,
                          ITaskExecutor taskExecutor)
        {
            Population = population;
            Fitness = fitness;
            TaskExecutor = taskExecutor;

            // Initial state values ->
            TimeEvolving = TimeSpan.Zero;
            State = GeneticAlgorithmState.NotStarted;

            // Defaults ->
            CrossoverCollection = new ICrossover[] {new TwoPointCrossover(), new CycleCrossover() };

            // Default values ->
            CrossoverProbability = DefaultCrossoverProbability;
            MutationProbability = DefaultMutationProbability;
        }
        
        /// <summary>
        /// Occurs when generation ran.
        /// </summary>
        public event EventHandler GenerationRan;

        /// <summary>
        /// Occurs when termination reached.
        /// </summary>
        public event EventHandler TerminationReached;

        /// <summary>
        /// Occurs when stopped.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Gets the population.
        /// </summary>
        /// <value>The population.</value>
        public PopulationBase Population { get; }

        /// <summary>
        /// Gets the fitness function.
        /// </summary>
        public IFitness Fitness { get; }

        /// <summary>
        /// Gets or sets the selection operator.
        /// </summary>
        public ISelection Selection { get; set; }

        /// <summary>
        /// Gets or sets the crossover operator.
        /// </summary>
        /// <value>The crossover.</value>
        public ICrossover[] CrossoverCollection { get; set; }

        /// <summary>
        /// Gets or sets the crossover probability.
        /// </summary>
        public float CrossoverProbability { get; set; }

        /// <summary>
        /// Gets or sets the mutation operator.
        /// </summary>
        public IMutation Mutation { get; set; }

        /// <summary>
        /// Gets or sets the mutation probability.
        /// </summary>
        public float MutationProbability { get; set; }

        /// <summary>
        /// Gets or sets the termination condition.
        /// </summary>
        public ITermination Termination { get; set; }

        /// <summary>
        /// Gets or sets the task executor which will be used to execute fitness evaluation.
        /// </summary>
        public ITaskExecutor TaskExecutor { get; set; }

        /// <summary>
        /// Gets the generations number.
        /// </summary>
        /// <value>The generations number.</value>
        public int GenerationsNumber
        {
            get
            {
                return Population.GenerationsNumber;
            }
        }

        /// <summary>
        /// Gets the best chromosome.
        /// </summary>
        /// <value>The best chromosome.</value>
        public IChromosome BestChromosome
        {
            get
            {
                return Population.BestChromosome;
            }
        }

        /// <summary>
        /// Gets the time evolving.
        /// </summary>
        public TimeSpan TimeEvolving { get; private set; }

        /// <summary>
        /// Gets the state.
        /// </summary>
        public GeneticAlgorithmState State
        {
            get
            {
                return m_state;
            }

            private set
            {
                var shouldStop = Stopped != null && m_state != value && value == GeneticAlgorithmState.Stopped;

                m_state = value;

                if (shouldStop)
                {
                    Stopped?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is running.
        /// </summary>
        /// <value><c>true</c> if this instance is running; otherwise, <c>false</c>.</value>
        public bool IsRunning
        {
            get
            {
                return State == GeneticAlgorithmState.Started || State == GeneticAlgorithmState.Resumed;
            }
        }

        /// <summary>
        /// Starts the genetic algorithm using population, fitness, selection, crossover, mutation and termination configured.
        /// </summary>
        public void Start()
        {
            // Check that all properties have been explicitly specified ->
            ExceptionHelper.ThrowIfNull("selection", Selection);
            ExceptionHelper.ThrowIfNull("crossover", CrossoverCollection);
            ExceptionHelper.ThrowIfNull("mutation", Mutation);
            ExceptionHelper.ThrowIfNull("termination", Termination);

            lock (m_lock)
            {
                State = GeneticAlgorithmState.Started;
                m_stopwatch = Stopwatch.StartNew();

                // Create initial Generation ->
                Population.CreateInitialGeneration();

                m_stopwatch.Stop();
                TimeEvolving = m_stopwatch.Elapsed;
            }

            Resume();
        }

        /// <summary>
        /// Resumes the last evolution of the genetic algorithm.
        /// <remarks>
        /// If genetic algorithm was not explicit Stop (calling Stop method), you will need provide a new extended Termination.
        /// </remarks>
        /// </summary>
        public void Resume()
        {
            try
            {
                lock (m_lock)
                {
                    m_stopRequested = false;
                }

                if (Population.GenerationsNumber == 0)
                {
                    throw new InvalidOperationException("Attempt to resume a genetic algorithm which was not yet started.");
                }

                if (Population.GenerationsNumber > 1)
                {
                    if (Termination.HasReached(this))
                    {
                        throw new InvalidOperationException("Attempt to resume a genetic algorithm with a termination ({0}) already reached. Please, specify a new termination or extend the current one.".With(Termination));
                    }

                    State = GeneticAlgorithmState.Resumed;
                }

                // this will return true if termination has reached at the end of current generation
                // as instance, when the GenerationNumberTermination is set to 1 (the case of brute force optimization)
                // generation must not evolve further and the Resume() will return ->
                if (PerformEvolution())
                {
                    return;
                }

                bool terminationConditionReached = false;

                do
                {
                    if (m_stopRequested)
                    {
                        break;
                    }

                    m_stopwatch.Restart();

                    // Create descendants from current generation and perform evolution ->
                    terminationConditionReached = CreateChildrenAndPerformEvolution();

                    m_stopwatch.Stop();
                    TimeEvolving += m_stopwatch.Elapsed;
                }
                while (!terminationConditionReached);
            }
            catch
            {
                State = GeneticAlgorithmState.Stopped;
                throw;
            }
        }

        /// <summary>
        /// Stops the genetic algorithm..
        /// </summary>
        public void Stop()
        {
            if (Population.GenerationsNumber == 0)
            {
                throw new InvalidOperationException("Attempt to stop a genetic algorithm which was not yet started.");
            }

            lock (m_lock)
            {
                m_stopRequested = true;
            }
        }

        /// <summary>
        /// Evolve one generation.
        /// </summary>
        /// <returns>True if termination has been reached, otherwise false.</returns>
        private bool CreateChildrenAndPerformEvolution()
        {
            // Create a container for offspring ->
            var offspring = new List<IChromosome>();

            // Select the chromosomes to be origin for crossovers and mutations ->
            var parents = Selection.SelectChromosomes(Population.ParentsQuantity, Population.CurrentGeneration);

            // TODO: implement crossover and mutations cycle.
            //while() ..

            // Create new generation and assign it to CurrentGeneration ->
            Population.CreateNewGeneration(offspring);

            return PerformEvolution();
        }

        /// <summary>
        /// Calculates fitness and selects chromosomes for next evolution.
        /// Raises <see cref="GenerationRan"/> and <see cref="TerminationReached"/> events.
        /// </summary>
        /// <returns>True if termination has been reached, otherwise false.</returns>
        private bool PerformEvolution()
        {
            // Calculate fitness for all the chomosomes in Current Generation ->
            EvaluateFitness();

            // Perform whatever actions need to be done over the population after the evalutation ->
            Population.EndCurrentGeneration();

            // Inform one step of evolution has been accomplished ->
            var handler = GenerationRan;
            handler?.Invoke(this, EventArgs.Empty);

            // Check if termination is reached ->
            if (Termination.HasReached(this))
            {
                State = GeneticAlgorithmState.TerminationReached;

                handler = TerminationReached;
                handler?.Invoke(this, EventArgs.Empty);

                return true;
            }

            if (m_stopRequested)
            {
                TaskExecutor.Stop();
                State = GeneticAlgorithmState.Stopped;
            }

            return false;
        }

        /// <summary>
        /// Evaluates the fitness.
        /// </summary>
        private void EvaluateFitness()
        {
            try
            {
                var chromosomesWithoutFitness = Population.CurrentGeneration.Chromosomes.Where(c => !c.Fitness.HasValue).ToList();

                foreach (var c in chromosomesWithoutFitness)
                {
                    TaskExecutor.Add(() =>
                    {
                        RunEvaluateFitness(c);
                    });
                }

                if (!TaskExecutor.Start())
                {
                    throw new TimeoutException("The fitness evaluation reached the {0} timeout.".With(TaskExecutor.Timeout));
                }
            }
            finally
            {
                TaskExecutor.Stop();
                TaskExecutor.Clear();
            }
        }

        /// <summary>
        /// Runs the evaluate fitness.
        /// </summary>
        /// <param name="chromosome">The chromosome.</param>
        private void RunEvaluateFitness(IChromosome chromosome)
        {
            try
            {
                chromosome.Fitness = Fitness.Evaluate(chromosome);
            }
            catch (Exception ex)
            {
                throw new FitnessException(Fitness, "Error executing Fitness.Evaluate for chromosome: {0}".With(ex.Message), ex);
            }
        }

        /*
        /// <summary>
        /// Crosses the specified parents.
        /// </summary>
        /// <param name="parents">The parents.</param>
        /// <returns>The result chromosomes.</returns>
        private IList<IChromosome> Cross(IList<IChromosome> parents)
        {
            return OperatorsStrategy.Cross(Population, Crossover, CrossoverProbability, parents);
        }

        /// <summary>
        /// Mutate the specified chromosomes.
        /// </summary>
        /// <param name="chromosomes">The chromosomes.</param>
        private void Mutate(IList<IChromosome> chromosomes)
        {
            OperatorsStrategy.Mutate(Mutation, MutationProbability, chromosomes);
        }
        
        */
    }
}

