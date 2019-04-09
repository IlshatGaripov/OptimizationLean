using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Optimization
{
    /// <summary>
    /// Manages execution of QCAlgorithm that is optimized in a separate AppDomain.
    /// </summary>
    public static class OptimizerAppDomainManager
    {
        private static AppDomainSetup _ads;
        private static Dictionary<string, Dictionary<string, decimal>> _results;
        private static object _resultsLocker;

        /// <summary>
        /// Startup method
        /// </summary>
        public static void Initialize()
        {
            _results = new Dictionary<string, Dictionary<string, decimal>>();
            _ads = SetupAppDomain();
            _resultsLocker = new object();
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


        //TODO: Resharper suggests this method has never been used ?? Can it be deleted?
        /*
        /// <summary>
        /// ..
        /// </summary>
        static Queuer CreateQueuerInAppDomain(out AppDomain ad)
        {
            // Create the second AppDomain.
            var name = Guid.NewGuid().ToString("x");
            ad = AppDomain.CreateDomain(name, null, _ads);

            // Create an instance of MarshalbyRefType in the second AppDomain. 
            // A proxy to the object is returned.
            Queuer rc = (Queuer)ad.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, typeof(Queuer).FullName);

            SetResults(ad, _results);

            return rc;
        }
        */

        /// <summary>
        /// Runs an algorithm in separate app doman and returns the results.
        /// </summary>
        /// <param name="list">Input parameters.</param>
        /// <returns>Backtest statisctics in way of a dictionary.</returns>
        public static Dictionary<string, decimal> RunAlgorithm(Dictionary<string, object> list)
        {
            var rc = CreateRunnerInAppDomain(out var ad);

            var result = rc.Run(list);

            lock (_resultsLocker)
            {
                foreach (var item in GetResults(ad))
                {
                    if (!_results.ContainsKey(item.Key))
                    {
                        _results.Add(item.Key, item.Value);
                    }
                }
            }

            // unload an App Domain and assembly.
            AppDomain.Unload(ad);

            return result;
        }

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
            var rc = (Runner)ad.CreateInstanceAndUnwrap(typeof(Runner).Assembly.FullName, "Runner");

            // set @Results@ property for App Domain.
            SetResults(ad, _results);

            return rc;
        }

        /// <summary>
        /// Can be used to "russian doll" QCAlgorithm
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Tuple<AppDomain, Task<Dictionary<string, decimal>>> RunAlgorithmAsync(Dictionary<string, object> list)
        {
            var runner = CreateRunnerInAppDomain(out EngineContext.AppDomain);

            var result = Task.Run(() => runner.Run(list));

            return Tuple.Create(EngineContext.AppDomain, result);
        }

        /// <summary>
        /// ??
        /// </summary>
        public static Dictionary<string, Dictionary<string, decimal>> GetResults()
        {
            return _results;
        }

        /// <summary>
        /// Get the app domain's Results property.
        /// </summary>
        public static Dictionary<string, Dictionary<string, decimal>> GetResults(AppDomain ad)
        {
            return GetData<Dictionary<string, Dictionary<string, decimal>>>(ad, "Results");
        }

        /// <summary>
        /// Gets the App Domains property corresponding to a key.
        /// In <see cref="CreateRunnerInAppDomain"/> we set "@Results@ property for App Domain.
        /// </summary>
        public static T GetData<T>(AppDomain ad, string key)
        {
            return (T)ad.GetData(key);
        }

        /// <summary>
        /// Sets value to App Domain's @Results@ property.
        /// </summary>
        public static void SetResults(AppDomain ad, object item)
        {
            ad.SetData("Results", item);
        }
    }

}
