using System;
using System.Collections.Generic;
using System.Reflection;

namespace Optimization
{
    /// <summary>
    /// Manages execution of QCAlgorithm that is optimized in a separate AppDomain.
    /// </summary>
    public static class OptimizerAppDomainManager
    {
        private static AppDomainSetup _ads;
        private static string _exeAssembly;

        /// <summary>
        /// Startup method
        /// </summary>
        public static void Initialize()
        {
            _ads = SetupAppDomain();
        }

        /// <summary>
        /// Construct and initialize settings for a second AppDomain.
        /// </summary>
        private static AppDomainSetup SetupAppDomain()
        {
            // Get and display the full name of the EXE assembly.
            _exeAssembly = Assembly.GetEntryAssembly()?.FullName;

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
            // Create runner in App Domain -> Get results -> Unload
            var rc = CreateRunnerInAppDomain(out var ad);
            var result = rc.Run(list);
            AppDomain.Unload(ad);

            return result;
        }

        // TODO: DO we need to create new appDomain every time we run an algorithm. Can AD be a common for all runners?
        /// <summary>
        /// Creates a lean algorithm runner in a new app domain
        /// </summary>
        /// <returns>a proxy to an object in newly created App Domain</returns>
        private static Runner CreateRunnerInAppDomain(out AppDomain ad)
        {
            // Create the second AppDomain.
            var name = Guid.NewGuid().ToString("x");
            ad = AppDomain.CreateDomain(name, null, _ads);

            // Create an instance of MarshalbyRefType in AppDomain. A proxy to the object is returned.
            var rc = (Runner)ad.CreateInstanceAndUnwrap(_exeAssembly, 
                    typeof(Runner).FullName ?? throw new InvalidOperationException());

            // create a clone of global config file and pass it to app domain as property
            var cloneConfig = Exstensions.Clone(Program.Config);
            ad.SetData("Configuration", cloneConfig);

            return rc;
        }
    }

}
