using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using QuantConnect.Configuration;
using QuantConnect.Lean.Engine;
using QuantConnect.Logging;
using QuantConnect.Util;

namespace Optimization.RunnerAzureApp
{ 
    /// <summary>
    /// Class that runs QC algorithm on a node in Azure batch.
    /// </summary>
    public static class AzureRunner
    {
        /// <summary>
        /// Custom Lean's result handler
        /// </summary>
        public static OptimizerResultHandler ResultHandler;

        /// <summary>
        /// Method performs necessary initialization and starts and algorithm inside Lean Engine.
        /// </summary>
        public static Dictionary<string, decimal> Run(Dictionary<string, string> inputs)
        {
            // chromosome's GUID 
            var id = (inputs.ContainsKey("Id") ? inputs["Id"] : Guid.NewGuid().ToString("N")).ToString();
            
            // set the algorithm input variables. 
            foreach (var pair in inputs.Where(i => i.Key != "Id"))
            {
                Config.Set(pair.Key, pair.Value);
            }

            // general Lean settings:
            Config.Set("environment", "backtesting");
            Config.Set("algorithm-language", "CSharp");     // omitted?
            Config.Set("result-handler", nameof(OptimizerResultHandler));   //override default result handler
            Config.Set("data-folder", "Data/");

            // separate log uniquely named for each backtest
            var logFileName = "log" + DateTime.Now.ToString("yyyyMMddssfffffff") + "_" + id + ".txt";
            Log.LogHandler = new FileLogHandler(logFileName);

            // LeanEngineSystemHandlers
            LeanEngineSystemHandlers leanEngineSystemHandlers;
            try
            {
                leanEngineSystemHandlers = LeanEngineSystemHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            leanEngineSystemHandlers.Initialize();   // can this be omitted?

            var job = leanEngineSystemHandlers.JobQueue.NextJob(out var assemblyPath);

            if (job == null)
            {
                throw new Exception("Engine.Main(): Job was null.");
            }

            // LeanEngineSystemHandlers
            LeanEngineAlgorithmHandlers leanEngineAlgorithmHandlers;
            try
            {
                leanEngineAlgorithmHandlers = LeanEngineAlgorithmHandlers.FromConfiguration(Composer.Instance);
            }
            catch (CompositionException compositionException)
            {
                Log.Error("Engine.Main(): Failed to load library: " + compositionException);
                throw;
            }

            // Engine
            try
            {
                var liveMode = Config.GetBool("live-mode");
                var algorithmManager = new AlgorithmManager(liveMode);
                // can this be omitted?
                leanEngineSystemHandlers.LeanManager.Initialize(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, job, algorithmManager);
                var engine = new Engine(leanEngineSystemHandlers, leanEngineAlgorithmHandlers, liveMode);
                engine.Run(job, algorithmManager, assemblyPath);
            }
            finally
            {
                // do not Acknowledge Job, clean up resources
                Log.Trace("Engine.Main(): Packet removed from queue: " + job.AlgorithmId);
                leanEngineSystemHandlers.Dispose();
                leanEngineAlgorithmHandlers.Dispose();
                Log.LogHandler.Dispose();
            }

            // Copy logs
            CopyLogFileToTheStorage(inputs["storageAccountName"], inputs["storageAccountKey"], logFileName);

            // Results
            ResultHandler = (OptimizerResultHandler)leanEngineAlgorithmHandlers.Results;
            return ResultHandler.FullResults;
        }

        /// <summary>
        /// Copies Lean log file to storage container.
        /// </summary>
        /// <param name="storageAccountName">The name of the Storage Account</param>
        /// <param name="storageAccountKey">The key of the Storage Account</param>
        /// <param name="fileName">Name of the log file to upload to Storage.</param>
        /// <returns></returns>
        public static ResourceFile CopyLogFileToTheStorage(string storageAccountName, string storageAccountKey, string fileName)
        {
            // Construct the Storage account connection string
            string storageConnectionString =
                $"DefaultEndpointsProtocol=https;AccountName={storageAccountName};AccountKey={storageAccountKey}";

            // Retrieve the storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Use the blob client to create the input container in Azure Storage 
            const string containerName = "lean-log";

            Console.WriteLine("Uploading file {0} to container [{1}]...", fileName, containerName);

            string blobName = Path.GetFileName(fileName);
            var filePath = Path.Combine(Environment.CurrentDirectory, fileName);

            // Container
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            container.CreateIfNotExistsAsync().Wait();

            // Copy to the storage blob
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            blobData.UploadFromFileAsync(filePath).Wait();

            // Set the expiry time and permissions for the blob shared access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = SharedAccessBlobPermissions.Read
            };

            
            // Construct the SAS URL for blob
            string sasBlobToken = blobData.GetSharedAccessSignature(sasConstraints);
            string blobSasUri = $"{blobData.Uri}{sasBlobToken}";

            return new ResourceFile(blobSasUri, blobName);
        }
    }
}
