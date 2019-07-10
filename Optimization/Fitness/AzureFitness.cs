using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp.Domain.Chromosomes;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Optimization
{
    /// <summary>
    /// Fitness function that calculates evaluation in Azure Cloud.
    /// </summary>
    public class AzureFitness: LeanFitness
    {
        private static readonly object Obj = new object();

        /// <summary>
        /// Constructor
        /// </summary>
        public AzureFitness(DateTime start, DateTime end, FitnessScore fitScore, bool enableFilter) : 
            base(start, end, fitScore, enableFilter)
        { }

        /// <summary>
        /// Performs the evaluation against the specified chromosome.
        /// </summary>
        /// <param name="chromosome">The chromosome to be evaluated.</param>
        /// <returns>The fitness of the chromosome.</returns>
        public override double Evaluate(IChromosome chromosome)
        {
            try
            {
                // All functionality is wrapped in async method. Execute it to obtain a result.
                return EvaluateAsync(chromosome).GetAwaiter().GetResult();
            }
            // Catch storage exception ->
            catch (StorageException ex)
            {
                var requestInformation = ex.RequestInformation;
                Program.Logger.Error(requestInformation.HttpStatusMessage);

                // get more details about the exception 
                var information = requestInformation.ExtendedErrorInformation;

                // if you have aditional information, you can use it for your logs
                if (information == null)
                    throw;

                var errorCode = information.ErrorCode;

                var message = $"({errorCode}) {information.ErrorMessage}";

                var details = information
                    .AdditionalDetails
                    .Aggregate("", (s, pair) => s + $"{pair.Key}={pair.Value},");

                Program.Logger.Error(message + " details " + details);
                throw;
            }
        }

        /// <summary>
        /// Async evalutation.
        /// </summary>
        private async Task<double> EvaluateAsync(IChromosome chromosome)
        {
            // Cast to base chromosome type
            var chromosomeBase = (Chromosome)chromosome;

            // Application Package Directory 
            string appPath =
                $"%AZ_BATCH_APP_PACKAGE_{AzureBatchManager.AppPackageId}#{AzureBatchManager.AppPackageVersion}%";

            // Unique identifier of a chromosome
            var id = chromosomeBase.Id;

            // Algorithm input parameters
            var algorithmInputs = chromosomeBase.ToKeyValueString();

            // Write to the log information before an experiment ->
            lock (Obj)
            {
                Program.Logger.Trace($">> {algorithmInputs}");
            }

            // -- 1 -- Create an argument string of gene key-values
            var runnerInputArguments = algorithmInputs + " ";

            // -- 2 -- Add an algorithm name to that string
            runnerInputArguments += $"algorithm-type-name {Program.Config.AlgorithmTypeName} ";

            // -- 3 -- Add algorithm dll and data folder locations
            var dllFileName = Path.GetFileName(Program.Config.AlgorithmLocation);
            runnerInputArguments += $"algorithm-location %AZ_BATCH_JOB_PREP_WORKING_DIR%\\{dllFileName} ";
            runnerInputArguments += "data-folder %AZ_BATCH_NODE_SHARED_DIR%\\Data ";

            // -- 4 -- Algorithm start and end dates
            runnerInputArguments += $"start-date {StartDate:O} ";
            runnerInputArguments += $"end-date {EndDate:O} ";

            // -- 5 -- File name where the final result of a backtest will be stored at.
            var resultsOutputFile = $"output_{id}.json";
            runnerInputArguments += $"results-output {resultsOutputFile} ";

            // -- 6 -- File name for Lean logs
            var leanLogFile = $"log_{id}.txt";
            runnerInputArguments += $"log-file {leanLogFile}";

            // Now using all arguments construct the resulting command line string
            string taskCommandLine = $"cmd /c {appPath}\\Debug\\Optimization.RunnerAppAzure.exe {runnerInputArguments}";

            // Create task id. Create a cloud task. 
            var taskId = $"task_{id}";
            CloudTask cloudTask = new CloudTask(taskId, taskCommandLine);

            // After app package finishes work and exits the Files:
            // Task output file and lean log will be automatically uploaded to the output blob container in Azure Storage.
            List<OutputFile> outputFileList = new List<OutputFile>();

            // #1 
            OutputFile outputFileA = new OutputFile(resultsOutputFile,
                new OutputFileDestination(
                    new OutputFileBlobContainerDestination(AzureBatchManager.OutputContainerSasUrl,
                        path: @"results\" + resultsOutputFile)),
                new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));

            // #2
            OutputFile outputFileB = new OutputFile(leanLogFile,
                new OutputFileDestination(
                    new OutputFileBlobContainerDestination(AzureBatchManager.OutputContainerSasUrl,
                        path: @"logs\" + leanLogFile)),
                new OutputFileUploadOptions(OutputFileUploadCondition.TaskSuccess));

            // Add Output Files to the list
            outputFileList.Add(outputFileA);
            outputFileList.Add(outputFileB);

            // Assign the list to cloud task's OutputTask property
            cloudTask.OutputFiles = outputFileList;

            // Create a collection to hold the tasks added to the job and add our task to that colleation.
            var cloudTaskCollection = new List<CloudTask>
            {
                cloudTask
            };

            // Azure Cloud objects
            var batchClient = AzureBatchManager.BatchClient;
            var blobClient = AzureBatchManager.BlobClient;
            var jobId = AzureBatchManager.JobId;

            // Call BatchClient.JobOperations.AddTask() to add the tasks as a collection to a queue
            await batchClient.JobOperations.AddTaskAsync(jobId, cloudTaskCollection);

            // Monitor for a task to complete. Timeout is set to 30 minutes. 
            await MonitorSpecificTaskToCompleteAsync(batchClient, jobId, taskId, TimeSpan.FromMinutes(30));

            // Obtain results dictionary ->
            var result = await ObtainResultFromTheBlob(blobClient, AzureBatchManager.OutputContainerName, 
                @"results\" + resultsOutputFile);

            // Calculate fitness ->
            var fitness = StatisticsAdapter.CalculateFitness(result, FitnessScore, FilterEnabled);

            // Save full results ->
            chromosomeBase.FitnessResult = new FitnessResult
            {
                Chromosome = chromosomeBase,
                StartDate = this.StartDate,
                EndDate = this.EndDate,
                FullResults = result
            };


            // Display results to the log ->
            lock (Obj)
            {
                Program.Logger.Trace($"({fitness}) {algorithmInputs}");
            }

            return fitness;
        }

        /// <summary>
        /// Monitors the specified task for completion and whether errors occurred.
        /// </summary>
        /// <param name="batchClient">A BatchClient object.</param>
        /// <param name="jobId">ID of the job containing the task to be monitored.</param>
        /// <param name="taskId">ID of the task to be monitored.</param>
        /// <param name="timeout">The period of time to wait for the tasks to reach the completed state.</param>
        private static async Task MonitorSpecificTaskToCompleteAsync(BatchClient batchClient, string jobId, string taskId, TimeSpan timeout)
        {
            // List the task which we track
            ODATADetailLevel detail = new ODATADetailLevel(selectClause: "id", filterClause: $"id eq '{taskId}'");
            List<CloudTask> monitoredCloudTasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

            // Task Monitor will be watching a single task
            TaskStateMonitor monitor = batchClient.Utilities.CreateTaskStateMonitor();

            try
            {
                // Waiting for the task to get to state Completed
                await monitor.WhenAll(monitoredCloudTasks, TaskState.Completed, timeout);
            }
            catch (Exception e)
            {
                Program.Logger.Error(e.Message);
                throw;
            }

            // All tasks have reached the "Completed" state, however, this does not guarantee all tasks completed successfully.
            // Here we further check for any tasks with an execution result of "Failure".

            // Update the detail level to populate only the executionInfo property.
            detail.SelectClause = "executionInfo";
            // Filter for tasks with 'Failure' result.
            detail.FilterClause = "executionInfo/result eq 'Failure'";

            List<CloudTask> failedTasks = await batchClient.JobOperations.ListTasks(jobId, detail).ToListAsync();

            if (failedTasks.Any())
            {
                Program.Logger.Error($"{taskId} failed.");
            }
        }


        /// <summary>
        /// Obtains final statistics from an output blob and calculate the chromosome fitness
        /// </summary>
        /// <param name="blobClient">A CloudBlobClient object.</param>
        /// <param name="containerName">Name of container.</param>
        /// <param name="blobName">Name of a file, blob.</param>
        private static async Task<Dictionary<string,decimal>> ObtainResultFromTheBlob(CloudBlobClient blobClient, string containerName, string blobName)
        {
            // Container
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            CloudBlockBlob blobData = container.GetBlockBlobReference(blobName);

            // Obtain the result from an output file
            using (var memoryStream = new MemoryStream())
            using (var streamReader = new StreamReader(memoryStream))
            {
                await blobData.DownloadToStreamAsync(memoryStream);

                // Read from stream to a json string
                memoryStream.Position = 0;
                var json = streamReader.ReadToEnd();

                // Convert json to results dictionary and return ->
                return JsonConvert.DeserializeObject<Dictionary<string, decimal>>(json);
            }
        }

    }
}
