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

    /// <summary>
    /// Algorithm optimization mode to search the best parameters
    /// </summary>
    public enum OptimizationMode
    {
        GeneticAlgorithm,
        BruteForce
    }

    /// <summary>
    /// Computation mode
    /// </summary>
    public enum TaskExecutionMode
    {
        Linear,
        Parallel,
        Azure
    }

    /// <summary>
    /// Mode to evaluate the algorithm fitness
    /// </summary>
    public enum FitnessScore
    {
        SharpeRatio,
        TotalNetProfit
    }

}
