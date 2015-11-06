using AtlasSSH;
using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Panda;
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

            // Get the expected resulting dataset name
            var scope = "user.gwatts";
            var ds = job.ResultingDatasetName(DatasetName, scope);

            // See if there is already a job defined that will produce this
            var pandaJob = (ds + "/").FindPandaJobWithTaskName();

            if (pandaJob == null)
            {
                // Check to see if the original dataset exists. We will use the location known as Local for doing the
                // setup, I suppose.
                var connection = new SSHConnection("host", "me");
                var files = connection
                    .setupATLAS()
                    .setupRucio("dude")
                    .VomsProxyInit("atlas", "dude")
                    .FilelistFromGRID(DatasetName);
                if (files.Length == 0)
                {
                    throw new ArgumentException("Unable to find dataset '{0}' on the grid!");
                }

                // Submit the job
                connection
                    .submitJob(job, DatasetName, ds);

                // Try to find the job again. If this fails, then things are really bad!
                pandaJob = (ds + "/").FindPandaJobWithTaskName();
                if (pandaJob == null)
                {
                    throw new InvalidOperationException(string.Format("Unknown error - submitted job ({0},{1}) on dataset {2}, but no panda task found!", JobName, JobVersion, DatasetName));
                }
            }

            // Return a helper obj that contains the info about this job that can be used by other commands.
            var r = new AtlasPandaTaskID() { ID = pandaJob.jeditaskid, Name = pandaJob.taskname };
            WriteObject(r);
        }
    }
}
