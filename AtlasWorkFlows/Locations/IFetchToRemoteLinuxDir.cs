using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Fetch a dataset from the GRID to a Linux machine.
    /// </summary>
    interface IFetchToRemoteLinuxDir
    {
        /// <summary>
        /// This will download the requested dataset from the GRID to a Linux directory. If anything goes
        /// wrong an exception should be thrown.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="linuxDirDestination"></param>
        void Fetch(string dsName, string linuxDirDestination, Action<string> statusUpdate = null, Func<string[], string[]> fileFilter = null, Func<bool> failNow = null);

        /// <summary>
        /// Return a list of all files in the dataset - this is everything, not just what is on disk.
        /// Expect that this will require going to the network.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        string[] GetListOfFiles(string p, Action<string> statusUpdate = null, Func<bool> failNow = null);

        /// <summary>
        /// Copy all files from the remote location down to a directory on Linux
        /// </summary>
        /// <param name="linuxLocation"></param>
        /// <param name="directoryInfo"></param>
        /// <remarks>
        /// Uses SCP
        /// </remarks>
        void CopyFromRemote(string linuxLocation, System.IO.DirectoryInfo directoryInfo, Action<string> statusUpdate = null, bool removeDirectoryWhenDone = false);
    }
}
