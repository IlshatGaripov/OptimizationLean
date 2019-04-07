using GeneticSharp.Domain.Chromosomes;

namespace Optimization
{
    public class DualPeriodSharpeFitness : OptimizerFitness
    {
        public DualPeriodSharpeFitness()
        {
            Name = "DualPeriodSharpe";
        }

        public override double Evaluate(IChromosome chromosome)
        {
            var dualConfig = Clone<OptimizerConfiguration>((OptimizerConfiguration)Program.Config);
            var start = Program.Config.StartDate.Value;
            var end = Program.Config.EndDate.Value;
            var diff = end - start;

            dualConfig.StartDate = end;
            dualConfig.EndDate = end + diff;

            var dualFitness = new OptimizerFitness();

            var first = base.Evaluate(chromosome);
            double second = -10;
            if (first > -10)
            {
                second = dualFitness.Evaluate(chromosome);
            }

            var fitness = new FitnessResult
            {
                Fitness = (first + second) / 2
            };
            fitness.Value = (decimal)base.GetAdjustedFitness(fitness.Fitness);

            var output = string.Format($"Start: {Program.Config.StartDate}, End: {Program.Config.EndDate}, Start: {dualConfig.StartDate}, End: {dualConfig.EndDate}, "
            + $"Id: {((Chromosome)chromosome).Id}, Dual Period {this.Name}: {fitness.Value}");
            Program.Logger.Info(output);

            Program.Config.StartDate = start;
            Program.Config.EndDate = end;

            return fitness.Fitness;
        }
    }
}
