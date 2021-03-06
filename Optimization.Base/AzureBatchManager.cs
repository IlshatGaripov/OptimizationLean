﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;

namespace Optimization.Base
{
    /// <summary>
    /// Manages azure client/credentials/storage etc. to provide an easy access to Azure cloud computing facilities.
    /// </summary>
    public static class AzureBatchManager
    {
        // Timer
        private static Stopwatch _timer = new Stopwatch();

        // Blob and File container names
        public const string OutputContainerName = "output";
        public const string DllContainerName = "dll";
        public const string DataFileShareName = "data";
        public const string DataNetDrive = "R:";

        // Pool and Job constants
        private const string PoolId = "RunnerOptimaPool";
        private const string PoolVmSize = "STANDARD_A1_v2";

        // Make JobId public as it will be accessed from other classes to queue the new tasks.
        public const string JobId = "RunnerOptimaJob";

        // Application package Id and version
        public const string AppPackageId = "Runner";
        public const string AppPackageVersion = "1";

        // Clents: Batch, Blob, File
        public static BatchClient BatchClient;
        public static CloudBlobClient BlobClient;
        public static CloudFileClient FileClient;

        // Sas for output container where results of backtests (evaluations) will be stored
        public static string OutputContainerSasUrl;

        // File Share
        public static CloudFileShare DataFileShare;


        /// <summary>
        /// Deploy Batch resourses for cloud computing. Open a batch client.
        /// </summary>
        public static async Task InitializeAsync()
        {
            try
            {
                Console.WriteLine("Optimization / Azure Start: {0}", DateTime.Now);
                Console.WriteLine();
                _timer = Stopwatch.StartNew();

                // This will boost parallel submission speed for REST APIs. If your use requires many simultaneous service calls set this number to something large, such as 100.
                // See: https://msdn.microsoft.com/en-us/library/system.net.servicepointmanager.defaultconnectionlimit.aspx for more info.
                System.Net.ServicePointManager.DefaultConnectionLimit = 100;

                // Create a Batch client and authenticate with shared key credentials.
                // The Batch client allows the app to interact with the Batch service.
                var batchAccountUrl = Shared.Config.BatchAccountUrl;
                var batchAccountName = Shared.Config.BatchAccountName;
                var batchAccountKey = Shared.Config.BatchAccountKey;
                BatchSharedKeyCredentials sharedKeyCredentials = new BatchSharedKeyCredentials(batchAccountUrl, batchAccountName, batchAccountKey);
                BatchClient = BatchClient.Open(sharedKeyCredentials);

                // Construct the Storage account connection string
                string storageConnectionString =
                    $"DefaultEndpointsProtocol=https;AccountName={Shared.Config.StorageAccountName};AccountKey={Shared.Config.StorageAccountKey}";

                // Retrieve the storage account object
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);

                // Create the blob client, to reference the blob storage containers
                BlobClient = storageAccount.CreateCloudBlobClient();

                // Create File Client, to access Azure Files
                FileClient = storageAccount.CreateCloudFileClient();

                // Create share if not exist
                DataFileShare = FileClient.GetShareReference(DataFileShareName);
                await DataFileShare.CreateIfNotExistsAsync();
                
                // Synchronize data between data folder and Cloud File Share
                Console.Write("Synchronize data with file share? [yes] no: ");
                string response = Console.ReadLine()?.ToLower();
                if (response != "n" && response != "no")
                {
                    await SynchronizeHistoricalDataWithFileShareAsync(FileClient);
                }

                // Creat Containers: OutPut Container (where tasks will upload results) and Container for Algorithm DLL
                // Obtain a shared access signature that provides write access to the output
                await CreateContainerIfNotExistAsync(BlobClient, DllContainerName);
                await CreateContainerIfNotExistAsync(BlobClient, OutputContainerName);
                OutputContainerSasUrl = GetContainerSasUrl(BlobClient, OutputContainerName, SharedAccessBlobPermissions.Write);

                // Create the Batch pool, if not exist, which contains the compute nodes that execute the tasks.
                await CreatePoolIfNotExistAsync(BatchClient, PoolId);

                // Create the job that runs the tasks.
                await CreateJobAsync(BatchClient, JobId, PoolId);
            }
            // Catch for an aggregate exception
            catch (AggregateException ae)
            {
                // Flatten agregates all inner exception in one
                foreach (var innerException in ae.Flatten().InnerExceptions)
                {
                    Shared.Logger.Error($"AzureBatchManager.DeployAsync(): {innerException.Message}");
                }
                throw;
            }
        }

