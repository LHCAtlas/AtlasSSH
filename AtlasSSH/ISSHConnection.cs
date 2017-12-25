using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public interface ISSHConnection : IDisposable
    {
        ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 60*60, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string,string> seeAndRespond = null, bool waitForCommandReponse = true);
        Task<ISSHConnection> ExecuteCommandAsync(string command, Action<string> output = null, int secondsTimeout = 60 * 60, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool waitForCommandReponse = true);

        ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null);
        ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null);
        ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null);

        Task<ISSHConnection> CopyRemoteDirectoryLocallyAsync(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null);
        Task<ISSHConnection> CopyRemoteFileLocallyAsync(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null);
        Task<ISSHConnection> CopyLocalFileRemotelyAsync(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null);

        /// <summary>
        /// Username used to log into the machine we are talking to. May be quired without triggering a connection
        /// to the remote system.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Return the name of the machine we are connected to
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// True if the machine we are connected to is visible on the open internet.
        /// </summary>
        bool GloballyVisible { get; }
    }
}
