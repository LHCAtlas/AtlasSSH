using Polly;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.SSHConnection;

namespace AtlasSSH
{
    /// <summary>
    /// A connection that can recover from being broken.
    /// </summary>
    public class SSHRecoveringConnection : ISSHConnection
    {
        /// <summary>
        /// The function that will return a connection anytime we need one.
        /// </summary>
        private Func<ISSHConnection> _makeConnection;

        /// <summary>
        /// How long to wait between attempts. By default it is 10 seconds.
        /// </summary>
        public TimeSpan RetryWaitPeriod { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Returns the username we are connecting with
        /// </summary>
        public string Username => ExecuteInConnection(c => c.Username);

        [Serializable]
        public class NullSSHConnectionException : InvalidOperationException
        {
            public NullSSHConnectionException() { }
            public NullSSHConnectionException(string message) : base(message) { }
            public NullSSHConnectionException(string message, Exception inner) : base(message, inner) { }
            protected NullSSHConnectionException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Create a recovering connection.
        /// </summary>
        /// <param name="makeConnection">Function that makes the underlying connection. Will be called each time the connection breaks.</param>
        public SSHRecoveringConnection(Func<ISSHConnection> makeConnection)
        {
            _makeConnection = makeConnection;
        }

        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }

        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            throw new NotImplementedException();
        }

        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Release our held connection.
        /// </summary>
        public void Dispose()
        {
            _connection?.Dispose();
        }

        /// <summary>
        /// Execute a command with the ability to recover.
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
            return ExecuteInConnection(c => c.ExecuteCommand(command, output, secondsTimeout, refreshTimeout, failNow, dumpOnly, seeAndRespond, waitForCommandReponse));
        }

        /// <summary>
        /// While one of these is around, our recover logic will just not go.
        /// </summary>
        class LockOutRecovery : IDisposable
        {
            private SSHRecoveringConnection _parent;

            public LockOutRecovery(SSHRecoveringConnection parent)
            {
                _parent = parent;
            }
            public void Dispose()
            {
                _parent._lockCount--;
            }
        }

        /// <summary>
        /// If this is non-zero, we won't invoke our recovery policy.
        /// </summary>
        private int _lockCount = 0;

        /// <summary>
        /// Return an object that, as long as it hasn't been disposed of, will prevent this SSHRecoveringConnection from
        /// running its recovery policy.
        /// </summary>
        /// <returns></returns>
        public IDisposable EnterNoRecoverRegion()
        {
            _lockCount++;
            return new LockOutRecovery(this);
        }

        /// <summary>
        /// Tracks the most recently "good" connection we have.
        /// </summary>
        private ISSHConnection _connection = null;

        /// <summary>
        /// Run anything inside a protected envelope that will restart if possible.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execute"></param>
        /// <returns></returns>
        private T ExecuteInConnection<T>(Func<ISSHConnection, T> execute)
        {
            if (_lockCount > 0)
            {
                return InternalExecuteInConnection(execute);
            } else
            {
                return Policy
                    .Handle<SshConnectionException>(e => e.Message.Contains("Client not connected"))
                    .Or<SSHConnectionDroppedException>()
                    .WaitAndRetryForever(index => RetryWaitPeriod)
                    .Execute(() =>
                    {
                        return InternalExecuteInConnection(execute);
                    });
            }
        }

        /// <summary>
        /// Code that does the actual work.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execute"></param>
        /// <returns></returns>
        private T InternalExecuteInConnection<T>(Func<ISSHConnection, T> execute)
        {
            if (_connection == null)
            {
                _connection = _makeConnection();
                if (_connection == null)
                {
                    throw new NullSSHConnectionException("Attempted to create a recoverable SSHConnection, but was given a null value!");
                }
            }
            return execute(_connection);
        }
    }
}
