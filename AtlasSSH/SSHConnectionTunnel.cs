using Polly;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static AtlasSSH.SSHConnection;
using System.IO;

namespace AtlasSSH
{
    /// <summary>
    /// A wrapper for a connection that enables you to use a "tunnel" syntax to connect
    /// between various machines. The following are all valid as arguments:
    ///     user@machine.com - Opens a connection to machine
    ///     user@machine.com -> user@machine2.com - opens a connection to machine 2 via a tunnel through machine 1
    ///     -tunneling as many hops as you want-
    /// 
    /// Connections can be added at anytime. So you can make a connection, do a few things, and then tunnel.
    /// If you issue an "exit" and a connection closes, this will not properly detect that.
    /// 
    /// Make sure to dispose of this object. All connections in the stack will be closed.
    /// 
    /// The connections aren't actually made until you access the Connection object.
    /// 
    /// Each connection tunnel isn't a seeprate connection, so it doesn't make sense to iterate through them.
    /// </summary>
    public class SSHConnectionTunnel : ISSHConnection
    {
        /// <summary>
        /// Return the last connection in our list.
        /// </summary>
        public string MachineName { get; private set; }

        /// <summary>
        /// Return the username of the deepest tunnel
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Return how many tunnels we have... (or will have). 0 if we will only have a connection to one machine.
        /// </summary>
        public int TunnelCount { get; private set; } = 0;

        /// <summary>
        /// Assume if we didn't have to tunnel, then we don't need to know about the globally visible guy.
        /// </summary>
        public bool GloballyVisible => TunnelCount == 0;

        /// <summary>
        /// Use a connection string like "user@machine1.com" or "user@machine1.com -> user@machine2.com" to
        /// make a connection via tunneling (or not!)
        /// </summary>
        /// <param name="connectionString"></param>
        public SSHConnectionTunnel(string connectionString = null)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                Add(connectionString);
            }
        }

        /// <summary>
        /// Used to hold connection strings
        /// </summary>
        private List<string> _machineConnectionStrings = new List<string>();

        /// <summary>
        /// Add a new tunnel. Use the format "user@machine.com" or "user@machine.com -> user@Machine2.com", etc.
        /// </summary>
        /// <param name="connection"></param>
        public void Add(string connection)
        {
            var mlist = connection
                .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());

            foreach (var c in mlist)
            {
                AddSingleConnection(c);
            }
        }

        /// <summary>
        /// Add a single connection to the list. If we are already opened, then we add another one.
        /// </summary>
        /// <param name="c"></param>
        private void AddSingleConnection(string c)
        {
            var userMachineInfo = ExtractUserAndMachine(c);
            MachineName = userMachineInfo.machine;
            Username = userMachineInfo.user;
            if (_deepestConnection == null)
            {
                _machineConnectionStrings.Add(c);
                TunnelCount = _machineConnectionStrings.Count - 1;
            }
            else
            {
                TunnelCount++;
                MakeSSHTunnelConnection(c);
            }
        }

        /// <summary>
        /// The deepest connection - makes for easy access
        /// </summary>
        private SSHConnection _deepestConnection = null;

        /// <summary>
        /// List of connections that we have, each one feeding to the next.
        /// </summary>
        private List<IDisposable> _connectionStack = null;

        /// <summary>
        /// Make the connection if it hasn't been made already
        /// </summary>
        /// <returns></returns>
        private SSHConnection GetDeepestConnection()
        {
            MakeConnections();
            if (_deepestConnection == null)
            {
                throw new InvalidOperationException("Attempt to use the SSHTunnelConnection without initalizing any connections");
            }
            return _deepestConnection;
        }

        /// <summary>
        /// Called to make the connections
        /// </summary>
        private void MakeConnections()
        {
            if (_deepestConnection == null && _machineConnectionStrings.Count > 0)
            {
                Policy
                    .Handle<UnableToCreateSSHTunnelException>()
                .WaitAndRetry(new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Execute(() =>
                {
                    _connectionStack = new List<IDisposable>();
                    var userMachineInfo = ExtractUserAndMachine(_machineConnectionStrings[0]);
                    _deepestConnection = new SSHConnection(userMachineInfo.machine, userMachineInfo.user);

                    foreach (var cinfo in _machineConnectionStrings.Skip(1))
                    {
                        MakeSSHTunnelConnection(cinfo);
                    }

                    // We don't need to track the connection strings any more.
                    _machineConnectionStrings = null;
                });
            } else if (_deepestConnection == null && _machineConnectionStrings.Count == 0)
            {
                throw new InvalidOperationException("Attempt to form an SSH tunnel without specifying any destination.");
            }
        }

        /// <summary>
        /// Given the connections already are made, add one to the stack.
        /// </summary>
        /// <param name="cinfo"></param>
        private void MakeSSHTunnelConnection(string cinfo)
        {
            var cUserMachine = ExtractUserAndMachine(cinfo);
            _connectionStack.Add(_deepestConnection.SSHTo(cUserMachine.machine, cUserMachine.user));
        }

        /// <summary>
        /// Thrown if we can't figure out how to parse the username/host string.
        /// </summary>
        [Serializable]
        public class InvalidHostSpecificationException : Exception
        {
            public InvalidHostSpecificationException() { }
            public InvalidHostSpecificationException(string message) : base(message) { }
            public InvalidHostSpecificationException(string message, Exception inner) : base(message, inner) { }
            protected InvalidHostSpecificationException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Extract the username and the machine from the connection string
        /// </summary>
        /// <param name="v"></param>
        /// <returns>Tuple with 0 being the user and 1 the machine</returns>
        private (string user, string machine) ExtractUserAndMachine(string v)
        {
            var t = v.Split('@');
            if (t.Length != 2)
            {
                throw new InvalidHostSpecificationException($"Looking for a SSH connection in the form of user@machine, but found '{v}' instead!");
            }
            return (t[0].Trim(), t[1].Trim());
        }

        /// <summary>
        /// Close off everything.
        /// </summary>
        public void Dispose()
        {
            if (_connectionStack != null)
            {
                foreach (var c in _connectionStack.Reverse<IDisposable>())
                {
                    c.Dispose();
                }
            }
        }

        /// <summary>
        /// Execute the command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="secondsTimeout"></param>
        /// <param name="refreshTimeout"></param>
        /// <param name="failNow"></param>
        /// <param name="dumpOnly"></param>
        /// <param name="seeAndRespond"></param>
        /// <param name="waitForCommandReponse"></param>
        /// <returns></returns>
        public ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 3600, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool waitForCommandReponse = true)
        {
            MakeConnections();
            return _deepestConnection.ExecuteCommand(command, output, secondsTimeout, refreshTimeout, failNow, dumpOnly, seeAndRespond, waitForCommandReponse);
        }

        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            MakeConnections();
            return _deepestConnection.CopyRemoteDirectoryLocally(remotedir, localDir, statusUpdate);
        }

        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            MakeConnections();
            return _deepestConnection.CopyRemoteFileLocally(lx, localFile, statusUpdate, failNow);
        }

        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            MakeConnections();
            return _deepestConnection.CopyLocalFileRemotely(localFile, linuxPath, statusUpdate, failNow);
        }
    }
}
