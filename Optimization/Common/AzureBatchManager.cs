using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;

namespace Optimization
{
    /// <summary>
    /// Manages azure client/credentials/storage etc. to provide an easy access to Azure cloud computing facilities.
    /// </summary>
    public static class AzureBatchManager
    {
        // Timer
        private static Stopwatch _timer = new Stopwatch();

        // Pool and Job constants
        private const string PoolId = "RunnerOptimaPool";
        private const int DedicatedNodeCount = 0;
        private const int LowPriorityNodeCount = 2;
        private const string PoolVmSize = "STANDARD_A1_v2";

        // Make JobId public as it will be accessed from other classes to queue the new tasks.
        public const string JobId = "RunnerOptimaJob";

        // Application package Id and version
        private const string AppPackageId = "Runner";
        private const string AppPackageVersion = "1";

        // Batch client
        public static BatchClient BatchClient;


        /// <summary>
        /// Deploy Batch resourses for cloud computing. Open a batch client.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        private static async Task DeployAsync()
        {
            Console.WriteLine("Azure Batch resources deployment start: {0}", DateTime.Now);
            Console.WriteLine();
            _timer = Stopwatch.StartNew();

            var batchAccountUrl = Program.Config.BatchAccountUrl;
            var batchAccountName = Program.Config.BatchAccountName;
            var batchAccountKey = Program.Config.BatchAccountKey;

            // Create a Batch client and authenticate with shared key credentials.
            // The Batch client allows the app to interact with the Batch service.
            BatchSharedKeyCredentials sharedKeyCredentials = new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);

            BatchClient = BatchClient.Open(sharedKeyCredentials);

            // Create the Batch pool, if not exist, which contains the compute nodes that execute the tasks.
            await CreatePoolIfNotExistAsync(BatchClient, PoolId);

            // Create the job that runs the tasks.
            await CreateJobAsync(BatchClient, JobId, PoolId);
        }

        /// <summary>
        /// Clean up Batch resources. Dispose a batch client.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        private static async Task DisposeAsync()
        {
            // Print out timing info
            _timer.Stop();
            Console.WriteLine();
            Console.WriteLine("Sample end: {0}", DateTime.Now);
            Console.WriteLine("Elapsed time: {0}", _timer.Elapsed);

            // Clean up Batch resources (if the user so chooses)
            Console.WriteLine();
            Console.Write("Delete job? [yes] no: ");
            string response = Console.ReadLine()?.ToLower();
            if (response != "n" && response != "no")
            {
                await BatchClient.JobOperations.DeleteJobAsync(JobId);
            }

            Console.Write("Delete pool? [yes] no: ");
            response = Console.ReadLine()?.ToLower();
            if (response != "n" && response != "no")
            {
                await BatchClient.PoolOperations.DeletePoolAsync(PoolId);
            }

            // Dispose a batch client.
            BatchClient?.Dispose();

        }

        /// <summary>
        /// Creates the Batch pool.
        /// </summary>
        /// <param name="batchClient">A BatchClient object</param>
        /// <param name="poolId">ID of the CloudPool object to create.</param>
        private static async Task CreatePoolIfNotExistAsync(BatchClient batchClient, string poolId)
        {
            CloudPool pool = null;
            try
            {
                Console.WriteLine("Creating pool [{0}]...", poolId);

                ImageReference imageReference = new ImageReference(
                        publisher: "MicrosoftWindowsServer",
                        offer: "WindowsServer",
                        sku: "2012-R2-Datacenter-smalldisk",
                        version: "latest");

                VirtualMachineConfiguration virtualMachineConfiguration =
                new VirtualMachineConfiguration(
                    imageReference: imageReference,
                    nodeAgentSkuId: "batch.node.windows amd64");

                // Create an unbound pool. No pool is actually created in the Batch service until we call
                // CloudPool.Commit(). This CloudPool instance is therefore considered "unbound," and we can
                // modify its properties.
                pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: DedicatedNodeCount,
                    targetLowPriorityComputeNodes: LowPriorityNodeCount,
                    virtualMachineSize: PoolVmSize,
                    virtualMachineConfiguration: virtualMachineConfiguration);

                // Specify the application and version to install on the compute nodes
                // This assumes that a Windows 64-bit zipfile of ffmpeg has been added to Batch account
                // with Application Id of "ffmpeg" and Version of "3.4".
                // Download the zipfile https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-3.4-win64-static.zip
                // to upload as application package
                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = AppPackageId,
                        Version = AppPackageVersion
                    }
                };

                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Accept the specific error code PoolExists as that is expected if the pool already exists
                if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", poolId);
                }
                else
                {
                    throw; // Any other exception is unexpected
                }
            }
        }

        /// <summary>
        /// Creates a job in the specified pool.
        /// </summary>
        /// <param name="batchClient">A BatchClient object.</param>
        /// <param name="jobId">ID of the job to create.</param>
        /// <param name="poolId">ID of the CloudPool object in which to create the job.</param>
        private static async Task CreateJobAsync(BatchClient batchClient, string jobId, string poolId)
        {

            Console.WriteLine("Creating job [{0}]...", jobId);

            CloudJob job = batchClient.JobOperations.CreateJob();
            job.Id = jobId;
            job.PoolInformation = new PoolInformation { PoolId = poolId };

            await job.CommitAsync();
        }
    }
}
