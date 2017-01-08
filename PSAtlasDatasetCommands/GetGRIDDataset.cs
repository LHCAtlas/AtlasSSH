using AtlasWorkFlows;
using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Panda;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Fetch a dataset from the grid using a location profile. Passwords must be set in the credential cache for this to work!
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDDataset")]
    public class GetGRIDDataset : PSCmdlet
    {
        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory=true, HelpMessage="Rucio dataset name to copy", ValueFromPipeline=true, Position=1, ParameterSetName = "dataset")]
        public string DatasetName { get; set; }

        [Parameter(Mandatory =true, HelpMessage ="Rucio dataset name the job was run on", ParameterSetName ="job")]
        public string JobSourceDatasetName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Job that was run on the dataset", ParameterSetName = "job")]
        public string JobName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Version of the job that was run on the dataset", ParameterSetName = "job")]
        public int JobVersion { get; set; }

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
        /// Fetch a particular dataset.
        /// </summary>
        protected override void ProcessRecord()
        {
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // Get the actual dataset name.
                var dataset = DatasetName;
                if (ParameterSetName == "job")
                {
                    // Get the job, see if it is finished, and then get the output dataset.
                    var job = JobParser.FindJob(JobName, JobVersion);
                    if (job == null)
                    {
                        throw new ArgumentException($"Job {JobName} v{JobVersion} is not known to the system!");
                    }

                    // Get the resulting job name for this guy.
                    var pandaJobName = job.ResultingDatasetName(JobSourceDatasetName) + "/";

                    // Now, to get the output dataset, we need to fetch the job.
                    var pandaTask = pandaJobName.FindPandaJobWithTaskName(true);
                    if (pandaTask == null)
                    {
                        throw new ArgumentException($"No panda task found with name '{pandaJobName}' for job '{JobName}' v{JobVersion}.");
                    }
                    var containers = pandaTask.DatasetNamesOUT();
                    if (containers.Length > 1)
                    {
                        throw new ArgumentException($"There are more than one output container for the panda task {pandaTask.jeditaskid} - can't decide. Need code upgrade!! Thanks for the fish!");
                    }
                    dataset = containers.First();
                }

                // Find all the members of this dataset.
                var allFilesToCopy = DatasetManager.ListOfFilesInDataset(dataset, m => DisplayStatus($"Listing Files in {dataset}", m), failNow: () => Stopping);
                if (nFiles != 0)
                {
                    allFilesToCopy = allFilesToCopy
                        .OrderBy(u => u.AbsolutePath)
                        .Take(nFiles)
                        .ToArray();
                }

                // If we have a location, we want to do a copy. If we don't have a location, then we just
                // want to get the files somewhere local.
                var loc = string.IsNullOrWhiteSpace(Location)
                    ? (IPlace)null
                    : DatasetManager.FindLocation(Location);

                if (loc == null && !string.IsNullOrWhiteSpace(Location))
                {
                    var err = new ArgumentException($"Location {Location} is now known to us!");
                    WriteError(new ErrorRecord(err, "NoSuchLocation", ErrorCategory.InvalidArgument, null));
                    throw err;
                }

                var resultUris = loc == null
                    ? DatasetManager.MakeFilesLocal(allFilesToCopy, m => DisplayStatus($"Downloading {dataset}", m), failNow: () => Stopping)
                    : DatasetManager.CopyFilesTo(loc, allFilesToCopy, m => DisplayStatus($"Downloading {dataset}", m), failNow: () => Stopping);

                // Dump all the returned files out to whatever is next in the pipeline.
                using (var pl = listener.PauseListening())
                {
                    foreach (var ds in resultUris)
                    {
                        WriteObject(ds);
                    }
                }
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }


        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="message"></param>
        private void DisplayStatus(string phase, string message)
        {
            var pr = new ProgressRecord(1, phase, message);
            WriteProgress(pr);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();
        }
    }
}
