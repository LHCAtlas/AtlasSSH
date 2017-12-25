using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.SSHRecoveringConnection;
using System.IO;
using Renci.SshNet.Common;
using static AtlasSSH.SSHConnection;

namespace AtlasSSHTest
{
    [TestClass]
    public class SSHRecoveringConnectionTest
    {
        [TestInitialize]
        public void TestInit()
        {
            dummyConnection.Reset();
        }
        [TestCleanup]
        public void TestCleanup()
        {
            dummyConnection.Reset();
        }

        [TestMethod]
        public void SSHRecoverUsername()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection()))
            {
                Assert.AreEqual("myname", c.Username);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(NullSSHConnectionException))]
        public void SSHRecoverNullConnection()
        {
            using (var c = new SSHRecoveringConnection(() => (ISSHConnection) null))
            {
                c.ExecuteLinuxCommand("ls");
            }
        }

        [TestMethod]
        public void SSHRecoverRegularConnection()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection()))
            {
                c.ExecuteLinuxCommand("ls");
                Assert.AreEqual(2, dummyConnection.CalledExecuteLinuxCommand);
            }
        }

        [TestMethod]
        public void SSHRecoverFailsConnectionSshConnectionException()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection(failExecuteCommandForNTimes: 1, genException: () => new SshConnectionException("Client not connected."))))
            {
                c.RetryWaitPeriod = TimeSpan.FromMilliseconds(5);
                c.ExecuteLinuxCommand("ls");
                Assert.AreEqual(3, dummyConnection.CalledExecuteLinuxCommand);
            }
        }

        [TestMethod]
        public void SSHRecoverFailsConnectionSSHConnectionDroppedException()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection(failExecuteCommandForNTimes: 1, genException: () => new SSHConnectionDroppedException())))
            {
                c.RetryWaitPeriod = TimeSpan.FromMilliseconds(5);
                c.ExecuteLinuxCommand("ls");
                Assert.AreEqual(3, dummyConnection.CalledExecuteLinuxCommand);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(SshConnectionException))]
        public void SSHRecoverNoRecover()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection(failExecuteCommandForNTimes: 1)))
            {
                c.RetryWaitPeriod = TimeSpan.FromMilliseconds(1);
                using (var blocker = c.EnterNoRecoverRegion())
                {
                    c.ExecuteLinuxCommand("ls");
                }
            }
        }

        [TestMethod]
        public void SSHRecoverNoRecoverAndRecover()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection(failExecuteCommandForNTimes: 1)))
            {
                using (var blocker = c.EnterNoRecoverRegion())
                {
                    c.RetryWaitPeriod = TimeSpan.FromMilliseconds(5);
                    try
                    {
                        c.ExecuteLinuxCommand("ls");
                    } catch { }
                    c.ExecuteCommand("ls");
                }
            }
        }

        [TestMethod]
        public void SSHRecoverMakeSureCommandRetriedOnException()
        {
            using (var c = new SSHRecoveringConnection(() => new dummyConnection(failExecuteCommandForNTimes: 1)))
            {
                c.RetryWaitPeriod = TimeSpan.FromMilliseconds(5);
                try
                {
                    c.ExecuteLinuxCommand("ls");
                }
                catch { }
                Assert.AreEqual(2, dummyConnection.ListOfCommands.Count());
                Assert.AreEqual("ls", dummyConnection.ListOfCommands[0]);
            }
        }

        /// <summary>
        /// A dummy connection to simulate errors (or not)
        /// </summary>
        class dummyConnection : ISSHConnection
        {
            public static void Reset()
            {
                CalledExecuteLinuxCommand = 0;
                ListOfCommands.Clear();
            }

            /// <summary>
            /// List of commands
            /// </summary>
            public static List<string> ListOfCommands = new List<string>();

            /// <summary>
            /// How many times have we been called?
            /// </summary>
            public static int CalledExecuteLinuxCommand = 0;

            /// <summary>
            /// As long as this is larger than 1, we will throw a SSHException when we are asked to do anything.
            /// </summary>
            private int _failExecuteCommandForNTimes;
            private Func<Exception> _genException;

            private Dictionary<string, string> _responses;

            /// <summary>
            /// Create ourselves.
            /// </summary>
            /// <param name="failExecuteCommandForNTimes"></param>
            public dummyConnection(int failExecuteCommandForNTimes = 0, Func<Exception> genException = null, IDictionary<string, string> responses = null)
            {
                this._failExecuteCommandForNTimes = failExecuteCommandForNTimes;
                if (genException == null)
                {
                    genException = () => new SshConnectionException("Client not connected.");
                }
                this._genException = genException;

                _responses = responses == null ? new Dictionary<string, string>() : new Dictionary<string, string>(responses);
                if (!_responses.ContainsKey("ls"))
                    _responses["ls"] = "junk.txt";
                if (!_responses.ContainsKey("echo $?"))
                    _responses["echo $?"] = "0";
            }

            /// <summary>
            /// Return the username.
            /// </summary>
            public string Username => "myname";

            public string MachineName => throw new NotImplementedException();

            public bool GloballyVisible => throw new NotImplementedException();

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

            public void Dispose()
            {
            }

            /// <summary>
            /// Run a command.
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
                CalledExecuteLinuxCommand++;
                if (_failExecuteCommandForNTimes > 0)
                {
                    _failExecuteCommandForNTimes--;
                    throw _genException();
                }
                if (!_responses.Keys.Contains(command))
                {
                    throw new ArgumentException($"Dictionary doesn't have command '{command}'.");
                }
                output?.Invoke(_responses[command]);
                ListOfCommands.Add(command);
                return this;
            }

            public Task<ISSHConnection> ExecuteCommandAsync(string command, Action<string> output = null, int secondsTimeout = 3600, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false, Dictionary<string, string> seeAndRespond = null, bool waitForCommandReponse = true)
            {
                throw new NotImplementedException();
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
}
