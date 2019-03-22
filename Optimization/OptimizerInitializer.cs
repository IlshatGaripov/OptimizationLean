using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;
using System.IO.Abstractions;

namespace Optimization
{
    public class OptimizerInitializer
    {
        /// <summary>
        /// Provides properties and methods for working with drives, files, and directories.
        /// </summary>
        private readonly IFileSystem _file = new FileSystem();

        /// <summary>
        /// ..
        /// </summary>
        private IOptimizerManager _manager;

        /// <summary>
        /// Class the holds json values.
        /// </summary>
        private OptimizerConfiguration _config;
        
        /// <summary>
        /// Master method. Initialization for everything.
        /// </summary>
        public void Initialize(string[] args)
        {
            // load values from config to an object. 
            _config = LoadConfig(args);

            // takes config from Lean folder and makes a local copy in the same directory as an application.
            _file.File.Copy(_config.ConfigPath, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"), true);

            // path to an assembly containing an algorithm.
            var path = _config.AlgorithmLocation;
            
            if (!string.IsNullOrEmpty(path))
            {
                // makes a local copy of an algorithm assembly in an application directory.
                _file.File.Copy(path, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(path)), true);

                // path to a pdb file corresponding to an algorithm assembly.
                var pdb = path.Replace(System.IO.Path.GetExtension(path), ".pdb");
                
                // due to locking issues, need to manually clean to update pdb
                if (_file.File.Exists(pdb))
                {
                    _file.File.Copy(pdb, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(pdb)), true);
                }
            }

            // Init those few variables used when joggling with App Domain features used launching the lean runner.
            OptimizerAppDomainManager.Initialize();

            var fitness = (OptimizerFitness)Assembly.GetExecutingAssembly().CreateInstance(_config.FitnessTypeName, 
                false, BindingFlags.Default, null,
                new object[] { _config, new FitnessFilter() }, null, null);

            if (_manager == null)
            {
                if (new[] { typeof(SharpeMaximizer), typeof(NFoldCrossReturnMaximizer), typeof(NestedCrossSharpeMaximizer),
                    typeof(NestedCrossSharpeMaximizer) }.Contains(fitness?.GetType()))
                {
                    _manager = new MaximizerManager();
                }
                else
                {
                    _manager = new GeneManager();
                }
            }

            _manager.Initialize(_config, fitness);
            _manager.Start();
        }

        /// <summary>
        /// Loads values from JSON text file to a special class holding the config values.
        /// </summary>
        private OptimizerConfiguration LoadConfig(IReadOnlyList<string> args)
        {
            // default path
            var path = "optimization.json";

            // if specified.. then
            if (!string.IsNullOrEmpty(args[0]))
            {
                path = args[0];
            }

            // generic version of DeserializeObject transforms text from json to specific class that holds the corresponding values.
            return JsonConvert.DeserializeObject<OptimizerConfiguration>(_file.File.ReadAllText(path));
        }
    }
}
