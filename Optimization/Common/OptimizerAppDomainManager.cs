using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

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
        private static string _callingDomainName;
        private static string _exeAssembly;

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
            _callingDomainName = Thread.GetDomain().FriendlyName;
            //Console.WriteLine(callingDomainName);

            // Get and display the full name of the EXE assembly.
            _exeAssembly = Assembly.GetEntryAssembly()?.FullName;
            //Console.WriteLine(exeAssembly);

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
        /// <returns>Backtest statisctics in way of a dictionary.</returns>
        public static Dictionary<string, decimal> RunAlgorithm(Dictionary<string, object> list)
        {
            var rc = CreateRunnerInAppDomain(out var ad);

            var result = rc.Run(list);

            /*
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
            */

            // unload an App Domain and assembly.
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



        //TODO: Resharper suggests this method has never been used ?? 
        /*
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
        /// Get the results dictionary.
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
