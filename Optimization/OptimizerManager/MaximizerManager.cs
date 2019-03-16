namespace Optimization
{

    public class MaximizerManager : IOptimizerManager
    {
        public const string Termination = "Termination Reached.";
        private IOptimizerConfiguration _config;
        private OptimizerFitness _fitness;

        public void Initialize(IOptimizerConfiguration config, OptimizerFitness fitness)
        {
            _config = config;
            _fitness = fitness;
            // _executor.MaxThreads = _config.MaxThreads > 0 ? _config.MaxThreads : 8;
        }

        public void Start()
        {
            GeneFactory.Initialize(_config.Genes);
            var chromosome = new Chromosome(false, GeneFactory.Config);
            _fitness.Evaluate(chromosome);

            Program.Logger.Info(Termination);

            var best = ((Chromosome)((SharpeMaximizer)_fitness).Best);

            var info = $"Algorithm: {_config.AlgorithmTypeName}, Fitness: {chromosome.Fitness}, {_fitness.Name}: " +
            $"{_fitness.GetValueFromFitness(chromosome.Fitness).ToString("F")}, {best.ToKeyValueString()}";

            Program.Logger.Info(info);
        }

    }

}
