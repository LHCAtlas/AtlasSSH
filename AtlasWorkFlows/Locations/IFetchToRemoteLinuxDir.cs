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
        void Fetch(string dsName, string linuxDirDestination);
    }
}
