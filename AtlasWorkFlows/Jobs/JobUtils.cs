using AtlasSSH;
using AtlasWorkFlows.Utils;
using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
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
        /// Functional throw.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static T ThrowIfNull<T>(this T obj, string message)
        {
            if (obj != null)
                return obj;

            throw new ArgumentException(message);
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
        /// <returns></returns>
        public static ISSHConnection SubmitJob(this ISSHConnection connection, AtlasJob job, string inputDataset, string resultingDataset, Action<string> statusUpdate = null, Func<bool> failNow = null, bool sameJobAsLastTime = false)
        {
            // Get the status update protected.
            Action<string> update = statusUpdate != null ? 
                statusUpdate 
                : s => { };

            // Now prep everything for use below (preprocessing).
            var submitCmd = job.SubmitCommand.SubmitCommand.CommandLine
                .Replace("*INPUTDS*", "{0}")
                .Replace("*OUTPUTDS*", "{1}");

            var cernCred = new CredentialSet("CERN")
                .Load()
                .FirstOrDefault()
                .ThrowIfNull("Please create a windows generic credential with a target of CERN to allow access to kinit");

            // If this is the first time through with a single job, then setup a directory we can use.
            if (!sameJobAsLastTime)
            {
                var linuxLocation = string.Format("/tmp/{0}", resultingDataset);
                connection
                    .Apply(() => update("Removing old build directory"))
                    .ExecuteCommand("rm -rf " + linuxLocation);

                connection
                .Apply(() => update("Setting up panda"))
                .ExecuteCommand("lsetup panda")
                .Apply(() => update("Setting up release"))
                .SetupRcRelease(linuxLocation, job.Release.Name)
                .Apply(() => update("Getting CERN credentials"))
                .Kinit(cernCred.Username, cernCred.Password)
                .Apply(job.Packages, (c, j) => c.Apply(() => update("Checking out package " + j.Name)).CheckoutPackage(j.Name, j.SCTag, failNow: failNow))
                .Apply(job.Commands, (co, cm) => co.Apply(() => update("Running command " + cm.CommandLine)).ExecuteLinuxCommand(cm.CommandLine, failNow: failNow))
                .Apply(() => update("Compiling release"))
                .BuildWorkArea(failNow: failNow);
            }

            // We should now be in the directory where everything is - so submit!
            return connection
                .Apply(() => update("Running submit command"))
                .ExecuteLinuxCommand(string.Format(submitCmd, inputDataset, resultingDataset), failNow: failNow);
        }

    }
}
