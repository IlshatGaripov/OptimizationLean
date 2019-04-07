using System;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace Optimization
{
    /// <summary>
    /// Initializes the config object and loads an assembly containing QCAlgorithm to the local folder.
    /// </summary>
    public static class OptimizerInitializer
    {
        /// <summary>
        /// Provides properties and methods for working with drives, files, and directories.
        /// </summary>
        private static readonly IFileSystem File = new FileSystem();
        
        /// <summary>
        /// Master method. Initialization for everything.
        /// </summary>
        public static void Initialize()
        {
            // takes config from Lean folder and makes a local copy in the same directory as an application.
            File.File.Copy(Program.Config.ConfigPath, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"), true);

            // path to an assembly containing an algorithm.
            var path = Program.Config.AlgorithmLocation;
            
            if (!string.IsNullOrEmpty(path))
            {
                // makes a local copy of an algorithm assembly in an application directory.
                File.File.Copy(path, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(path)), true);

                // path to a pdb file corresponding to an algorithm assembly.
                var pdb = path.Replace(System.IO.Path.GetExtension(path), ".pdb");
                
                // due to locking issues, need to manually clean to update pdb
                if (File.File.Exists(pdb))
                {
                    File.File.Copy(pdb, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, System.IO.Path.GetFileName(pdb)), true);
                }
            }
        }

        /// <summary>
        /// Loads values from JSON text file to a special class holding the config values.
        /// </summary>
        public static OptimizerConfiguration LoadConfigFromFile()
        {
            // default path
            const string path = "optimization.json";

            // generic version of DeserializeObject transforms text from json to specific class that holds the corresponding values.
            return JsonConvert.DeserializeObject<OptimizerConfiguration>(File.File.ReadAllText(path));
        }
    }
}
