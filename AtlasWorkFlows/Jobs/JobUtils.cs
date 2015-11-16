using AtlasSSH;
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
        /// Hash algorithm (which will work no matter what machine or bitness we are in).
        /// </summary>
        private static Lazy<MD5> _hasher = new Lazy<MD5>(() => MD5.Create());

        /// <summary>
        /// Return the hash code for this job definition.
        /// </summary>
        /// <param name="J"></param>
        /// <returns></returns>
        public static string Hash (this AtlasJob J)
        {
            var jobspec = J.Print();

            // Convert the input string to a byte array and compute the hash.
            byte[] data = _hasher.Value.ComputeHash(Encoding.UTF8.GetBytes(jobspec));
            var sBuilder = data.Where((x,i) => i % 4 == 0).Aggregate(new StringBuilder(), (bld, d) => { bld.Append(d.ToString("X2")); return bld; });

            Trace.WriteLine($"Hash {sBuilder.ToString()} for job spec {jobspec}");
            return sBuilder.ToString();
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
        public static ISSHConnection SubmitJob(this ISSHConnection connection, AtlasJob job, string inputDataset, string resultingDataset, Action<string> statusUpdate = null, Func<bool> failNow = null)
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

            // Actually run the buld and submit.
            var linuxLocation = string.Format("/tmp/{0}", resultingDataset);
            return connection
                .Apply(() => update("Removing old build directory"))
                .ExecuteCommand("rm -rf " + linuxLocation)
                .Apply(() => update("Setting up panda"))
                .ExecuteCommand("lsetup panda")
                .Apply(() => update("Setting up release"))
                .SetupRcRelease(linuxLocation, job.Release.Name)
                .Apply(() => update("Getting CERN credentials"))
                .Kinit(cernCred.Username, cernCred.Password)
                .Apply(job.Packages, (c, j) => c.Apply(() => update("Checking out package " + j.Name)).CheckoutPackage(j.Name, j.SCTag, failNow: failNow))
                .Apply(job.Commands, (co, cm) => co.Apply(() => update("Running command " + cm.CommandLine)).ExecuteLinuxCommand(cm.CommandLine, failNow: failNow))
                .Apply(() => update("Compiling release"))
                .BuildWorkArea(failNow: failNow)
                .Apply(() => update("Running submit command"))
                .ExecuteLinuxCommand(string.Format(submitCmd, inputDataset, resultingDataset), failNow: failNow);
        }

    }
}
