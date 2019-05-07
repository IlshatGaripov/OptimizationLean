using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Fitnesses;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;

namespace Optimization
{
    /// <summary>
    /// Fitness function that calculates evaluation in Azure Cloud.
    /// </summary>
    class AzureFitness: IFitness
    {
        /// <summary>
        /// Performs the evaluation against the specified chromosome.
        /// </summary>
        /// <param name="chromosome">The chromosome to be evaluated.</param>
        /// <returns>The fitness of the chromosome.</returns>
        public double Evaluate(IChromosome chromosome)
        {

            // all the code is wrapper in async method . Execute it in async fashion to obtain a result.

            return EvaluateAsync(chromosome).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Async implementation of evalutation function.
        /// </summary>
        private static async Task<double> EvaluateAsync(IChromosome chromosome)
        {
            // Cast to base chromosome type
            var chromosomeBase = (Chromosome)chromosome;

            // Application Package Directory 
            string appPath = String.Format("%AZ_BATCH_APP_PACKAGE_{0}#{1}%",
                AzureBatchManager.AppPackageId, AzureBatchManager.AppPackageVersion);

            // Unique identifier of a chromosome
            var id = chromosomeBase.Id;

            // Create a dictionary and populate it with gene key-value pairs
            var geneKeyValues = chromosomeBase.ToDictionary();

            // -- 1 -- Create an argument string of those key-values
            string runnerInputArguments = string.Empty;
            foreach (var pair in geneKeyValues)
            {
                runnerInputArguments += $"{pair.Key} {pair.Value} ";
            }

            // -- 2 -- Add an algorithm name to that string
            runnerInputArguments += $"algorithm-type-name {Program.Config.AlgorithmTypeName} ";

            // -- 3 -- Add algorithm dll and data folder locations
            runnerInputArguments += $"algorithm-location {appPath}\\Debug\\Optimization.Example.dll ";
            runnerInputArguments += $"data-folder {appPath}\\Debug\\Data\\ ";

            // -- 4 -- Algorithm start and end dates
            if (Program.Config.StartDate.HasValue && Program.Config.EndDate.HasValue)
            {
                runnerInputArguments += $"startDate {Program.Config.StartDate.Value:O} ";
                runnerInputArguments += $"endDate {Program.Config.EndDate.Value:O} ";
            }

            // -- 5 -- File name where the final result of a backtest will be stored at.
            var resultsOutputFile = $"output_{id}.json";
            runnerInputArguments += $"results-output {resultsOutputFile} ";

            // -- 6 -- File name for Lean logs
            var leanLogFile = $"log_{id}.txt";
            runnerInputArguments += $"log-file {leanLogFile}";

            // Now using all arguments construct the resulting command line string
            string taskCommandLine = $"cmd /c {appPath}\\Debug\\Optimization.RunnerAzureApp.exe {runnerInputArguments}";

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
            List<CloudTask> cloudTaskCollection = new List<CloudTask>
            {
                cloudTask
            };

            var batchClient = AzureBatchManager.BatchClient;
            var jobId = AzureBatchManager.JobId;

            // Call BatchClient.JobOperations.AddTask() to add the tasks as a collection to a queue
            await batchClient.JobOperations.AddTaskAsync(jobId, cloudTaskCollection);

            // Monitor for a task 01 to complete. Timeout is set to 20 minutes. 
            await MonitorSpecificTaskToCompleteAsync(batchClient, jobId, taskId, TimeSpan.FromMinutes(20));

            return 0;
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

                Console.WriteLine($"{taskId} reached state Completed within the timeout periods.");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
                Console.WriteLine($"{taskId} failed.");
            }
            else
            {
                Console.WriteLine($"{taskId} reached state Completed.");
            }
        }

    }
}
