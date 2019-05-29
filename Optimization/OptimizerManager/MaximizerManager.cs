using GeneticSharp.Domain.Fitnesses;

namespace Optimization
{
    public class MaximizerManager : IOptimizerManager
    {
        public const string Termination = "Termination Reached.";
        private readonly IFitness _fitness = new OptimizerFitness(Program.Config.StartDate.Value, Program.Config.EndDate.Value);

        public void Start()
        {
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
