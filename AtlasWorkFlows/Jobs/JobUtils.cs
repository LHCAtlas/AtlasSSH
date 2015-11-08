using AtlasSSH;
using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public static int Hash (this AtlasJob J)
        {
            return J.Print().GetHashCode();
        }

        /// <summary>
        /// Use apply to make repeated application of things to a connection easy to read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="what"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static ISSHConnection Apply<T>(this ISSHConnection connection, IEnumerable<T> what, Action<ISSHConnection, T> doit)
        {
            foreach (var w in what)
            {
                doit(connection, w);
            }
            return connection;
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
        public static ISSHConnection SubmitJob(this ISSHConnection connection, AtlasJob job, string inputDataset, string resultingDataset)
        {
            var submitCmd = job.SubmitCommand.SubmitCommand.CommandLine
                .Replace("*INPUTDS*", "{0}")
                .Replace("*OUTPUTDS*", "{1}");

            var cernCred = new CredentialSet("CERN")
                .Load()
                .FirstOrDefault()
                .ThrowIfNull("Please create a windows generic credential with a target of CERN to allow access to kinit");

            var linuxLocation = string.Format("/tmp/{0}", resultingDataset);
            return connection
                .ExecuteCommand("rm -rf " + linuxLocation)
                .SetupRcRelease(linuxLocation, job.Release.Name)
                .Kinit(cernCred.Username, cernCred.Password)
                .Apply(job.Packages, (c, j) => c.CheckoutPackage(j.Name, j.SCTag))
                .Apply(job.Commands, (co, cm) => co.ExecuteLinuxCommand(cm.CommandLine))
                .BuildWorkArea()
                .ExecuteLinuxCommand(string.Format(submitCmd, inputDataset, resultingDataset));
        }

    }
}
