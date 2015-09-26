using AtlasSSH;
using System;
using System.Collections.Generic;
using System.IO;
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
            var c = InitConnection(statusUpdater);
            if (statusUpdater != null)
                statusUpdater("Starting download of files from GRID");
            c.DownloadFromGRID(dsName, linuxDirDestination, statusUpdater, fileFilter);
        }

        /// <summary>
        /// Init the connection for use by other parts of this object.
        /// </summary>
        /// <returns></returns>
        private SSHConnection InitConnection(Action<string> statusUpdater)
        {
            if (_connection != null)
                return _connection;

            if (statusUpdater != null)
                statusUpdater("Setting up GRID Environment");
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
        public string[] GetListOfFiles(string dsName, Action<string> statusUpdater = null)
        {
            var s = InitConnection(statusUpdater);
            if (statusUpdater != null)
                statusUpdater("Getting list of files in Dataset");
            return s.FilelistFromGRID(dsName);
        }

        /// <summary>
        /// Get rid of the connection when we are done.
        /// </summary>
        public void Dispose()
        {
            if (_connection != null)
                _connection.Dispose();
        }

        /// <summary>
        /// Download a directory from a remote location to here.
        /// </summary>
        /// <param name="linuxLocation"></param>
        /// <param name="directoryInfo"></param>
        public void CopyFromRemote(string linuxLocation, DirectoryInfo directoryInfo, Action<string> statusUpdater = null, bool whenremoveDirectoryWhenDone = false)
        {
            var c = InitConnection(statusUpdater);
            if (statusUpdater != null)
                statusUpdater("Copying downloaded files back from remote via SCP.");
            c.CopyRemoteDirectoryLocally(linuxLocation, directoryInfo);

            // Remove that directory?
            if (whenremoveDirectoryWhenDone)
            {
                c.ExecuteCommand(string.Format("rm -rf {0}", linuxLocation), statusUpdater);
            }
        }
    }
}
