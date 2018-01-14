using AtlasSSH;
using AtlasWorkFlows.Utils;
using CredentialManagement;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
    [Serializable]
    public class GRIDSubmitException : Exception
    {
        public GRIDSubmitException() { }
        public GRIDSubmitException(string message) : base(message) { }
        public GRIDSubmitException(string message, Exception inner) : base(message, inner) { }
        protected GRIDSubmitException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Some utility routines to help with working with jobs
    /// </summary>
    public static class JobUtils
    {
        /// <summary>
        /// Append an item to the array. Not efficeint, but...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="newItem"></param>
        /// <returns></returns>
        public static T[] Append<T>(this T[] old, T newItem)
        {
            var addon = new T[] { newItem };
            if (old == null)
            {
                return addon;
            }
            return old.Concat(addon).ToArray();
        }

        /// <summary>
        /// Return the hash code for this job definition.
        /// </summary>
        /// <param name="J"></param>
        /// <returns></returns>
        public static string Hash (this AtlasJob J)
        {
            return J.Print().ComputeMD5Hash();
        }

        /// <summary>
        /// We will run the job submission. We assume that any checking has already gone one
        /// and we are just going to execute all the commands required of this job request.
        /// Assume setupATLAS and rucio and voms proxy init have all been done before
        /// this guy is called!
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="job"></param>
        /// <param name="datasetToStartWith"></param>
        /// <param name="credSet">Set of credentials to load. Default to CERN</param>
        /// <returns></returns>
        public static async Task<ISSHConnection> SubmitJobAsync(this ISSHConnection connection, AtlasJob job, string inputDataSet, string resultingDataSet, Action<string> statusUpdate = null, Func<bool> failNow = null, bool sameJobAsLastTime = false, string credSet = "CERN", bool dumpOnly = false)
        {
            // Get the status update protected.
            Action<string> update = statusUpdate != null ? 
                statusUpdate 
                : s => { };

            // Figure out the proper submit command.
            string submitCmd = (job.SubmitPatternCommands.Length > 0
                ? MatchSubmitPattern(job.SubmitPatternCommands, inputDataSet)
                : job.SubmitCommand.SubmitCommand.CommandLine)
                .Replace("*INPUTDS*", "{0}")
                .Replace("*OUTPUTDS*", "{1}");

            var cernCred = new CredentialSet(credSet)
                .Load()
                .FirstOrDefault()
                .ThrowIfNull(() => new GRIDSubmitException($"Please create a windows generic credential with a target of '{credSet}' to allow access to kinit"));

            // If this is the first time through with a single job, then setup a directory we can use.
            if (!sameJobAsLastTime)
            {
                var linuxLocation = string.Format("/tmp/{0}", resultingDataSet);
                await connection.Apply(() => update("Removing old build directory"))
                        .ExecuteLinuxCommandAsync("rm -rf " + linuxLocation, dumpOnly: dumpOnly);

                await connection
                    .Apply(() => update("Setting up panda"))
                    .ExecuteLinuxCommandAsync("lsetup panda", dumpOnly: dumpOnly);
                await connection.Apply(() => update("Setting up release"))
                    .SetupRcReleaseAsync(linuxLocation, job.Release.Name, dumpOnly: dumpOnly);
                await connection.Apply(() => update("Getting CERN credentials"))
                    .KinitAsync(cernCred.Username, cernCred.Password, dumpOnly: dumpOnly);
                await connection
                    .ApplyAsync(job.Packages, (c, j) => c.Apply(() => update("Checking out package " + j.Name)).CheckoutPackageAsync(j.Name, j.SCTag, failNow: failNow, dumpOnly: dumpOnly));
                await connection
                    .ApplyAsync(job.Commands, (co, cm) => co.Apply(() => update("Running command " + cm.CommandLine)).ExecuteLinuxCommandAsync(cm.CommandLine, failNow: failNow, dumpOnly: dumpOnly));
                await connection
                    .Apply(() => update("Compiling release"))
                    .BuildWorkAreaAsync(failNow: failNow, dumpOnly: dumpOnly);
            }

            // We should now be in the directory where everything is - so submit!
            return await connection
                .Apply(() => update($"Running submit command ({inputDataSet})"))
                .ExecuteLinuxCommandAsync(string.Format(submitCmd, inputDataSet, resultingDataSet), failNow: failNow, dumpOnly: dumpOnly);
        }

        /// <summary>
        /// Look at the list of possible patterns and find a match. There had better be one and only one.
        /// </summary>
        /// <param name="submitPatternCommands"></param>
        /// <param name="inputDataset"></param>
        /// <returns></returns>
        private static string MatchSubmitPattern(SubmitPattern[] submitPatternCommands, string inputDataset)
        {
            var matched = from sc in submitPatternCommands
                          let reg = Regex.Match(inputDataset, sc.Regex)
                          where reg.Success
                          select sc;
            var all = matched.ToArray();
            if (all.Length != 1)
            {
                throw new GRIDSubmitException($"Dataset '{inputDataset}' does not match any patterns for job submission");
            }
            return all[0].SubmitCommand.SubmitCommand.CommandLine;
        }
    }
}
