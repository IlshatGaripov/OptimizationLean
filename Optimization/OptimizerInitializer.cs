using System;
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
        private readonly IFileSystem _file;

        /// <summary>
        /// ..
        /// </summary>
        private IOptimizerManager _manager;

        /// <summary>
        /// Class the holds json values.
        /// </summary>
        private OptimizerConfiguration _config;

        /// <summary>
        /// Constructor default.
        /// </summary>
        public OptimizerInitializer()
        {
            _file = new FileSystem();
        }

        /// <summary>
        /// Constructor with parameters.
        /// </summary>
        public OptimizerInitializer(IFileSystem file, IOptimizerManager manager)
        {
            _file = file;
            _manager = manager;
        }

        /// <summary>
        /// Master method. Initialization for everything.
        /// </summary>
        public void Initialize(string[] args)
        {
            // load values to config class
            _config = LoadConfig(args);

            // ?? copy from Lean source to exe destination
            _file.File.Copy(_config.ConfigPath, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"), true);

            // location of an assembly containing an algorithm
            var path = _config.AlgorithmLocation;

            // if specified
            if (!string.IsNullOrEmpty(path))
            {
                // copy assembly
                _file.File.Copy(path, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(path)), true);

                // the corresponding pdf file location
                var pdb = path.Replace(System.IO.Path.GetExtension(path), ".pdb");

                // due to locking issues, need to manually clean to update pdb
                if (!_file.File.Exists(pdb))
                {
                    _file.File.Copy(pdb, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(pdb)), true);
                }
            }

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
        /// Initializes a class that holds values we have in optimization config JSON. 
        /// </summary>
        private OptimizerConfiguration LoadConfig(string[] args)
        {
            var path = "optimization.json";
            if (!string.IsNullOrEmpty(args[0]))
            {
                path = args[0];
            }

            // generic version of DeserializeObject uploads text
            // from json to a class that holds fields we have in JSON
            return JsonConvert.DeserializeObject<OptimizerConfiguration>(_file.File.ReadAllText(path));
        }

    }

}
