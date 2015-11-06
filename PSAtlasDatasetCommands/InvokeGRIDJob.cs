using AtlasWorkFlows.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Starts a job and returns a reference to it. If the job has already run, it will
    /// return a reference to that previously run job as well.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "GRIDJob")]
    public class InvokeGRIDJob : PSCmdlet
    {
        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Rucio dataset name for job input", ValueFromPipeline = true, Position = 3)]
        public string DatasetName { get; set; }

        /// <summary>
        /// Get/Set the name fo the job that we will be running.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Job name to apply to the dataset", Position = 1)]
        public string JobName { get; set; }

        /// <summary>
        /// Get/Set the name fo the job that we will be running.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Job name to apply to the dataset", Position = 2)]
        public int JobVersion { get; set; }

        /// <summary>
        /// Load up the job requested. Fail, obviously, if we can't.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Get the job
            var job = JobParser.FindJob(JobName, JobVersion);

            base.BeginProcessing();
        }
    }
}
