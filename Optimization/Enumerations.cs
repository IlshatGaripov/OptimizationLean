namespace Optimization
{
    /// <summary>
    /// Mode to evaluate the algorithm fitness
    /// </summary>
    public enum FitnessScore
    {
        SharpeRatio = 1,
        TotalNetProfit = 2
    }

    /// <summary>
    /// Algorithm optimization mode to search the best parameters
    /// </summary>
    public enum OptimizationMode
    {
        Genetic,
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
}
