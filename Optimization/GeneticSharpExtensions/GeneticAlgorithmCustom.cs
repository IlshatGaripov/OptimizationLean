using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GeneticSharp.Domain;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Crossovers;
using GeneticSharp.Domain.Fitnesses;
using GeneticSharp.Domain.Mutations;
using GeneticSharp.Domain.Randomizations;
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

            // Init mutation - Will replace a gene at random position with a new randomly generated gene ->
            Mutation = new UniformMutation();

            // Collection of crossover operators to use ->
            CrossoverCollection = new List<ICrossover>();
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
        /// Collection of crossover operators.
        /// </summary>
        public List<ICrossover> CrossoverCollection { get; set; }

        /// <summary>
        /// Default mutation operator.
        /// </summary>
        public IMutation Mutation { get; set; }

        /// <summary>
        /// Gets or sets the mutation probability.
        /// </summary>
        public float MutationProbability { get; set; }

        /// <summary>
        /// Number of parents to select for crossovers.
        /// </summary>
        public int CrossoverParentsNumber { get; set; }

        /// <summary>
        /// Probabiliy to swap genes in UniformCrossover.
        /// </summary>
        public float CrossoverMixProbability { get; set; }

        /// <summary>
        /// Max number of chromosomes each generation to contain.
        /// </summary>
        public int GenerationMaxSize { get; set; }

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
        public int GenerationsNumber => Population.GenerationsNumber;

        /// <summary>
        /// Gets the best chromosome.
        /// </summary>
        public IChromosome BestChromosome => Population.BestChromosome;

        /// <summary>
        /// Gets the current chromosomes list
        /// </summary>
        public IList<IChromosome> Chromosomes
        {
            get => Population.CurrentGeneration.Chromosomes;
            set => Population.CurrentGeneration.Chromosomes = value;
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
            get => m_state;

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
        /// Starts the genetic optimization algorithm.
        /// </summary>
        public void Start()
        {
            // Check that all properties have been explicitly specified ->
            ExceptionHelper.ThrowIfNull("selection", Selection);
            ExceptionHelper.ThrowIfNull("termination", Termination);

            // Throw if one of the variables is zero -> 
            if (GenerationMaxSize == 0 ||
                CrossoverParentsNumber == 0 ||
                Math.Abs(MutationProbability) < 0.01||
                Math.Abs(CrossoverMixProbability) < 0.01)
            {
                throw new ArgumentException("Checking failed. One of the arguments is zero.");
            }
            
            // Init Uniform Crossover op. ->
            CrossoverCollection.Add(new UniformCrossover(CrossoverMixProbability));

            lock (m_lock)
            {
                State = GeneticAlgorithmState.Started;
                m_stopwatch = Stopwatch.StartNew();

                // Create initial Generation ->
                Population.CreateInitialGeneration();

                m_stopwatch.Stop();
                TimeEvolving = m_stopwatch.Elapsed;
            }

            // -> Continue
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

                if (Population.GenerationsNumber > 1)
                {
                    if (Termination.HasReached(this))
                    {
                        throw new InvalidOperationException("Attempt to resume a genetic algorithm with a termination ({0}) already reached. Please, specify a new termination or extend the current one.".With(Termination));
                    }

                    State = GeneticAlgorithmState.Resumed;
                }

                // This will return true if termination has reached.
                // For instance, when the GenerationNumberTermination is set to 1 (the case of brute force optimization)
                // generation must not evolve further and method will return ->
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
            catch (Exception e)
            {
                Console.WriteLine(e);
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
            // Create container for the offspring ->
            var offspring = new List<IChromosome>();

            // Select the chromosomes to be origin for crossovers and mutations ->
            var parents = SelectParents(CrossoverParentsNumber);

            while (offspring.Count < GenerationMaxSize)
            {
                // Select random crossover operator, select random parents 
                // apply the operator, add result to offspring collection ->
                var temp = RandomCrossover(parents);
                offspring.AddRange(temp);

                // To increase diversity apply mutation to results of crossover and add to offspring ->
                Mutate(temp);
                offspring.AddRange(temp);
            }

            // Add 10 % of elite. If 10 % is less than 1 choose just a best chromosome ->
            var numberOfBest = (int)(0.1 * GenerationMaxSize);
            numberOfBest = numberOfBest > 1 ? numberOfBest : 1;

            // Just take. Chromosomes should have been already ordered by desc ->
            var elite = Chromosomes.Take(numberOfBest);
            offspring.AddRange(elite);

            // Mutate best chromosome's genes three times ->
            var best = Population.BestChromosome;
            for(int i = 0; i < 3; i++)
            {
                for(int j = 0; j < best.Length; j++)
                {
                    // Clone, replace specific gene with random value, add to collection ->
                    var temp = best.CreateNew();
                    IndexedGeneMutation(temp, j);
                    offspring.Add(temp);
                }
            }

            // Create new generation and assign it to CurrentGeneration ->
            Population.CreateNewGeneration(offspring);

            // Evaluate, choose best and inform ->
            return PerformEvolution();
        }

        /// <summary>
        /// Calculates fitness for all chromosomes, decides which chromosomes to leave, raises events.
        /// Raises <see cref="GenerationRan"/> and <see cref="TerminationReached"/> events.
        /// </summary>
        /// <returns>True if termination has been reached, otherwise false.</returns>
        private bool PerformEvolution()
        {
            // Calculate fitness for all the chomosomes in Current Generation ->
            EvaluateFitness();

            // Leave only the values that have positive fitness and order by descending ->
            Chromosomes = Chromosomes
                .Where(c => c.Fitness != null && c.Fitness.Value > 0)
                .OrderByDescending(c => c.Fitness.Value)
                .ToList();

            // Truncate if amount is more than max size ->
            if (Chromosomes.Count > GenerationMaxSize)
            {
                Chromosomes = Chromosomes.Take(GenerationMaxSize).ToList();
            }

            // Trace the best chromosome ->
            Population.RegisterTheBestChromosome();

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
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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

        /// <summary>
        /// Select parents
        /// </summary>
        /// <param name="number">Number of parents chromosomes to select.</param>
        /// <returns></returns>
        private IList<IChromosome> SelectParents(int number)
        {
            // Create Genetic Sharp lib type Generation as it's required by selection methods ->
            var generationGeneticSharp= new GeneticSharp.Domain.Populations.Generation(Population.CurrentGeneration.Number, Chromosomes);

            // Perform selection ->
            return Selection.SelectChromosomes(number, generationGeneticSharp);
        }

        /// <summary>
        /// Crosses the specified parents.
        /// </summary>
        /// <param name="parents">The parents.</param>
        /// <returns>The result chromosomes.</returns>
        private IList<IChromosome> RandomCrossover(ICollection<IChromosome> parents)
        {
            // Select random crossover operator from list ->
            var randomCrossoverIndex = RandomizationProvider.Current.GetInt(0, CrossoverCollection.Count);
            var randomCrossover = CrossoverCollection[randomCrossoverIndex];

            // Get number of parent required for selected operator ->
            var parentsRequired = randomCrossover.ParentsNumber;

            // Select in random the required number of unique parents ->
            var randomParentIndexes = RandomizationProvider.Current.GetUniqueInts(parentsRequired, 0, parents.Count);
            var randomParents = parents.Where((p, i) => randomParentIndexes.Contains(i)).ToList();

            // Apply crossover and return the offspring list ->
            return randomCrossover.Cross(randomParents);
        }

        /// <summary>
        /// Replaces the chromosome's index gene with a new random value
        /// </summary>
        /// <param name="chromosome">Chromosome to mutate</param>
        /// <param name="i">Index to replace a gene at</param>
        private static void IndexedGeneMutation(IChromosome chromosome, int i)
        {
            chromosome.ReplaceGene(i, chromosome.GenerateGene(i));
        }

        /// <summary>
        /// Mutate the specified chromosomes.
        /// </summary>
        /// <param name="chromosomes">The chromosomes.</param>
        private void Mutate(IList<IChromosome> chromosomes)
        {
            foreach (var c in chromosomes)
            {
                Mutation.Mutate(c, MutationProbability);
            }
        }
    }
}

