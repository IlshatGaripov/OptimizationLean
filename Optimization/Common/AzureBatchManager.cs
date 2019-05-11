using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Optimization
{
    /// <summary>
    /// Manages azure client/credentials/storage etc. to provide an easy access to Azure cloud computing facilities.
    /// </summary>
    public static class AzureBatchManager
    {
        // Timer
        private static Stopwatch _timer = new Stopwatch();

        // Output container
        public const string OutputContainerName = "output";

        // Pool and Job constants
        private const string PoolId = "RunnerOptimaPool";
        private const string PoolVmSize = "STANDARD_A1_v2";

        // Make JobId public as it will be accessed from other classes to queue the new tasks.
        public const string JobId = "RunnerOptimaJob";

        // Application package Id and version
        public const string AppPackageId = "Runner";
        public const string AppPackageVersion = "1";

        // Batch client and 
        public static BatchClient BatchClient;
        public static CloudBlobClient BlobClient;

        // Sas for output container where results of backtests (evaluations) will be stored
        public static string OutputContainerSasUrl;


        /// <summary>
        /// Deploy Batch resourses for cloud computing. Open a batch client.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task DeployAsync()
        {
            Console.WriteLine("Azure Batch resources deployment start: {0}", DateTime.Now);
            Console.WriteLine();
            _timer = Stopwatch.StartNew();

            // == BATCH CLIENT ==

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


            // == STORAGE ==

            // Construct the Storage account connection string
            string storageConnectionString = String.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1}",
                Program.Config.StorageAccountName, Program.Config.StorageAccountKey);

            // Retrieve the storage account
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

            // Create the blob client, for use in obtaining references to blob storage containers
            BlobClient = storageAccount.CreateCloudBlobClient();

            await CreateContainerIfNotExistAsync(BlobClient, OutputContainerName);

            // Obtain a shared access signature that provides write access to the output container to which
            // the tasks will upload their output.
            OutputContainerSasUrl = GetContainerSasUrl(BlobClient, OutputContainerName, SharedAccessBlobPermissions.Write);
        }

        /// <summary>
        /// Clean up Batch resources. Dispose a batch client.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task FinalizeAsync()
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
                    targetDedicatedComputeNodes: Program.Config.DedicatedNodeCount,
                    targetLowPriorityComputeNodes: Program.Config.LowPriorityNodeCount,
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

                // Commit in blocking fashion. To make sure pool has been created when Job
                // associated with a pool will be created.
                pool.Commit();
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

            try
            {
                await job.CommitAsync();
            }
            catch (BatchException be)
            {
                // Accept the specific error code PoolExists as that is expected if the pool already exists
                if (be.RequestInformation?.BatchError?.Code == BatchErrorCodeStrings.JobExists)
                {
                    Console.WriteLine("Job {0} already exists. I will delete it. Launch again in a minute", jobId);
                    await batchClient.JobOperations.DeleteJobAsync(JobId);

                }

                throw; // Any other exception is unexpected
            }
        }

        /// <summary>
        /// Creates a container with the specified name in Blob storage, unless a container with that name already exists.
        /// </summary>
        /// <param name="blobClient">A <see cref="CloudBlobClient"/>.</param>
        /// <param name="containerName">The name for the new container.</param>

        private static async Task CreateContainerIfNotExistAsync(CloudBlobClient blobClient, string containerName)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // delete first to clean up contained files and then create
            await container.CreateIfNotExistsAsync();

            Console.WriteLine("Creating container [{0}].", containerName);
        }

        /// <summary>
        /// Returns a shared access signature (SAS) URL providing the specified
        ///  permissions to the specified container. The SAS URL provided is valid for 2 hours from
        ///  the time this method is called. The container must already exist in Azure Storage.
        /// </summary>
        /// <param name="blobClient">A <see cref="CloudBlobClient"/>.</param>
        /// <param name="containerName">The name of the container for which a SAS URL will be obtained.</param>
        /// <param name="permissions">The permissions granted by the SAS URL.</param>
        /// <returns>A SAS URL providing the specified access to the container.</returns>
        private static string GetContainerSasUrl(CloudBlobClient blobClient, string containerName, SharedAccessBlobPermissions permissions)
        {
            // Set the expiry time and permissions for the container access signature. In this case, no start time is specified,
            // so the shared access signature becomes valid immediately. Expiration is in 2 hours.
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(2),
                Permissions = permissions
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the URL string for the container, including the SAS token
            return String.Format("{0}{1}", container.Uri, sasContainerToken);
        }
    }
}
