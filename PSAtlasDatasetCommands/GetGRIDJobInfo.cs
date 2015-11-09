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
                var names = t.datasets.Where(ds => ds.streamname == "IN").GroupBy(ds => ds.containername).Select(k => k.Key).ToArray();
                WriteObject(names);
            }
            if (OutputContainerNames.IsPresent)
            {
                var names = t.datasets.Where(ds => ds.streamname == "OUTPUT0").GroupBy(ds => ds.containername).Select(k => k.Key).ToArray();
                WriteObject(names);
            }
        }
    }
}
