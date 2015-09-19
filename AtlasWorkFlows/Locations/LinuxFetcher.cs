using AtlasSSH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Fetch a dataset from the GRID to a local directory
    /// </summary>
    class LinuxFetcher : IFetchToRemoteLinuxDir
    {
        private string _username;
        private string _linuxHost;

        /// <summary>
        /// Initialize the Linux fetcher
        /// </summary>
        /// <param name="username"></param>
        public LinuxFetcher(string linuxHost, string username)
        {
            _username = username;
            _linuxHost = linuxHost;
        }

        /// <summary>
        /// Do the actual fetching
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="linuxDirDestination"></param>
        public void Fetch(string dsName, string linuxDirDestination, Action<string> statusUpdater = null)
        {
            using (var s = new SSHConnection(_linuxHost, _username))
            {
                s
                    .setupATLAS()
                    .setupRucio(_username)
                    .VomsProxyInit("atlas", _username)
                    .DownloadFromGRID(dsName, linuxDirDestination, statusUpdater);
            }
        }
    }
}
