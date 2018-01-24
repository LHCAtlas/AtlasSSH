using Polly;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using static AtlasSSH.SSHConnection;

namespace AtlasSSH
{
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
    /// A connection that can recover from being broken.
    /// </summary>
    /// <remarks>
    /// While each instance is threadsafe from other instances, do not make more than one call at a time from different threads into the same instance.
    /// </remarks>
    public sealed class SSHRecoveringConnection : ISSHConnection
    {
        /// <summary>
        /// The function that will return a connection anytime we need one.
        /// </summary>
        private Func<ISSHConnection> _makeConnection;

        /// <summary>
        /// Make the connection, but async.
        /// </summary>
        private Func<Task<ISSHConnection>> _makeConnectionAsync;

        /// <summary>
        /// How long to wait between attempts. By default it is 10 seconds.
        /// </summary>
        public TimeSpan RetryWaitPeriod { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Returns the username we are connecting with
        /// </summary>
        public string UserName => ExecuteInConnection(c => c.UserName);

        public string MachineName => ExecuteInConnection(c => c.MachineName);

        public bool GloballyVisible => ExecuteInConnection(c => c.GloballyVisible);

        /// <summary>
        /// Create a recovering connection from a function taht builds teh connection.
        /// </summary>
        /// <param name="makeConnection">Function that makes the underlying connection. Will be called each time the connection breaks.</param>
        public SSHRecoveringConnection(Func<Task<ISSHConnection>> makeConnection)
        {
            _makeConnectionAsync = makeConnection;
            _makeConnection = () => _makeConnectionAsync().Result;
        }

        /// <summary>
        /// Given a synchronos connection makeer.
        /// </summary>
        /// <param name="makeConnection"></param>
        public SSHRecoveringConnection(Func<ISSHConnection> makeConnection)
        {
            _makeConnection = makeConnection;
            _makeConnectionAsync = () => Task.Factory.StartNew(() => makeConnection());
        }

        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return ExecuteInConnection(c => c.CopyLocalFileRemotely(localFile, linuxPath, statusUpdate, failNow));
        }
        public Task<ISSHConnection> CopyLocalFileRemotelyAsync(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return ExecuteInConnectionAsync(c => c.CopyLocalFileRemotelyAsync(localFile, linuxPath, statusUpdate, failNow));
        }

        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            return ExecuteInConnection(c => c.CopyRemoteDirectoryLocally(remotedir, localDir, statusUpdate));
        }
        public Task<ISSHConnection> CopyRemoteDirectoryLocallyAsync(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            return ExecuteInConnectionAsync(c => c.CopyRemoteDirectoryLocallyAsync(remotedir, localDir, statusUpdate));
        }

        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return ExecuteInConnection(c => c.CopyRemoteFileLocally(lx, localFile, statusUpdate, failNow));
        }
        public Task<ISSHConnection> CopyRemoteFileLocallyAsync(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return ExecuteInConnectionAsync(c => c.CopyRemoteFileLocallyAsync(lx, localFile, statusUpdate, failNow));
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

        public Task<ISSHConnection> ExecuteCommandAsync(string command, Action<string> output = null, int secondsTimeout = 3600, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool waitForCommandReponse = true)
        {
            return ExecuteInConnectionAsync(c => c.ExecuteCommandAsync(command, output, secondsTimeout, refreshTimeout, failNow, dumpOnly, seeAndRespond, waitForCommandReponse));
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
            }
            else
            {
                return Policy
                    .Handle<SshConnectionException>(e => e.Message.Contains("Client not connected"))
                    .Or<SSHConnectionDroppedException>()
                    .Or<TimeoutException>(e => e.Message.Contains("back from host"))
                    .WaitAndRetryForever(index => RetryWaitPeriod, (except, cnt) => { Trace.WriteLine($"Failed In Connection to {_connection.MachineName}: {except.Message}");  _connection.Dispose(); _connection = null; })
                    .Execute(() =>
                    {
                        return InternalExecuteInConnection(execute);
                    });
            }
        }

        /// <summary>
        /// Execute the command, asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execute"></param>
        /// <returns></returns>
        private async Task<T> ExecuteInConnectionAsync<T>(Func<ISSHConnection, Task<T>> execute)
        {
            if (_lockCount > 0)
            {
                return await InternalExecuteInConnectionAsync(execute);
            }
            else
            {
                return await Policy
                    .Handle<SshConnectionException>(e => e.Message.Contains("Client not connected"))
                    .Or<SSHConnectionDroppedException>()
                    .Or<TimeoutException>(e => e.Message.Contains("back from host"))
                    .WaitAndRetryForeverAsync(index => RetryWaitPeriod, (except, cnt) => { Trace.WriteLine($"Failed In Connection to {_connection.MachineName}: {except.Message}"); _connection.Dispose(); _connection = null; })
                    .ExecuteAsync(async () =>
                    {
                        return await InternalExecuteInConnectionAsync(execute);
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

        /// <summary>
        /// Run an command function as a future.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="execute"></param>
        /// <returns></returns>
        private async Task<T> InternalExecuteInConnectionAsync<T>(Func<ISSHConnection, Task<T>> execute)
        {
            if (_connection == null)
            {
                _connection = await _makeConnectionAsync();
                if (_connection == null)
                {
                    throw new NullSSHConnectionException("Attempted to create a recoverable SSHConnection, but was given a null value!");
                }
            }
            return await execute(_connection);
        }
    }
}
