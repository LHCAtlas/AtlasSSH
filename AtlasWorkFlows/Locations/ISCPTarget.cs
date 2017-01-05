using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// An IPlace that supports a SCP target.
    /// Note: You must check to make sure IsVisible(location) is true before
    /// attempting, or you'll face a long timeout (e.g. behavior undefined).
    /// </summary>
    interface ISCPTarget
    {
        /// <summary>
        /// Return the machine name of the other target
        /// </summary>
        string SCPMachineName { get; }

        /// <summary>
        /// Return the username for accessing via scp the target.
        /// </summary>
        string SCPUser { get; }

        /// <summary>
        /// Return true if the machine's SCP end points can be accessed from
        /// the machine with the ip address <paramref name="internetLocation"/>.
        /// </summary>
        /// <param name="internetLocation">ip address of location that will be sourcing the copy.</param>
        /// <returns></returns>
        bool SCPIsVisibleFrom(string internetLocation);

        /// <summary>
        /// Return the absoulte path on the remote machine of the file we are interested in.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        string GetSCPFilePath(Uri f);

        /// <summary>
        /// Absolute path where files for this dataset should be deposited.
        /// This path must exist! :-)
        /// </summary>
        /// <param name="dsName"></param>
        /// <returns></returns>
        string GetPathToCopyFiles(string dsName);

        /// <summary>
        /// Copy the remote files listed in files down to the local disk directory.
        /// </summary>
        /// <param name="files">gridds uri's fo files that we want to have copied to the local directory</param>
        /// <param name="ourpath">Location to place the files. It should have been created already.</param>
        void CopyFromRemoteToLocal(string dsName, string[] files, DirectoryInfo ourpath);

        /// <summary>
        /// Copy a file from a local windows path to a remote dataset.
        /// It is assumed the catalog is already updated.
        /// </summary>
        /// <param name="dsName">Dataset name where the files should be stored</param>
        /// <param name="files">Files that we are going to copy to</param>
        void CopyFromLocalToRemote(string dsName, IEnumerable<FileInfo> files);
    }
}
