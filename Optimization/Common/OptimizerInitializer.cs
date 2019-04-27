using System;
using Newtonsoft.Json;
using System.IO.Abstractions;
using Newtonsoft.Json.Converters;

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
            // path to an assembly containing an algorithm.
            var path = Program.Config.AlgorithmLocation;

            if (string.IsNullOrEmpty(path)) return;

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

        /// <summary>
        /// Loads values from JSON text file to a special class holding the config values.
        /// </summary>
        public static OptimizerConfiguration LoadConfigFromFile(string path)
        {
            // DateTimeFormat for proper deserialize of start and end date string to DateTime
            var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = "yyyy-MM-dd" };

            // generic version of DeserializeObject transforms text from json to specific class that holds the corresponding values.
            return JsonConvert.DeserializeObject<OptimizerConfiguration>(File.File.ReadAllText(path), dateTimeConverter);
        }
    }
}
