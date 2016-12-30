using AtlasWorkFlows;
using AtlasWorkFlows.Jobs;
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
        [Parameter(Mandatory=true, HelpMessage="Rucio dataset name to fetch", ValueFromPipeline=true, Position=1, ParameterSetName = "dataset")]
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
        /// The location where we should download the dataset to.
        /// </summary>
        [Parameter(HelpMessage="Location where the dataset should be downloaded to")]
        [ValidateLocation]
        public string Location { get; set; }

        [Parameter(HelpMessage = "Timeout in seconds between updates from the download. Defaults to 1 hour")]
        public int Timeout { get; set; }

        /// <summary>
        /// Setup defaults.
        /// </summary>
        public GetGRIDDataset()
        {
            Timeout = 3600;
        }

        /// <summary>
        /// Initialize the context for making whatever connections we are going to need.
        /// </summary>
        protected override void BeginProcessing()
        {
        }

        /// <summary>
        /// Fetch a particular dataset.
        /// </summary>
        protected override void ProcessRecord()
        {
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // First, deal with the file filter
                Func<string[], string[]> filter = nFiles == 0 ? (Func<string[],string[]>) null : flist => flist.OrderBy(f => f).Take(nFiles).ToArray();

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

                // Now, how we pick up what we want will depend on if we are trying to download to a location.
                Uri[] r = null;
                if (!string.IsNullOrWhiteSpace(Location))
                {
                    using (var holder = GRIDDatasetLocator.SetLocationFilter(loc => false))
                    {
                        r = GRIDDatasetLocator.FetchDatasetUrisAtLocation(Location, dataset, fname => DisplayStatus(fname), fileFilter: filter, failNow: () => Stopping, timeoutDownloadSecs: Timeout);
                    }
                }
                else
                {
                    r = GRIDDatasetLocator.FetchDatasetUris(dataset, fname => DisplayStatus(fname), fileFilter: filter, failNow: () => Stopping, timeoutDownloadSecs: Timeout);
                }

                // Dump all the returned files out to whatever is next in the pipeline.
                using (var pl = listener.PauseListening())
                {
                    foreach (var ds in r)
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
        /// <param name="fname"></param>
        private void DisplayStatus(string fname)
        {
            var pr = new ProgressRecord(1, "Downloading", fname);
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