        /// <summary>
        /// Clean up Batch resources. Dispose a batch client.
        /// </summary>
        /// <returns>A <see cref="System.Threading.Tasks.Task"/> object that represents the asynchronous operation.</returns>
        public static async Task DisposeAsync()
        {
            // Print out timing info
            _timer.Stop();
            Console.WriteLine();
            Console.WriteLine("Optimization / Azure End: {0}", DateTime.Now);
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

            // Delete containers
            await DeleteContainerIfExistAsync(BlobClient, OutputContainerName);
            await DeleteContainerIfExistAsync(BlobClient, DllContainerName);
        }

        /// <summary>
        /// Creates the Batch pool.
        /// </summary>
        /// <param name="batchClient">A BatchClient object</param>
        /// <param name="poolId">ID of the CloudPool object to create.</param>
        private static async Task CreatePoolIfNotExistAsync(BatchClient batchClient, string poolId)
        {
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
                var pool = batchClient.PoolOperations.CreatePool(
                    poolId: poolId,
                    targetDedicatedComputeNodes: Shared.Config.DedicatedNodeCount,
                    targetLowPriorityComputeNodes: Shared.Config.LowPriorityNodeCount,
                    virtualMachineSize: PoolVmSize,
                    virtualMachineConfiguration: virtualMachineConfiguration);

                // Specify the application and version to install on the compute nodes
                pool.ApplicationPackageReferences = new List<ApplicationPackageReference>
                {
                    new ApplicationPackageReference
                    {
                        ApplicationId = AppPackageId,
                        Version = AppPackageVersion
                    }
                };
                
                // Start task to store credentials to mount file share
                string startTaskCommandLine =
                    $"cmd /c \"cmdkey /add:{DataFileShare.Uri.Host} /user:AZURE\\{Shared.Config.StorageAccountName} /pass:{Shared.Config.StorageAccountKey}\"";

                var dllReference =
                    await UploadResourceFileToContainerAsync(BlobClient, DllContainerName, Shared.Config.AlgorithmLocation);

                // This data will be stored on every node
                List<ResourceFile> inputFiles = new List<ResourceFile> { dllReference };

                pool.StartTask = new StartTask
                {
                    CommandLine = startTaskCommandLine,
                    UserIdentity = new UserIdentity(new AutoUserSpecification(
                        elevationLevel: ElevationLevel.NonAdmin,
                        scope: AutoUserScope.Pool)),
                    ResourceFiles = inputFiles,
                    WaitForSuccess = true
                };

                // Commit async
                await pool.CommitAsync();
            }
            catch (BatchException be)
            {
                // Accept the specific error code PoolExists as that is expected if the pool already exists
                if (be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.PoolExists)
                {
                    Console.WriteLine("The pool {0} already existed when we tried to create it", poolId);
                    Console.Write("Reboot the existing pool nodes? [yes] no: ");
                    string response = Console.ReadLine()?.ToLower();
                    if (response != "n" && response != "no")
                    {
                        foreach (var node in BatchClient.PoolOperations.ListComputeNodes(poolId))
                        {
                            Console.WriteLine("Reboot the node number {0}", node.Id);
                            await node.RebootAsync();
                        }
                    }
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

            // Commit the Job
            try
            {
                await job.CommitAsync();
            }
            catch (BatchException be)
            {
                // Catch specific error code JobExists as that is expected if the job already exists
                if (be.RequestInformation.BatchError.Code == BatchErrorCodeStrings.JobExists)
                {
                    Console.WriteLine("Job [{0}] already exists.", jobId);
                    Console.WriteLine("Deleting job [{0}]...", jobId);
                    await batchClient.JobOperations.DeleteJobAsync(JobId);

                    // try creating a job again
                    bool tryAgain = true;
                    while (tryAgain)
                    {
                        try
                        {
                            tryAgain = false;
                            var n = 10;
                            Thread.Sleep(n * 1000);
                            Console.WriteLine($"Creating job [{jobId}] after {n} sec.");
                            await job.CommitAsync();
                        }
                        catch (BatchException innerException)
                        {
                            if (innerException.RequestInformation.BatchError.Code == BatchErrorCodeStrings.JobBeingDeleted)
                            {
                                Console.WriteLine("Job is being deleted.. Try again..");
                                tryAgain = true;
                            }
                        }
                    }
                }
                else
                {
                    // Any other exception is unexpected
                    Shared.Logger.Error($"CreateJobAsync(): BatchException: Code - {be.RequestInformation.BatchError.Code}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Creates a container with the specified name in Blob storage, unless a container with that name already exists.
        /// </summary>
        /// <param name="blobClient">A <see cref="CloudBlobClient"/>.</param>
        /// <param name="containerName">The name for the new container.</param>
        private static async Task CreateContainerIfNotExistAsync(CloudBlobClient blobClient, string containerName)
        {
            Console.WriteLine("Creating container [{0}].", containerName);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // delete first to clean up contained files and then create
            await container.CreateIfNotExistsAsync();
        }

        /// <summary>
        /// Deletes a container with the specified name in Blob storage, if a container with that name exists.
        /// </summary>
        /// <param name="blobClient">A <see cref="CloudBlobClient"/>.</param>
        /// <param name="containerName">The name for the new container.</param>
        private static async Task DeleteContainerIfExistAsync(CloudBlobClient blobClient, string containerName)
        {
            Console.WriteLine("Deleting container [{0}].", containerName);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);

            // delete first to clean up contained files and then create
            await container.DeleteIfExistsAsync();
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
            // so the shared access signature becomes valid immediately. 
            SharedAccessBlobPolicy sasConstraints = new SharedAccessBlobPolicy
            {
                // Expiration is in 12 hours.
                SharedAccessExpiryTime = DateTime.UtcNow.AddHours(12),
                Permissions = permissions
            };

            // Generate the shared access signature on the container, setting the constraints directly on the signature
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            string sasContainerToken = container.GetSharedAccessSignature(sasConstraints);

            // Return the URL string for the container, including the SAS token
            return $"{container.Uri}{sasContainerToken}";
        }

        /// <summary>
        /// Uploads the specified file to the specified blob container.
        /// </summary>
        /// <param name="blobClient">Blob client<see cref="CloudBlobClient"/>.</param>
        /// <param name="containerName">The name of the blob storage container to which the file should be uploaded.</param>
        /// <param name="filePath">The full path to the file to upload to Storage.</param>
        /// <returns>A ResourceFile object representing the file in blob storage.</returns>
        private static async Task<ResourceFile> UploadResourceFileToContainerAsync(CloudBlobClient blobClient, string containerName, string filePath)
        {
            Console.WriteLine("Uploading file {0} to container [{1}]...", filePath, containerName);
            Console.WriteLine();

            string blobName = Path.GetFileName(filePath);

            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);
            await blobData.UploadFromFileAsync(filePath);

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

            return ResourceFile.FromUrl(blobSasUri, blobName);
        }

        /// <summary>
        /// Method uploads an up to date data required for experiment - bars/ticks - from Lean data folder to Azure File Share
        /// </summary>
        /// <param name="fileClient">Azure File client</param>
        /// <remarks>Uses Shared.Logger for tracing the logging outputs</remarks>
        private static async Task SynchronizeHistoricalDataWithFileShareAsync(CloudFileClient fileClient)
        {
            // Start timer to check how long it takes to upload all the zip files
            Stopwatch uploadTimer = Stopwatch.StartNew();
            Shared.Logger.Trace($"Synchronize Data <-> FileShare. time: {DateTime.Now}");

            // list all cloud share file
            var rootDirectory = DataFileShare.GetRootDirectoryReference();
            var cloudFileShareFiles = new List<string>();

            // execute a method in recursive way to retrieve all inner files
            ListCloudFileShareFiles(rootDirectory, ref cloudFileShareFiles);

            // list local data folder files
            var dataFolder = Shared.Config.DataFolder;
            var dataFolderFilesFullPath = Directory.GetFiles(Shared.Config.DataFolder, "*", SearchOption.AllDirectories).ToList();

            // little preparation for to compare the list with the cloud files 
            var dataFolderFilesComparativePath =
                dataFolderFilesFullPath.Select(i => i.Replace(dataFolder, "").Replace("\\", "/").ToLower()).ToList();

            // find difference between two folders, these files need to be uploaded to cloud share to appropriate folders
            var fileDifference = dataFolderFilesComparativePath.Except(cloudFileShareFiles.Select(i => i.Replace("/data", "")));

            // We will be copying the files straight away - as if the folder they need to be placed to exist -
            // if folder does not exist an exception will be thrown - we are going to catch it and then create a missing folder.
            // To check whether a folder exists for every file to copy can be very time consuming as folder.Exists() takes time.
            // we can not afford to check folder existance at every iteration, most probably folder does already exist.
            foreach (var file in fileDifference)
            {
                var fileName = file.Split('/').Last();
                var uriAddLine = file.Replace($"/{fileName}", "");    // file - name = path

                // see https://docs.microsoft.com/ru-ru/dotnet/api/microsoft.azure.storage.file.cloudfiledirectory?view=azure-dotnet
                // :
                var cloudDirectory = new CloudFileDirectory(new Uri(DataFileShare.Uri + uriAddLine), FileClient.Credentials);

                try
                {
                    var cloudFileReference = cloudDirectory.GetFileReference(fileName);    // Cloud File variable
                    await cloudFileReference.UploadFromFileAsync(Shared.Config.DataFolder + file);
                    Shared.Logger.Trace($"File uploaded: {file}");
                }
                catch (StorageException se)
                {
                    if (se.RequestInformation.ErrorCode == "ParentNotFound")
                    {
                        try
                        {
                            Shared.Logger.Error($"\"ParentNotFound\" exception trying to upload: {file}. Needs to create the missing folders.");
                            // See what folders in a branch are missing and create
                            var folderTree = new List<CloudFileDirectory> { cloudDirectory };
                            var parent = cloudDirectory.Parent;

                            while (!parent.Exists())
                            {
                                folderTree.Add(parent);
                                parent = parent.Parent;
                            }

                            // Now create folder in reverse order
                            folderTree.Reverse();

                            foreach (var folder in folderTree)
                            {
                                await folder.CreateIfNotExistsAsync();
                                Shared.Logger.Trace($"Created Folder: {folder.Uri.LocalPath}");
                            }

                            // Finally copy a file
                            var cloudFileReference = cloudDirectory.GetFileReference(fileName);    // Cloud File variable
                            await cloudFileReference.UploadFromFileAsync(Shared.Config.DataFolder + file);
                            Shared.Logger.Trace($"File uploaded : {file}");
                        }
                        catch (Exception e)
                        {
                            Shared.Logger.Error(e.Message);
                            throw;
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            
            // Print out timing info
            uploadTimer.Stop();
            Shared.Logger.Trace($"Synchronization <-> Compelete. time: {DateTime.Now}");
            Shared.Logger.Trace($"Operation took time: {uploadTimer.Elapsed}" + Environment.NewLine);
        }

        /// <summary>
        /// Lists all files that are located inside a given cloud file directory.
        /// Searches in base folder and subfolders.
        /// </summary>
        /// <param name="fileDirectory">Directory to search inside</param>
        /// <param name="outputList">List that will maintain a local path of all files contained in <see cref="fileDirectory"/></param>
        public static void ListCloudFileShareFiles(CloudFileDirectory fileDirectory, ref List<string> outputList)
        {
            var fileList = fileDirectory.ListFilesAndDirectories();

            // Iterate over all files/directories in the folder
            foreach (var listItem in fileList)
            {
                // listItem can be of CloudFileDirectory
                if (listItem.GetType() == typeof(CloudFileDirectory))
                {
                    ListCloudFileShareFiles((CloudFileDirectory)listItem, ref outputList);
                }
                // or CloudFile type
                if (listItem.GetType() == typeof(CloudFile))
                {
                    outputList.Add(listItem.Uri.LocalPath);
                }
            }
        }

    }
}
