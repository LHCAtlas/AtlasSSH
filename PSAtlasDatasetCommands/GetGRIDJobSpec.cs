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
    /// Retrn a job specification (in english).
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDJobSpec")]
    public class GetGRIDJobSpec : PSCmdlet
    {
        [Parameter(Mandatory = true, HelpMessage = "Job Name to return", Position = 1, ValueFromPipelineByPropertyName = true)]
        public string JobName { get; set; }

        [Parameter(Mandatory = true, HelpMessage = "Job version to return", Position = 2, ValueFromPipelineByPropertyName = true)]
        public string JobVersion { get; set; }

        /// <summary>
        /// Should we pretty print (false) or nice print (true)?
        /// </summary>
        [Parameter(HelpMessage = "Print out the form of the job definition used to calculate hashes")]
        public SwitchParameter PrintCacheForm { get; set; }

        /// <summary>
        /// Make sure we are properly setup
        /// </summary>
        public GetGRIDJobSpec()
        {
            JobName = null;
            JobVersion = null;
        }

        /// <summary>
        /// Get a job and print it out.
        /// </summary>
        protected override void ProcessRecord()
        {
            int version;
            if (!int.TryParse(JobVersion, out version))
            {
                throw new ArgumentException(string.Format("JobVersion must be a valid integer, not '{0}'", JobVersion));
            }

            var job = JobParser.FindJob(JobName, version);

            if (job == null)
            {
                throw new ArgumentException(string.Format("Job '{0}' v{1} not found on system. Create .jobspec?", JobName, JobVersion));
            }

            var str = job.Print(prettyPrint: !PrintCacheForm.IsPresent);
            WriteObject(str);
        }
    }
}
