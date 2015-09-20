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
    class LinuxFetcher : IFetchToRemoteLinuxDir, IDisposable
    {
        private string _username;
        private string _linuxHost;

        private SSHConnection _connection = null;

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
        public void Fetch(string dsName, string linuxDirDestination, Action<string> statusUpdater = null, Func<string[], string[]> fileFilter = null)
        {
            InitConnection()
                .DownloadFromGRID(dsName, linuxDirDestination, statusUpdater, fileFilter);
        }

        /// <summary>
        /// Init the connection for use by other parts of this object.
        /// </summary>
        /// <returns></returns>
        private SSHConnection InitConnection()
        {
            if (_connection != null)
                return _connection;

            _connection = new SSHConnection(_linuxHost, _username);
            _connection
                    .setupATLAS()
                    .setupRucio(_username)
                    .VomsProxyInit("atlas", _username);

            return _connection;
        }

        /// <summary>
        /// Get a list of files back.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public string[] GetListOfFiles(string dsName)
        {
            return InitConnection()
                .FilelistFromGRID(dsName);
        }

        /// <summary>
        /// Get rid of the connection when we are done.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null)
                _connection.Dispose();
        }
    }
}
