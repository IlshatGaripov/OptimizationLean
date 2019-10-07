using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Optimization.Base;

namespace Optimization.Genetic
{
    /// <summary>
    /// The possible states for a genetic algorithm.
    /// </summary>
    public enum GeneticAlgorithmState
    {
        /// <summary>
        /// The GA has not been started yet.
        /// </summary>
        NotStarted,

        /// <summary>
        /// The GA has been started and is running.
        /// </summary>
        Started,

        /// <summary>
        /// The GA has been stopped and is not running.
        /// </summary>
        Stopped,

        /// <summary>
        /// The GA has been resumed after a stop or termination reach and is running.
        /// </summary>
        Resumed,

        /// <summary>
        /// The GA has reach the termination condition and is not running.
        /// </summary>
        TerminationReached
    }

    /// <summary>
    /// This is a custom implementation of genetic algorithm from Genetic Sharp library
    /// As the library file (GeneticAlgorithm) needed minor modification to solve our problem. 
    /// </summary>
    public sealed class GeneticAlgorithm : IGeneticAlgorithm
    {
        private bool m_stopRequested;
        private readonly object m_lock = new object();
        private GeneticAlgorithmState m_state;
        private Stopwatch m_stopwatch;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneticAlgorithm"/> class.
        /// </summary>
        /// <param name="population">The chromosomes population.</param>
        /// <param name="fitness">The fitness evaluation function.</param>
        /// <param name="taskExecutor">Task executor.</param>
        public GeneticAlgorithm(
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
        public event EventHandler<GenerationRanEventArgs> GenerationRan;

        /// <summary>
        /// Occurs when termination reached.
        /// </summary>
        public event EventHandler<TerminationReachedEventArgs> TerminationReached;

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
        /// Gets or sets the selection operator. Default is Roulette Wheel.
        /// </summary>
        public ISelection Selection { get; set; } = new RouletteWheelSelection();

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
        /// Gets or sets the termination condition. Default is 100 gen termination.
        /// </summary>
        public ITermination Termination { get; set; } = new GenerationNumberTermination(100);

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
            if (CrossoverParentsNumber == 0 ||
                Math.Abs(MutationProbability) < 0.01||
                Math.Abs(CrossoverMixProbability) < 0.01)
            {
                throw new ArgumentException("Checking failed. One of the arguments is zero.");
            }

            // Add operator to CrossoverCollection ->
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
        /// If genetic algorithm was not explicit Stop (calling Stop method), you will need provide a new extended Termination.
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
                if (EvaluateChooseBestAndFireEvents())
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

                    // Create next generation ->
                    terminationConditionReached = CreateNextGeneration();

                    m_stopwatch.Stop();
                    TimeEvolving += m_stopwatch.Elapsed;
                }
                while (!terminationConditionReached);
            }
            catch (Exception e)
            {
                // QC log handler
                Shared.Logger.Error(e.Message);
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
        private bool CreateNextGeneration()
        {
            List<IChromosome> list;
            if (Population.CurrentGeneration.IsFruitless)
            {
                // If previous generation has not got enough positive fit chromosomes use a simple
                // strategy to form the next generation: generate in random the initial amount
                list = Population.GenerateChromosomes();

                // Plus add to the list that scarce amount of good chromosomes from past gen ->
                list.AddRange(Population.CurrentGeneration.Chromosomes);
            }
            else
            {
                // Otherwise use custom GA means to form new generation:
                // mutations, crossovers, elite selection, etc. ->
                list = CreateChildren();
            }

            // Create new generation
            Population.CreateNewGeneration(list);

            // Evaluate, choose best and fire events ->
            return EvaluateChooseBestAndFireEvents();
        }

        /// <summary>
        /// Calculates fitness for all chromosomes, decides which chromosomes to leave, raises events.
        /// Raises <see cref="GenerationRan"/> and <see cref="TerminationReached"/> events.
        /// </summary>
        /// <returns>True if termination has been reached, otherwise false.</returns>
        private bool EvaluateChooseBestAndFireEvents()
        {
            // Calculate fitness for all the chomosomes in Current Generation ->
            EvaluateFitness();

            // Analyze the chromosomes fitness results, select best ->
            Population.OnEvaluationCompleted();

            // Raise Generation ran event ->
            GenerationRan?.Invoke(this, new GenerationRanEventArgs(Population.CurrentGeneration));

            // Check if termination is reached and raise event if reached ->
            if (Termination.HasReached(this))
            {
                State = GeneticAlgorithmState.TerminationReached;
                TerminationReached?.Invoke(this, new TerminationReachedEventArgs(Population)); 
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
        /// Creates next generation solutions candidates.
        /// </summary>
        /// <returns>List containit offspring</returns>
        private List<IChromosome> CreateChildren()
        {
            var offspring = new List<IChromosome>();   // Container to hold the offspring

            // Select parents for crossover/mutations ->
            var parents = SelectParents(CrossoverParentsNumber);

            // Until offspring size is less than max ->
            while (offspring.Count < Population.GenerationMaxSize)
            {
                // Select random crossover operator, select random parents, apply ->
                var temp = RandomCrossover(parents);
                offspring.AddRange(temp);

                // To increase diversity apply mutation to results of crossover ->
                Mutate(temp);
                offspring.AddRange(temp);
            }
            
            // If 20 % is less than 1 unit choose just a single solution ->
            var numberOfBest = (int)(0.2 * Population.GenerationMaxSize);
            numberOfBest = numberOfBest > 1 ? numberOfBest : 1;

            // Chromosomes are ordered by fitness desc so just take ->
            var elite = Chromosomes.Take(numberOfBest);
            offspring.AddRange(elite);

            // Change best chromosome's every gene to random value three times ->
            var best = Population.BestChromosome;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < best.Length; j++)
                {
                    // Clone, replace specific gene with random value, add to collection ->
                    var temp = best.CreateNew();
                    IndexedGeneMutation(temp, j);
                    offspring.Add(temp);
                }
            }

            return offspring;
        }

        /// <summary>
        /// Evaluates the fitness.
        /// </summary>
        private void EvaluateFitness()
        {
            try
            {
                var withoutFitness = Population.CurrentGeneration.Chromosomes.Where(c => !c.Fitness.HasValue).ToList();
                var haveFitness = Population.CurrentGeneration.Chromosomes.Where(c => c.Fitness.HasValue).ToList();

                // Inform how many solutions we send for evaluation and dates ->
                var leanFit = (LeanFitness) Fitness;
                Shared.Logger.Trace($"EvaluateFitness(): Sending {withoutFitness.Count} for backtest and {haveFitness.Count} have got fitness");
                Shared.Logger.Trace($"Period: {leanFit.StartDate:M/d/yy} to {leanFit.EndDate:M/d/yy}");

                foreach (var c in withoutFitness)
                {
                    TaskExecutor.Add(() => { c.Fitness = Fitness.Evaluate(c); } );
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
        /// Select parents
        /// </summary>
        /// <param name="number">Number of parents chromosomes to select.</param>
        /// <returns></returns>
        private IList<IChromosome> SelectParents(int number)
        {
            // perform selection
            return Selection.SelectChromosomes(number, Population.CurrentGeneration);
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
        private void Mutate(IEnumerable<IChromosome> chromosomes)
        {
            foreach (var c in chromosomes)
            {
                Mutation.Mutate(c, MutationProbability);
            }
        }
    }

    /// <summary>
    /// Event args class passed to <see cref="GeneticAlgorithm"/> TerminationReached event
    /// </summary>
    public class TerminationReachedEventArgs : EventArgs
    {
        /// <summary>
        /// Population that contains information of all generations of GA.
        /// </summary>
        public PopulationBase Pupulation { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public TerminationReachedEventArgs(PopulationBase population)
        {
            Pupulation = population;
        }
    }

    /// <summary>
    /// Event args class passed to <see cref="GeneticAlgorithm"/> GenerationRan event
    /// </summary>
    public class GenerationRanEventArgs : EventArgs
    {
        /// <summary>
        /// Final Generation that ran.
        /// </summary>
        public Generation Generation;

        /// <summary>
        /// Constructor
        /// </summary>
        public GenerationRanEventArgs(Generation generation)
        {
            Generation = generation;
        }
    }

}

