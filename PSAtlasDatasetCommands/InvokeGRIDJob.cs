using AtlasSSH;
using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Panda;
using CredentialManagement;
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
            // Setup for verbosity if we need it.
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // Get the job
                var job = JobParser.FindJob(JobName, JobVersion);

                // Get the expected resulting dataset name. Since this will be a personal
                // dataset, we need to get the GRID info.

                var gridCredentials = new CredentialSet("GRID").Load().FirstOrDefault();
                if (gridCredentials == null)
                {
                    throw new ArgumentException("Please create a generic windows credential with the target 'GRID' with the username as the rucio grid username and the password to be used with voms proxy init");
                }
                string ds = job.ResultingDatasetName(DatasetName, gridCredentials);

                // See if there is already a job defined that will produce this
                var pandaJob = (ds + "/").FindPandaJobWithTaskName();

                if (pandaJob == null)
                {
                    // Where are we going to be doing the submission on?
                    var sm = JobParser.GetSubmissionMachine();

                    // Check to see if the original dataset exists. We will use the location known as Local for doing the
                    // setup, I suppose.
                    var connection = new SSHConnection(sm.MachineName, sm.Username);
                    var files = connection
                        .Apply(() => DisplayStatus("Setting up ATLAS"))
                        .setupATLAS()
                        .Apply(() => DisplayStatus("Setting up Rucio"))
                        .setupRucio(gridCredentials.Username)
                        .Apply(() => DisplayStatus("Acquiring GRID credentials"))
                        .VomsProxyInit("atlas", failNow: () => Stopping)
                        .Apply(() => DisplayStatus("Checking dataset exists on the GRID"))
                        .FilelistFromGRID(DatasetName, failNow: () => Stopping);
                    if (files.Length == 0)
                    {
                        throw new ArgumentException("Unable to find dataset '{0}' on the grid!", DatasetName);
                    }

                    // Submit the job
                    connection
                        .SubmitJob(job, DatasetName, ds, DisplayStatus, failNow: () => Stopping);

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
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="fname"></param>
        private void DisplayStatus(string message)
        {
            var pr = new ProgressRecord(1, string.Format("Submitting {0} v{1}", JobName, JobVersion), message);
            WriteProgress(pr);
        }
    }
}
