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
    [Cmdlet(VerbsCommon.Get, "GRIDJobStatus")]
    public class GetGRIDJobStatus : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "BigPanda task ID", Position = 1, ParameterSetName ="TaskID")]
        public int TaskID { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "BigPanda task name", Position = 1, ParameterSetName = "TaskName")]
        public string TaskName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "BigPanda task Object (e.g. from Invoke-GRIDJob)", ValueFromPipeline = true, Position = 1, ParameterSetName = "TaskObject")]
        public AtlasPandaTaskID PandaTaskObject { get; set; }

        /// <summary>
        /// Do the lookup!
        /// </summary>
        protected override void ProcessRecord()
        {
            PandaTask t = null;
            if (ParameterSetName == "TaskID")
            {
                t = TaskID.FindPandaJobWithTaskName();
            }
            if (ParameterSetName == "TaskObject")
            {
                t = PandaTaskObject.ID.FindPandaJobWithTaskName();
            }
            if (ParameterSetName == "TaskName")
            {
                t = TaskName.FindPandaJobWithTaskName();
            }

            if (t == null)
            {
                throw new ArgumentException("Unable to find the task!");
            }
            WriteObject(t.status);
        }
    }
}
