using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Panda;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Diagnostics;
using System.Management.Automation;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Returns the job status from the GRID and bigpanda.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDJobInfo")]
    public class GetGRIDJobInfo : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "BigPanda task ID", Position = 1, ParameterSetName ="TaskID")]
        public int TaskID { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "BigPanda task name", Position = 1, ParameterSetName = "TaskName")]
        public string TaskName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "BigPanda task Object (e.g. from Invoke-GRIDJob)", ValueFromPipeline = true, Position = 1, ParameterSetName = "TaskObject")]
        public AtlasPandaTaskID PandaTaskObject { get; set; }

        [Parameter(Mandatory =true, HelpMessage ="Input dataset name", ValueFromPipeline =true, Position =1, ParameterSetName ="DatasetGRIDJob")]
        public string DatasetName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Input dataset name", ValueFromPipeline = false, ParameterSetName = "DatasetGRIDJob")]
        public int JobVersion { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Input dataset name", ValueFromPipeline = false, ParameterSetName = "DatasetGRIDJob")]
        public string JobName { get; set; }

        [Parameter(HelpMessage ="Get the job's current status")]
        public SwitchParameter JobStatus { get; set; }

        [Parameter(HelpMessage = "Get the job's output container names")]
        public SwitchParameter OutputContainerNames { get; set; }

        [Parameter(HelpMessage = "Get the job's input container names")]
        public SwitchParameter InputContainerNames { get; set; }
        
        /// <summary>
        /// Do the lookup!
        /// </summary>
        protected override void ProcessRecord()
        {
            // Setup for verbosity if we need it.
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                PandaTask t = null;
                bool needdatasets = OutputContainerNames.IsPresent || InputContainerNames.IsPresent;

                if (ParameterSetName == "TaskID")
                {
                    t = TaskID.FindPandaJobWithTaskName(needdatasets);
                }
                if (ParameterSetName == "TaskObject")
                {
                    t = PandaTaskObject.ID.FindPandaJobWithTaskName(needdatasets);
                }
                if (ParameterSetName == "TaskName")
                {
                    t = TaskName.FindPandaJobWithTaskName(needdatasets);
                }
                if (ParameterSetName == "DatasetGRIDJob")
                {
                    // Get the job and resulting dataset name.
                    var job = JobParser.FindJob(JobName, JobVersion);
                    var ds = job.ResultingDatasetName(DatasetName);

                    // Now, look up the job itself.
                    t = (ds + "/").FindPandaJobWithTaskName();
                }

                if (t == null)
                {
                    throw new ArgumentException("Unable to find the task!");
                }

                if (JobStatus.IsPresent)
                {
                    WriteObject(t.status);
                }
                if (InputContainerNames.IsPresent)
                {
                    WriteObject(t.DatasetNamesIN());
                }
                if (OutputContainerNames.IsPresent)
                {
                    WriteObject(t.DatasetNamesOUT());
                }
            } finally
            {
                Trace.Listeners.Remove(listener);
            }
        }
    }
}
