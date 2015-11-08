using AtlasWorkFlows.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Find a grid job spec from all grid jobs we know.
    /// </summary>
    [Cmdlet(VerbsCommon.Find, "GRIDJob")]
    public class FindGRIDJob : PSCmdlet
    {
        [Parameter(Mandatory = false, HelpMessage = "Job Name to return", Position = 1)]
        public string JobName { get; set; }

        [Parameter(Mandatory = false, HelpMessage = "Job version to return", Position = 2)]
        public string JobVersion { get; set; }

        /// <summary>
        /// Make sure we are properly setup
        /// </summary>
        public FindGRIDJob()
        {
            JobName = null;
            JobVersion = null;
        }

        /// <summary>
        /// Look for all jobs that match a certian criteria.
        /// </summary>
        protected override void ProcessRecord()
        {
            Func<AtlasJob, bool> nameSelector = _ => true;
            Func<AtlasJob, bool> versionSelector = _ => true;

            if (!string.IsNullOrWhiteSpace(JobName))
            {
                var matcher = new Regex(JobName.Replace("*", ".*"));
                nameSelector = j => matcher.Match(j.Name).Success;
            }

            if (!string.IsNullOrWhiteSpace(JobVersion))
            {
                int version = 0;
                if (!int.TryParse(JobVersion, out version))
                {
                    throw new ArgumentException(string.Format("JobVersion must be a valid integer, not '{0}'", JobVersion));
                }

                versionSelector = j => j.Version == version;
            }

            // Now we can actually go through and get all the jobs.

            var jobs = JobParser.FindJobs(j => nameSelector(j) && versionSelector(j));

            // And return what we found out.
            foreach (var j in jobs.Select(fullSpec => new AtlasJobSpec() { JobName = fullSpec.Name, JobVersion = fullSpec.Version }))
            {
                WriteObject(j);
            }
        }
    }
}
