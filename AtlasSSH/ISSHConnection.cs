using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AtlasSSH
{
    public interface ISSHConnection : IDisposable
    {
        ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 60*60, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string,string> seeAndRespond = null, bool waitForCommandReponse = true);
        ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null);
    }
}
