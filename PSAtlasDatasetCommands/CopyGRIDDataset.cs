using AtlasWorkFlows;
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
    /// Copy a GRID dataset from one location to another.
    /// </summary>
    [Cmdlet(VerbsCommon.Copy, "GRIDDataset")]
    public class CopyGRIDDataset : PSCmdlet
    {
        public CopyGRIDDataset()
        {
            Timeout = 60;
            JobIteration = 0;
        }

        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Rucio dataset name to copy", ValueFromPipeline = true, Position = 1, ParameterSetName = "dataset")]
        public string DatasetName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Rucio dataset name the job was run on", ParameterSetName = "job", ValueFromPipeline = true)]
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
        [Parameter(HelpMessage = "Maximum number of files to copy")]
        public int nFiles { get; set; }

        /// <summary>
        /// The location where we should find the dataset and copy it from.
        /// </summary>
        [Parameter(HelpMessage = "Location where the dataset should be copied from", Mandatory = true)]
        [ValidateLocation]
        public string SourceLocation { get; set; }

        /// <summary>
        /// The location where we should find the dataset and copy it to.
        /// </summary>
        [Parameter(HelpMessage = "Location where the dataset should be copied to", Mandatory = true)]
        [ValidateLocation]
        public string DestinationLocation { get; set; }

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
                // Get the actual dataset name.
                var dataset = DatasetName.Trim();
                if (ParameterSetName == "job")
                {
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
                    var containers = pandaTask.DataSetNamesOUT();
                    if (containers.Length > 1)
                    {
                        throw new ArgumentException($"There are more than one output container for the panda task {pandaTask.jeditaskid} - can't decide. Need code upgrade!! Thanks for the fish!");
                    }
                    dataset = containers.First();
                }

                // Find all the members of this dataset.
                var allFilesToCopy = DataSetManager.ListOfFilesInDataSetAsync(dataset, m => DisplayStatus($"Listing Files in {dataset}", m), failNow: () => Stopping)
                    .Result;
                if (nFiles != 0)
                {
                    allFilesToCopy = allFilesToCopy
                        .OrderBy(u => u.AbsolutePath)
                        .Take(nFiles)
                        .ToArray();
                }

                // Turn the source and destination locations into actual locations.
                var locSource = SourceLocation.AsIPlace().Result;
                var locDest = DestinationLocation.AsIPlace().Result;

                // Do the actual copy. This will fail if one of the files can't be found at the source.
                var resultUris = DataSetManager
                    .CopyFilesAsync(locSource, locDest, allFilesToCopy, mbox => DisplayStatus($"Downloading {dataset}", mbox), failNow: () => Stopping, timeout: Timeout)
                    .Result;

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
                Trace.Listeners.Remove(listener);
            }
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
            } else
            {
                _pr.Activity = phase;
                _pr.StatusDescription = message;
            }
            WriteProgress(_pr);
        }

        /// <summary>
        /// Make sure no connections have been left over from some previous failure.
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
