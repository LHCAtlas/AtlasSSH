﻿using AtlasWorkFlows;
using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Panda;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using Nito.AsyncEx.Synchronous;
using System.Management.Automation;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Fetch a dataset from the grid using a location profile. Passwords must be set in the credential cache for this to work!
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDDataset")]
    public class GetGRIDDataset : PSCmdlet
    {
        public GetGRIDDataset()
        {
            Timeout = 60;
            JobIteration = 0;
        }

        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory=true, HelpMessage="Rucio dataset name to copy", ValueFromPipeline=true, Position=1, ParameterSetName = "dataset")]
        public string DatasetName { get; set; }

        [Parameter(Mandatory =true, HelpMessage ="Rucio dataset name the job was run on", ParameterSetName ="job", ValueFromPipeline = true)]
        public string JobSourceDatasetName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Job that was run on the dataset", ParameterSetName = "job")]
        public string JobName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Version of the job that was run on the dataset", ParameterSetName = "job")]
        public int JobVersion { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Job iteration number", ParameterSetName = "job")]
        public int JobIteration { get; set; }

        /// <summary>
        /// Number of minutes for a timeout
        /// </summary>
        [Parameter(HelpMessage = "Number of minutes to wait for a timeout.")]
        public int Timeout { get; set; }

        /// <summary>
        /// How many files should be downloaded. They are sorted before making a determination (so it isn't random).
        /// </summary>
        [Parameter(HelpMessage="Maximum number of files to fetch")]
        public int nFiles { get; set; }

        /// <summary>
        /// The location where we should copy the dataset to.
        /// </summary>
        [Parameter(HelpMessage="Location where the dataset should be copied to")]
        [ValidateLocation]
        public string Location { get; set; }

        /// <summary>
        /// If set, return locations of all times
        /// </summary>
        [Parameter(HelpMessage = "If present then return the locations of the files")]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// Fetch a particular dataset.
        /// </summary>
        protected override void ProcessRecord()
        {
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // Get the dataset name that we are to process. Depending on parameters, it can come
                // from one of two places.
                var dataset = ParameterSetName == "job"
                    ? GetJobDatasetName()
                    : DatasetName.Trim();

                // Find all the members of this dataset.
                var allFilesToCopy = DataSetManager.ListOfFilesInDataSetAsync(dataset, m => DisplayStatus($"Listing Files in {dataset}", m), failNow: () => Stopping).Result;
                if (nFiles != 0)
                {
                    allFilesToCopy = allFilesToCopy
                        .OrderBy(u => u.AbsolutePath)
                        .Take(nFiles)
                        .ToArray();
                }

                // If we have a location, we want to do a copy. If we don't have a location, then we just
                // want to get the files somewhere local.
                var loc = Location.AsIPlace().Result;
                //WriteError(new ErrorRecord(err, "NoSuchLocation", ErrorCategory.InvalidArgument, null));

                var resultUris = loc == null
                    ? DataSetManager.MakeFilesLocalAsync(allFilesToCopy, m => DisplayStatus($"Downloading {dataset}", m), failNow: () => Stopping, timeout: Timeout).Result
                    : DataSetManager.CopyFilesToAsync(loc, allFilesToCopy, m => DisplayStatus($"Downloading {dataset}", m), failNow: () => Stopping, timeout: Timeout).Result;

                // Dump all the returned files out to whatever is next in the pipeline.
                if (PassThru.IsPresent)
                {
                    using (var pl = listener.PauseListening())
                    {
                        foreach (var ds in resultUris)
                        {
                            WriteObject(ds);
                        }
                    }
                }
            }
            finally
            {
                ClearStatus();
                Trace.Listeners.Remove(listener);
            }
        }

        private string GetJobDatasetName()
        {
            string dataset;
            // Get the job, see if it is finished, and then get the output dataset.
            var job = JobParser.FindJob(JobName, JobVersion);
            if (job == null)
            {
                throw new ArgumentException($"Job {JobName} v{JobVersion} is not known to the system!");
            }

            // Get the resulting job name for this guy.
            var pandaJobName = job.ResultingDataSetName(JobSourceDatasetName, JobIteration) + "/";

            // Now, to get the output dataset, we need to fetch the job.
            var pandaTask = pandaJobName.FindPandaJobWithTaskName(true);
            if (pandaTask == null)
            {
                throw new ArgumentException($"No panda task found with name '{pandaJobName}' for job '{JobName}' v{JobVersion}.");
            }
            if (pandaTask.status != "finished" && pandaTask.status != "done")
            {
                throw new ArgumentException($"Panda task {pandaTask.jeditaskid} has status {pandaTask.status} - which is not done or finished ({pandaJobName}).");
            }
            var containers = pandaTask.DataSetNamesOUT();
            if (containers.Length > 1)
            {
                throw new ArgumentException($"There are more than one output container for the panda task {pandaTask.jeditaskid} - can't decide. Need code upgrade!! Thanks for the fish!");
            }
            if (containers.Length == 0)
            {
                throw new ArgumentException($"There are no output containers for the panda task {pandaTask.jeditaskid} ({pandaJobName}) - so nothing to fetch!");
            }
            dataset = containers.First();
            return dataset;
        }

        private ProgressRecord _pr = null;

        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="message"></param>
        private void DisplayStatus(string phase, string message)
        {
            if (_pr == null)
            {
                _pr = new ProgressRecord(1, phase, message);
                _pr.RecordType = ProgressRecordType.Processing;
            }
            else
            {
                _pr.Activity = phase;
                _pr.StatusDescription = message;
            }
            WriteProgress(_pr);
        }

        /// <summary>
        /// Called when we are done with the status updates.
        /// </summary>
        private void ClearStatus()
        {
            if (_pr != null)
            {
                _pr.RecordType = ProgressRecordType.Completed;
                WriteProgress(_pr);
            }
        }

        /// <summary>
        /// Make sure no connections are left over from a previous run.
        /// </summary>
        protected override void BeginProcessing()
        {
            DataSetManager.ResetConnectionsAsync()
                .WaitAndUnwrapException();
            base.BeginProcessing();
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void EndProcessing()
        {
            DataSetManager.ResetConnectionsAsync()
                .WaitAndUnwrapException();
            base.EndProcessing();
        }
    }
}
