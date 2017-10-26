using AtlasSSH;
using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Panda;
using CredentialManagement;
using Polly;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;

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

        [Parameter(Mandatory = false, HelpMessage = "Job iteration. Defaults to 0, add 1, 2, etc., to re-run same job with slightly different output dataset.")]
        public int JobIteration { get; set; }

        /// <summary>
        /// Set this switch if you want to wait for the job to show up in panda. This can take a very long time, so it is
        /// not recommended.
        /// </summary>
        [Parameter(HelpMessage = "Wait for the job to be registered in Panda. This can take many minutes. Not recommended that you use this. Will also cause the command to return the panda info object.")]
        public SwitchParameter WaitForPandaRegistration { get; set; }

        /// <summary>
        /// Debug and see what might happen.
        /// </summary>
        [Parameter(HelpMessage = "The final submit command will not be issued, but will be written to Host output instead. Everything else will be run.")]
        public SwitchParameter WhatIf { get; set; }

        [Parameter(HelpMessage = "Defaults to using the local panda task cache")]
        public SwitchParameter DoNotUsePandaTaskCache { get; set; }

        /// <summary>
        /// Hold onto the connection
        /// </summary>
        private SSHConnection _connection = null;

        /// <summary>
        /// Hold onto the grid credentials
        /// </summary>
        private Credential _gridCredentials = null;

        /// <summary>
        /// Initialize common command defaults.
        /// </summary>
        public InvokeGRIDJob()
        {
            JobIteration = 0;
        }

        /// <summary>
        /// Setup a few things for running. Much of what we need
        /// is only lazy initalized.
        /// </summary>
        protected override void BeginProcessing()
        {
            // Setup for verbosity if we need it.
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // Get the grid credentials
                _gridCredentials = new CredentialSet("GRID").Load().FirstOrDefault();
                if (_gridCredentials == null)
                {
                    throw new ArgumentException("Please create a generic windows credential with the target 'GRID' with the username as the rucio grid username and the password to be used with voms proxy init");
                }
            } finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Load up the job requested. Fail, obviously, if we can't.
        /// </summary>
        protected override void ProcessRecord()
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
                var originalDatasetName = DatasetName.Trim();
                string resultDatasetName = job.ResultingDatasetName(originalDatasetName, _gridCredentials, JobIteration);

                // See if there is already a job defined that will produce this
                var pandaJob = (resultDatasetName + "/").FindPandaJobWithTaskName(useCacheIfPossible: !DoNotUsePandaTaskCache.IsPresent);

                if (pandaJob == null)
                {
                    // Where are we going to be doing the submission on?
                    var sm = JobParser.GetSubmissionMachine();

                    // Get the remove environment configured if it needs to be
                    var firstJob = false;
                    if (_connection == null)
                    {
                        firstJob = true;
                        _connection = new SSHConnection(sm.MachineName, sm.Username);
                        _connection
                            .Apply(() => DisplayStatus("Setting up ATLAS"))
                            .setupATLAS(dumpOnly: WhatIf.IsPresent)
                            .Apply(() => DisplayStatus("Setting up Rucio"))
                            .setupRucio(_gridCredentials.Username, dumpOnly: WhatIf.IsPresent)
                            .Apply(() => DisplayStatus("Acquiring GRID credentials"))
                            .VomsProxyInit("atlas", failNow: () => Stopping, dumpOnly: WhatIf.IsPresent);
                    }

                    // Check to see if the original dataset exists. We will use the location known as Local for doing the
                    // setup, I suppose.
                    var files = _connection
                        .Apply(() => DisplayStatus("Checking dataset exists on the GRID"))
                        .FilelistFromGRID(originalDatasetName, failNow: () => Stopping, dumpOnly: WhatIf.IsPresent);
                    if (files.Length == 0 && !WhatIf.IsPresent)
                    {
                        throw new ArgumentException($"Dataset '{originalDatasetName}' has zero files - won't submit a job against it!");
                    }

                    // Submit the job
                    _connection
                        .SubmitJob(job, originalDatasetName, resultDatasetName, DisplayStatus, failNow: () => Stopping, sameJobAsLastTime: !firstJob, dumpOnly: WhatIf.IsPresent);

                    // Try to find the job again if requested. The submission can take a very long time to show up in
                    // big panda, so skip unless requested.
                    if (WaitForPandaRegistration)
                    {
                        var pandJobResult = Policy
                            .Handle<InvalidOperationException>()
                            .WaitAndRetryForever(nthRetry => TimeSpan.FromMinutes(1),
                                                 (e, ts) => { WriteWarning($"Failed to find the submitted panda job on bigpanda: {e.Message}. Will wait one minute and try again."); })
                            .ExecuteAndCapture(() => FindPandaJobForDS(resultDatasetName));
                        if (pandJobResult.Outcome != OutcomeType.Successful)
                        {
                            throw pandJobResult.FinalException;
                        }
                        pandaJob = pandJobResult.Result;
                    }
                }

                // Return a helper obj that contains the info about this job that can be used by other commands.
                if (pandaJob != null)
                {
                    var r = new AtlasPandaTaskID() { ID = pandaJob.jeditaskid, Name = pandaJob.taskname };
                    using (var pp = listener.PauseListening())
                    {
                        WriteObject(r);
                    }
                }
            } finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Do the panda task lookup. Throw if we can't find the job.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private PandaTask FindPandaJobForDS(string ds)
        {
            PandaTask pandaJob = (ds + "/").FindPandaJobWithTaskName();
            if (pandaJob == null)
            {
                throw new InvalidOperationException(string.Format("Unknown error - submitted job ({0},{1}) on dataset {2}, but no panda task found!", JobName, JobVersion, DatasetName));
            }

            return pandaJob;
        }

        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="fname"></param>
        private void DisplayStatus(string message)
        {
            var pr = new ProgressRecord(1, $"Submitting {JobName} v{JobVersion} ({DatasetName})", message);
            WriteProgress(pr);
        }
    }
}
