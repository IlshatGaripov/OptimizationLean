using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{

    public class MaximizerManager : IOptimizerManager
    {
        public const string Termination = "Termination Reached.";
        private readonly IFitness _fitness = new OptimizerFitness();

        public void Start()
        {
            GeneFactory.Initialize(Program.Config.Genes);
            var chromosome = new Chromosome(Program.Config.Genes.Length);
            _fitness.Evaluate(chromosome);

            Program.Logger.Info(Termination);

            var best = ((Chromosome)((SharpeMaximizer)_fitness).Best);

            var info =
                $"Algorithm: {Program.Config.AlgorithmTypeName}, Fitness: {chromosome.Fitness}, Best: {best.ToKeyValueString()}";

            Program.Logger.Info(info);
        }

    }

}
