using System;
using System.Collections.Generic;
using System.Reflection;
using Optimization.RunnerLocal;

namespace Optimization
{
    /// <summary>
    /// Setup for running QCAlgorithm in a separate AppDomain.
    /// </summary>
    public static class OptimizerAppDomainManager
    {
        private static AppDomainSetup _ads;
        private static string _assembly;

        /// <summary>
        /// Startup method
        /// </summary>
        public static void Initialize()
        {
            _assembly = Assembly.GetAssembly(typeof(Runner)).FullName;

            _ads = SetupAppDomain();
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Release()
        {

        }

        /// <summary>
        /// Construct and initialize settings for a second AppDomain.
        /// </summary>
        private static AppDomainSetup SetupAppDomain()
        {
            var ads = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                DisallowBindingRedirects = false,
                DisallowCodeDownload = true,
                ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
            };

            return ads;
        }

        /// <summary>
        /// Runs an algorithm in separate app doman and returns the results.
        /// </summary>
        /// <param name="list">Input parameters.</param>
        /// <returns>Full backtest results of<see cref="OptimizerResultHandler"/></returns>
        public static Dictionary<string, decimal> RunAlgorithm(Dictionary<string, object> list)
        {
            // Create a new AppDomain ->
            var name = Guid.NewGuid().ToString("x");
            var ad = AppDomain.CreateDomain(name, null, _ads);

            // Create a proxy is new AppDomain ->
            var rc = (Runner)ad.CreateInstanceAndUnwrap(_assembly,
                typeof(Runner).FullName ?? throw new InvalidOperationException());

            // Obtain results -> 
            var result = rc.Run(list);

            // Unload ->
            AppDomain.Unload(ad);

            return result;
        }
    }

}
