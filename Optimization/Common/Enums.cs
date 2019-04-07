namespace Optimization
{
    public enum OptimizerTypeOptions
    {
        Genetic = 0,
        RandomSearch = 1,
        GridSearch = 2,
        ParticleSwarm = 3,
        Bayesian = 4,
        GlobalizedBoundedNelderMead = 5
    }

    public enum OptimizationMode
    {
        GeneticAlgorithm,
        BruteForce
    }
}
