using AtlasSSH;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    /// <summary>
    /// Dummy class for testing
    /// </summary>
    public class dummySSHConnection : ISSHConnection
    {
        private Dictionary<string, string> _responses = null;

        const string CrLf = "\r\n";

        [Serializable]
        public class UnknownTestCommandException : Exception
        {
            public UnknownTestCommandException() { }
            public UnknownTestCommandException(string message) : base(message) { }
            public UnknownTestCommandException(string message, Exception inner) : base(message, inner) { }
            protected UnknownTestCommandException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context)
            { }
        }

        /// <summary>
        /// If we see a given command we should return with a response.
        /// </summary>
        /// <param name="commandAndResponse"></param>
        public dummySSHConnection(Dictionary<string, string> commandAndResponse)
        {
            _responses = commandAndResponse;
        }

        private class CommandChangeInfo
        {
            public string _afterCommand;
            public string _command;
            public string _response;
        }

        /// <summary>
        /// Keep track fo commands to change.
        /// </summary>
        private Queue<CommandChangeInfo> _changeQueue = new Queue<CommandChangeInfo>();

        public string Username => throw new NotImplementedException();

        public string MachineName => throw new NotImplementedException();

        public bool GloballyVisible => throw new NotImplementedException();

        /// <summary>
        /// After we see a command, then do the replace. We will allow the given command to "execute" first.
        /// These are Queued.
        /// </summary>
        /// <param name="afterSeeCommand"></param>
        /// <param name="command"></param>
        /// <param name="commandResponse"></param>
        /// <returns></returns>
        public dummySSHConnection AddQueuedChange(string afterSeeCommand, string command, string commandResponse)
        {
            _changeQueue.Enqueue(new CommandChangeInfo() { _afterCommand = afterSeeCommand, _command = command, _response = commandResponse });
            return this;
        }

        /// <summary>
        /// Execute a command and return whatever we are supposed to.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="secondsTimeout"></param>
        /// <returns></returns>
        public ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 60*60, bool refreshTiming = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool WaitForCommandResponse = false)
        {
            string result;
            if (!_responses.TryGetValue(command, out result))
            {
                Console.WriteLine("Error: asked for a command '{0}' and had no response!", command);
                throw new UnknownTestCommandException("Command " + command + " not known - can't response");
            }
            using (var lstream = new StringReader(result))
            {
                string line;
                while ((line = lstream.ReadLine()) != null) {
                    if (output != null) {
                        output(line);
                    }
                }
            }

            // If the next queued item matches, then we should "execute" the update
            if (_changeQueue.Count > 0)
            {
                if (_changeQueue.Peek()._afterCommand == command)
                {
                    var x = _changeQueue.Dequeue();
                    _responses[x._command] = x._response;
                }
            }

            // Return ourselves so we can continue to be all functionally!
            return this;
        }

        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, System.IO.DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }

        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }

        public Task<ISSHConnection> ExecuteCommandAsync(string command, Action<string> output = null, int secondsTimeout = 3600, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool waitForCommandReponse = true)
        {
            return Task<ISSHConnection>.Factory.StartNew(() => ExecuteCommand(command, output, secondsTimeout, refreshTimeout, failNow, dumpOnly, seeAndRespond, waitForCommandReponse));
        }

        public Task<ISSHConnection> CopyRemoteDirectoryLocallyAsync(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            throw new NotImplementedException();
        }

        public Task<ISSHConnection> CopyRemoteFileLocallyAsync(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }

        public Task<ISSHConnection> CopyLocalFileRemotelyAsync(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }
    }
}
