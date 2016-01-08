using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
    /// <summary>
    /// Everything you need to get a list of files from a dataset and a job definition - including copying and submitting
    /// </summary>
    public class AtlasJobEnvrionment
    {
        public AtlasJob Job { get; set; }

        /// <summary>
        /// Make a deep clone
        /// </summary>
        /// <returns></returns>
        public AtlasJobEnvrionment Clone(Action<AtlasJobEnvrionment> modifyIt = null)
        {
            // do a deep copy
            var result = new AtlasJobEnvrionment();
            result.Job = this.Job.Clone();

            // If asked, then we can modify this before it comes back (and avoid
            // the normal deep copy).
            if (modifyIt != null)
                modifyIt(result);
            return result;
        }
    }

    /// <summary>
    /// Helper classes to interact with the atlas job environment
    /// </summary>
    public static class AtlasJobEnvrionmentUtils
    {
        /// <summary>
        /// Return a clean atlas job environment
        /// </summary>
        public static AtlasJobEnvrionment CleanAtlasJobEnvrionment { get { return new AtlasJobEnvrionment(); } }

        #region AtlasJob Manipulation
        public static AtlasJobEnvrionment NameVersionRelease(this AtlasJobEnvrionment e, string name, int version, string release)
        {
            return e.Clone(r => r.Job = r.Job.NameVersionRelease(name,version,release));
        }

        public static AtlasJobEnvrionment Package(this AtlasJobEnvrionment e, string packageName, string SCTag = "")
        {
            return e.Clone(r => r.Job = r.Job.Package(packageName, SCTag));
        }

        public static AtlasJobEnvrionment Command(this AtlasJobEnvrionment e, string commandLine)
        {
            return e.Clone(r => r.Job = r.Job.Command(commandLine));
        }

        public static AtlasJobEnvrionment SubmitCommand(this AtlasJobEnvrionment e, string commandLine)
        {
            return e.Clone(r => r.Job = r.Job.SubmitCommand(commandLine));
        }
        #endregion

    }
}
