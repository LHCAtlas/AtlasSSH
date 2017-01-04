using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
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
        /// Return the path to a dataset that can be used in an SCP copy.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetSCPDatasetPath(string dsName);

        /// <summary>
        /// Record this dataset information in the local repro.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="v"></param>
        void CopyDataSetInfo(string key, string[] v);

        /// <summary>
        /// Absolute path where files for this dataset should be deposited.
        /// This path must exist! :-)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string GetPathToCopyFiles(string key);
    }
}
