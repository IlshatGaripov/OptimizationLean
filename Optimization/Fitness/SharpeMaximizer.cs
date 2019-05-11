using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeneticSharp.Domain.Chromosomes;
using SharpLearning.Optimization;
using System.Runtime.CompilerServices;

namespace Optimization
{

    public class SharpeMaximizer : OptimizerFitness
    {
        public virtual string ScoreKey { get; set; } = "SharpeRatio";
        public IChromosome Best { get; set; }
        private ConditionalWeakTable<OptimizerResult, string> _resultIndex;
        private const double ErrorFitness = 1.01;

        public SharpeMaximizer()
        {
            _resultIndex = new ConditionalWeakTable<OptimizerResult, string>();
            Name = "Sharpe";
        }

        public override double Evaluate(IChromosome chromosome)
        {
            try
            {
                var parameters = Program.Config.Genes.Select(s =>
                    new MinMaxParameterSpec(min: (double)(s.MinDecimal ?? s.MinInt.Value), max: (double)(s.MaxDecimal ?? s.MaxInt.Value),
                    transform: Transform.Linear, parameterType: s.Scale > 0 ? ParameterType.Continuous : ParameterType.Discrete)
                ).ToArray();


                IOptimizer optimizer = null;
                if (Program.Config.Fitness != null)
                {
                    if (Program.Config.Fitness.OptimizerTypeName == OptimizerTypeOptions.RandomSearch.ToString())
                    {
                        optimizer = new RandomSearchOptimizer(parameters, iterations: Program.Config.Generations, seed: 42, maxDegreeOfParallelism: Program.Config.MaxThreads);
                    }
                    else if (Program.Config.Fitness.OptimizerTypeName == OptimizerTypeOptions.ParticleSwarm.ToString())
                    {
                        optimizer = new ParticleSwarmOptimizer(parameters, maxIterations: Program.Config.Generations, numberOfParticles: Program.Config.PopulationSize,
                            seed: 42, maxDegreeOfParallelism: Program.Config.MaxThreads);
                    }
                    else if (Program.Config.Fitness.OptimizerTypeName == OptimizerTypeOptions.Bayesian.ToString())
                    {
                        /*
                        optimizer = new BayesianOptimizer(parameters, maxIterations: Program.Config.Generations, numberOfStartingPoints: Program.Config.PopulationSize, seed: 42);
                        */
                    }
                    else if (Program.Config.Fitness.OptimizerTypeName == OptimizerTypeOptions.GlobalizedBoundedNelderMead.ToString())
                    {
                        optimizer = new GlobalizedBoundedNelderMeadOptimizer(parameters, maxRestarts: Program.Config.Generations, 
                            maxIterationsPrRestart: Program.Config.PopulationSize, seed: 42, maxDegreeOfParallelism: Program.Config.MaxThreads);
                    }
                    else if (Program.Config.Fitness.OptimizerTypeName == OptimizerTypeOptions.Genetic.ToString())
                    {
                        throw new Exception("Genetic optimizer cannot be used with Sharpe Maximizer");
                    }
                }

                //todo:
                // GridSearchOptimizer?

                OptimizerResult Func(double[] p) => Minimize(p, (Chromosome) chromosome);

                // run optimizer
                var result = optimizer.OptimizeBest(Func);

                Best = ToChromosome(result, chromosome);

                return result.Error;
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex);
                return ErrorFitness;
            }
        }

        protected OptimizerResult Minimize(double[] p, Chromosome configChromosome)
        {
            var id = Guid.NewGuid().ToString("N");
            try
            {
                StringBuilder output = new StringBuilder();
                var list = configChromosome.ToDictionary();

                list.Add("Id", id);
                output.Append("Id: " + id + ", ");

                for (int i = 0; i < Program.Config.Genes.Count(); i++)
                {
                    var key = Program.Config.Genes.ElementAt(i).Key;
                    var precision = Program.Config.Genes.ElementAt(i).Scale ?? 0;
                    var value = Math.Round(p[i], precision);
                    list[key] = value;

                    output.Append(key + ": " + value.ToString() + ", ");
                }

                if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
                {
                    output.AppendFormat("Start: {0}, End: {1}, ", Program.Config.StartDate, Program.Config.EndDate);
                }

                var score = GetScore(list);
                var fitness = CalculateFitness(score);

                output.AppendFormat("{0}: {1}", Name, fitness.Value.ToString("0.##"));
                Program.Logger.Info(output);

                var result = new OptimizerResult(p, fitness.Fitness);
                _resultIndex.Add(result, id);
                return result;
            }
            catch (Exception)
            {
                Program.Logger.Error($"Id: {id}, Iteration failed.");

                var result = new OptimizerResult(p, ErrorFitness);
                _resultIndex.Add(result, id);
                return result;
            }
        }

        public virtual Dictionary<string, decimal> GetScore(Dictionary<string, object> list)
        {
            return RunAlgorithm(list);
        }

        public virtual Dictionary<string, decimal> RunAlgorithm(Dictionary<string, object> list)
        {
            return OptimizerAppDomainManager.RunAlgorithm(list);
        }

        private IChromosome ToChromosome(OptimizerResult result, IChromosome source)
        {
            var destination = (Chromosome)source;
            destination.Id = _resultIndex.GetValue(result, (k) => Guid.NewGuid().ToString("N") );

            var list = destination.ToDictionary();
            for (int i = 0; i < Program.Config.Genes.Count(); i++)
            {
                var pair = (KeyValuePair<string, object>)destination.GetGene(i).Value;
                destination.ReplaceGene(i, new Gene(new KeyValuePair<string, object>(pair.Key, result.ParameterSet[i])));
            }

            destination.Fitness = result.Error;
            return destination;
        }

        protected override FitnessResult CalculateFitness(Dictionary<string, decimal> result)
        {
            var ratio = result[ScoreKey];

            if (Filter != null && !Filter.IsSuccess(result, this))
            {
                ratio = ErrorRatio;                
            }

            return new FitnessResult
            {
                Value = ratio,
                Fitness = 1 - ((double)ratio / 1000)
            };
        }

        public override double GetAdjustedFitness(double? fitness)
        {
            return ((fitness ?? ErrorFitness) - 1) * 1000 * -1;
        }
    }
    
}
