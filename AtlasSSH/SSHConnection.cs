using CredentialManagement;
using Nito.AsyncEx;
using Polly;
using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.CredentialUtils;

namespace AtlasSSH
{
    /// <summary>
    /// Thrown when the SSH connection has dropped for whatever reason (e.g. the client is no longer
    /// connected).
    /// </summary>
    [Serializable]
    public class SSHConnectionDroppedException : Exception
    {
        public SSHConnectionDroppedException() { }
        public SSHConnectionDroppedException(string message) : base(message) { }
        public SSHConnectionDroppedException(string message, Exception inner) : base(message, inner) { }
        protected SSHConnectionDroppedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown when the ssh command is interupted by the failnow
    /// </summary>
    [Serializable]
    public class SSHCommandInterruptedException : Exception
    {
        public SSHCommandInterruptedException() { }
        public SSHCommandInterruptedException(string message) : base(message) { }
        public SSHCommandInterruptedException(string message, Exception inner) : base(message, inner) { }
        protected SSHCommandInterruptedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }

    [Serializable]
    public class UnableToCreateSSHTunnelException : Exception
    {
        public UnableToCreateSSHTunnelException() { }
        public UnableToCreateSSHTunnelException(string message) : base(message) { }
        public UnableToCreateSSHTunnelException(string message, Exception inner) : base(message, inner) { }
        protected UnableToCreateSSHTunnelException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Thrown when we can't make the initial connection.
    /// </summary>
    [Serializable]
    public class SSHConnectFailureException : Exception
    {
        public SSHConnectFailureException() { }
        public SSHConnectFailureException(string message) : base(message) { }
        public SSHConnectFailureException(string message, Exception inner) : base(message, inner) { }
        protected SSHConnectFailureException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// The low-level connection. Supports very simple SSH and SCP interactions. This represents a single
    /// shell - once you give it comments the shell will "remember" what you've done to the environment.
    /// </summary>
    /// <remarks>
    /// Built on SSH.NET.
    /// 
    /// Passwords are fetched from the windows generic password store.
    /// 
    /// If anything is done to change the command prompt, this class will fail to work properly.
    /// </remarks>
    public sealed class SSHConnection : IDisposable, ISSHConnection
    {
        const int TerminalWidth = 240;

        /// <summary>
        /// Create the connection. The connection is not actually made until it is actually requested.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="userName"></param>
        public SSHConnection(string host, string userName)
        {
            UserName = userName;
            MachineName = host;

            // Fetch the username and password.
            string password = FetchUserCredentials(host, userName);
            if (password == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, userName));
            }

            // Create the connection, but do it lazy so we don't do anything if we aren't used.
            _client = new Lazy<SshClient>(() =>
            {
                var c = new SshClient(host, userName, password);
                try
                {
                    ConnectSSHRobustly(c);
                    return c;
                }
                catch (Exception e)
                {
                    c.Dispose();
                    throw new SSHConnectFailureException($"Unable to make the connection to {host}.", e);
                }
            });

            // Create the scp client for copying things
            _scp = new Lazy<ScpClient>(() =>
            {
                var c = new ScpClient(host, userName, password);
                try
                {
                    ConnectSSHRobustly(c);
                    c.ErrorOccurred += (sender, error) => _scpError = error.Exception;
                    return c;
                } catch
                {
                    c.Dispose();
                    throw;
                }
            });

            // And create a shell stream. Initialize to find the prompt so we can figure out, later, when
            // a task has finished.
            _shell = new AsyncLazy<ShellStream>(async () =>
            {
                var s = _client.Value.CreateShellStream("Commands", TerminalWidth, 200, 132, 80, 240 * 200);
                try
                {
                    _prompt = await ExtractPromptText(s, _client.Value);
                }
                catch
                {
                    s.Dispose();
                    throw;
                }
                return s;
            }, AsyncLazyFlags.RetryOnFailure);
        }

        /// <summary>
        /// Robustly make the connection to an ssh endpoint. This includes retries and the like.
        /// </summary>
        /// <param name="c"></param>
        private static void ConnectSSHRobustly(BaseClient c)
        {
            c.KeepAliveInterval = TimeSpan.FromSeconds(15);
            Policy
                .Handle<SshOperationTimeoutException>()
                .Or<SshConnectionException>()
                .WaitAndRetry(new[] { TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60) }, (exception, timespan, count, ctx) => Trace.WriteLine($"Failed to connect to {c.ConnectionInfo.Host} ({exception.Message}) - retry count {count}"))
                .Execute(() => c.Connect());
        }

        /// <summary>
        /// Waits until there is some sort of prompt text, and extracts it.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private static async Task<string> ExtractPromptText(ShellStream s, SshClient c)
        {
            await s.WaitTillPromptText();
            s.WriteLine("# this initialization");
            await DumpTillFind(s, c, "# this initialization");
            var prompt = await s.ReadRemainingText(1000);
            if (prompt == null || prompt.Length == 0)
            {
                throw new LinuxCommandErrorException($"Did not find the shell prompt! I will not be able to properly interact with this shell! ({c.ConnectionInfo.Host})");
            }
            Trace.WriteLine("Initialization: prompt=" + prompt, "SSHConnection");
            if (prompt.Contains("\n"))
            {
                Trace.WriteLine("Prmopt contains a \\n character: '{prompt}'");
                throw new InvalidOperationException("Prmopt contains a \\n character: '{prompt}'");
            }
            return prompt;
        }

        /// <summary>
        /// Record any errors during the download
        /// </summary>
        private Exception _scpError = null;

        /// <summary>
        /// Hold onto the client. By referencing it, a connection will be made.
        /// </summary>
        private Lazy<SshClient> _client;

        /// <summary>
        /// Hold onto a shell stream. By referencing it the first time it will be created.
        /// </summary>
        private AsyncLazy<ShellStream> _shell;

        private Lazy<ScpClient> _scp;

        /// <summary>
        /// What we've determined is the command prompt. We have to use it b.c. we want to make sure not to get
        /// lost as to what command is being worked on.
        /// </summary>
        private string _prompt;

        public string UserName { get; private set; }

        public string MachineName { get; private set; }

        /// <summary>
        /// Since we are connected, we assume we are always there.
        /// </summary>
        public bool GloballyVisible => true;

        /// <summary>
        /// Dump the input until we see a particular string in the returning text.
        /// </summary>
        /// <param name="s">The ssh stream to run against</param>
        /// <param name="matchText">The text to match - command is done and we return when we see it</param>
        /// <param name="failNow">Function that returns true if we should throw out right away</param>
        /// <param name="ongo">When we see a complete line, call this function. Defaults to null</param>
        /// <param name="secondsTimeout">How long should there be before we have a timeout.</param>
        /// <param name="refreshTimeout">If we see something back from the host, reset the timeout counter</param>
        /// <param name="crlfExpectedAtEnd">If the crlf is expected, then eat it when we see it. A command line prompt, for example, will not have this.</param>
        /// <param name="seeAndRespond">Dictionary of strings to look for and to respond to with further input.</param>
        private static async Task DumpTillFind(ShellStream s,
            SshClient client,
            string matchText, 
            Action<string> ongo = null, 
            int secondsTimeout = 60*60, 
            bool refreshTimeout = false,
            Func<bool> failNow = null,
            bool crlfExpectedAtEnd = false,
            Dictionary<string, string> seeAndRespond = null
            )
        {
            var lb = new LineBuffer();

            // Supress reporting back of the text we are going to "match" in.
            if (ongo != null)
            {
                lb.AddAction(l => { if (!l.Contains(matchText)) { ongo(l); } });
            }

            // We get a match when we see everything in the input buffer
            bool gotmatch = false;
            lb.AddAction(l => gotmatch = gotmatch || l.Contains(matchText));

            // Build the actions that will occur when we see various patterns in the text.
            var expectedMatchText = matchText + (crlfExpectedAtEnd ? LineBuffer.CrLf : "");
            var matchTextAction = new ExpectAction(expectedMatchText, l => { Trace.WriteLine($"DumpTillFill: Found expected Text: '{SummarizeText(l)}'"); lb.Add(l); gotmatch = true; });
            Trace.WriteLine($"DumpTillFind: searching for text: {matchText} (with crlf: {crlfExpectedAtEnd})");

            var expect_actions = (new ExpectAction[] { matchTextAction })
                .Concat(seeAndRespond == null
                        ? Enumerable.Empty<ExpectAction>()
                        : seeAndRespond
                            .Select(sr => new ExpectAction(sr.Key, whatMatched => { Trace.WriteLine($"DumpTillFill: Found seeAndRespond: {whatMatched}"); lb.Add(whatMatched); s.WriteLine(sr.Value); }))
                       )
                       .ToArray();
            if (seeAndRespond != null)
            {
                foreach (var item in seeAndRespond)
                {
                    Trace.WriteLine($"DumpTillFind:  -> also looking for '{item.Key}' and will respond with '{item.Value}'");
                }
            }

            // Run until we hit a timeout. Timeout is finegraned so that we can
            // deal with the sometimes very slow GRID.
            var timeout = DateTime.Now + TimeSpan.FromSeconds(secondsTimeout);
            var streamDataLength = new ValueHasChanged<long>(() => s.Length);
            streamDataLength.Evaluate();

            while (timeout > DateTime.Now)
            {
                // Make sure the connection hasn't dropped. We won't get an exception later on if that hasn't happened.
                if (!client.IsConnected)
                {
                    throw new SSHConnectionDroppedException($"Connection to {client.ConnectionInfo.Host} has been dropped.");
                }

                // Wait for some sort of interesting text to come back.
                await Task.Factory.FromAsync(
                    s.BeginExpect(TimeSpan.FromMilliseconds(100), null, null, expect_actions),
                    s.EndExpect);

                // If a crlf is not expected, then if we have a match we are done.
                if (!crlfExpectedAtEnd)
                {
                    gotmatch = gotmatch || lb.Match(matchText);
                }
                if (gotmatch)
                    break;

                // Reset the timeout if data has come in and we are doing that.
                if (streamDataLength.HasChanged)
                {
                    if (refreshTimeout)
                    {
                        timeout = DateTime.Now + TimeSpan.FromSeconds(secondsTimeout);
                    }

                    var data = s.ReadLine(TimeSpan.FromTicks(1));
                    if (data != null && data.Length > 0)
                    {
                        // Archive the line
                        //Trace.WriteLine($"DumpTillFind: Read text: {data}");
                        lb.Add(data + LineBuffer.CrLf);
                    }
                }

                // Check to see if we should fail
                if (failNow != null && failNow())
                {
                    throw new SSHCommandInterruptedException("Calling routine requested termination of command");
                }
            }
            var tmpString = lb.ToString();
            lb.DumpRest();
            if (!gotmatch)
            {
                Debug.WriteLine($"Waiting for '{matchText}' back from host and it was not seen inside of {secondsTimeout} seconds. Remaining in buffer: '{tmpString}'");
                throw new TimeoutException($"Waiting for '{matchText}' back from host and it was not seen inside of {secondsTimeout} seconds. Remaining in buffer: '{tmpString}'");
            }
        }

        /// <summary>
        /// Generate a summarization version of the text...
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string SummarizeText(string str)
        {
            return str.Length > 50
                ? str.Substring(0, 25) + ".." + str.Substring(str.Length - 25)
                : str;
        }

        /// <summary>
        /// Execute a single command syncrohonsly. Output from that command will be sent back via the output call-back.
        /// action.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="failNow">If this ever returns true, fail as fast as possible.</param>
        /// <param name="dumpOnly">If true, print to the Trace machnism all commands, but do not execute them.</param>
        /// <param name="refreshTimeout">If true, reset the the timeout counter every time some new input shows up.</param>
        /// <param name="WaitForCommandResult">If false don't attempt to get the output from the command.</param>
        /// <returns></returns>
        public ISSHConnection ExecuteCommand(string command,
            Action<string> output = null,
            int secondsTimeout = 60 * 60, bool refreshTimeout = false,
            Func<bool> failNow = null,
            bool dumpOnly = false,
            Dictionary<string, string> seeAndRespond = null,
            bool WaitForCommandResult = true)
        {
            return ExecuteCommandAsync(command, output, secondsTimeout, refreshTimeout, failNow, dumpOnly, seeAndRespond, WaitForCommandResult).Result;
        }

        /// <summary>
        /// Execute a single command. Output from that command will be sent back via the output
        /// action.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="failNow">If this ever returns true, fail as fast as possible.</param>
        /// <param name="dumpOnly">If true, print to the Trace machnism all commands, but do not execute them.</param>
        /// <param name="refreshTimeout">If true, reset the the timeout counter every time some new input shows up.</param>
        /// <param name="WaitForCommandResult">If false don't attempt to get the output from the command.</param>
        /// <returns></returns>
        public async Task<ISSHConnection> ExecuteCommandAsync(string command, 
            Action<string> output = null,
            int secondsTimeout = 60*60, bool refreshTimeout = false,
            Func<bool> failNow = null,
            bool dumpOnly = false,
            Dictionary<string, string> seeAndRespond = null,
            bool WaitForCommandResult = true)
        {
            Trace.WriteLine("ExecuteCommand: " + command, "SSHConnection");
            if (!dumpOnly)
            {
                var buf = new CircularStringBuffer(1024);
                try
                {
                    (await _shell).WriteLine(command);
                    await DumpTillFind((await _shell), _client.Value,
                        command.Substring(0, Math.Min(TerminalWidth - 30, command.Length)),
                        ongo: s => buf.Add(s),
                        crlfExpectedAtEnd: true, secondsTimeout: 20, failNow: failNow); // The command is (normally) repeated back to us...
                    if (WaitForCommandResult)
                    {
                        await DumpTillFind((await _shell), _client.Value, _prompt, s => {
                            buf.Add(s);
                            output?.Invoke(s);
                        },
                            secondsTimeout: secondsTimeout, refreshTimeout: refreshTimeout, failNow: failNow, seeAndRespond: seeAndRespond);
                    }
                } catch (TimeoutException e)
                {
                    throw new TimeoutException($"{e.Message} - occured while executing command {command}. Last text we saw was '{buf.ToString()}'", e);
                }
            }
            return this;
        }

        /// <summary>
        /// Track the context for the ssh sub-shell we are in.
        /// </summary>
        private class SSHSubShellContext : IDisposable
        {
            private SSHConnection _connection;
            private string _oldPrompt;

            /// <summary>
            /// Initi with context info
            /// </summary>
            /// <param name="prompt"></param>
            /// <param name="sSHConnection"></param>
            public SSHSubShellContext(string prompt, SSHConnection sSHConnection)
            {
                this._oldPrompt = prompt;
                this._connection = sSHConnection;
            }

            /// <summary>
            /// Restore everything
            /// </summary>
            public void Dispose()
            {
                // Put the prompt back first, so that we come back properly.
                _connection._prompt = _oldPrompt;
                _connection.ExecuteCommand("exit");
            }
        }

        public IDisposable SSHTo (string host, string userName)
        {
            return SSHToAsync(host, userName).Result;
        }

        /// <summary>
        /// ssh to another machine. This implies we have to deal with a new prompt.
        /// </summary>
        /// <param name="host">host of remote machine to ssh to</param>
        /// <param name="userName">username to use when we ssh there</param>
        /// <returns>A context item. Dispose and it will exit the remote shell. If you let it garbage collect you might be caught by unexpected behavior!!</returns>
        public async Task<IDisposable> SSHToAsync(string host, string userName)
        {
            // Archive the prompt for later use
            // Since getting the connection runing is lazy, the prompt won't be set
            // unless a command has been issued. So if this is the first command,
            // prompt won't be set yet.
            if (_prompt == null)
            {
                var bogus = await ExecuteCommandAsync("pwd");
            }
            var r = new SSHSubShellContext(_prompt, this);

            // Issue the ssh command... Since this isn't coming back, we have to do it a little differently.
            await ExecuteCommandAsync($"ssh -oStrictHostKeyChecking=no -o TCPKeepAlive=yes -o ServerAliveInterval=15 {userName}@{host}", WaitForCommandResult: false);
            _prompt = null;
            var cmdResult = new StringBuilder();
            while (_prompt == null)
            {
                cmdResult.Append(await (await _shell).WaitTillPromptText());
                var text = (await _shell).Read();
                cmdResult.Append(text);
                if (text.StartsWith("Password"))
                {
                    var password = FetchUserCredentials(host, userName);
                    if (password == null)
                    {
                        throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, userName));
                    }
                    (await _shell).WriteLine(password);
                } else if (!string.IsNullOrWhiteSpace(text))
                {
                    _prompt = text;
                    Trace.WriteLine($"SSHTo: Setting prompt to '{_prompt}'.", "SSHConnection");
                }
            }

            // Now make sure we successfully went down a step to look at what was going on.
            string shellStatus = "";
            try
            {
                await ExecuteCommandAsync("echo $?", l => shellStatus = l, secondsTimeout: 30);
            }
            catch (Exception e)
            {
                shellStatus = e.Message;
            }
            if (shellStatus != "0")
            {
                // The whole connection is borked. We have to dump this connection totally.
                throw new UnableToCreateSSHTunnelException($"Unable to create SSH tunnel to {userName}@{host} (ssh command returned {shellStatus}). Error text from the command: {cmdResult.ToString()}");
            }

            // Return the old context so we can restore ourselves when we exit the thing.
            return r;
        }

        /// <summary>
        /// Syncronously copy a remote directory to a local file.
        /// </summary>
        /// <param name="remotedir"></param>
        /// <param name="localDir"></param>
        /// <param name="statusUpdate"></param>
        /// <returns></returns>
        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            return CopyRemoteDirectoryLocallyAsync(remotedir, localDir, statusUpdate).Result;
        }

        /// <summary>
        /// Asyncronously Copy a remote directory locally
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="remotedir"></param>
        /// <param name="localDir"></param>
        /// <returns></returns>
        public async Task<ISSHConnection> CopyRemoteDirectoryLocallyAsync(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            // Status update function
            void updateStatus(object o, Renci.SshNet.Common.ScpDownloadEventArgs args) => statusUpdate(args.Filename);

            // Now run everything.
            _scpError = null;
            if (statusUpdate != null)
            {
                _scp.Value.Downloading += updateStatus;
            }
            try
            {
                await Task.Factory.StartNew(() => _scp.Value.Download(remotedir, localDir));
                if (_scpError != null)
                {
                    throw _scpError;
                }
                return this;
            }
            finally
            {
                if (statusUpdate != null)
                {
                    _scp.Value.Downloading -= updateStatus;
                }
            }
        }

        /// <summary>
        /// Copy a single file locally.
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="ourpath"></param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters", Justification = "Need DirectoryInfo for this to make sense")]
        public ISSHConnection CopyRemoteFileLocally(string lx, DirectoryInfo ourpath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            if (string.IsNullOrWhiteSpace(lx))
            {
                throw new ArgumentNullException("You must specify a linux path");
            }
            if (ourpath == null)
            {
                throw new ArgumentNullException("Must specify ourpath so we know where to copy to!");
            }
            var lxFname = lx.Split('/').Last();
            return CopyRemoteFileLocally(lx, new FileInfo(Path.Combine(ourpath.FullName, lxFname)), statusUpdate, failNow);
        }

        /// <summary>
        /// Syncronously copy a file from a remote location to our local directory at a specific spot.
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="localFile"></param>
        /// <param name="statusUpdate"></param>
        /// <param name="failNow"></param>
        /// <returns></returns>
        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return CopyRemoteFileLocallyAsync(lx, localFile, statusUpdate, failNow).Result;
        }

        /// <summary>
        /// Copy a file from a remote location to our local directory at a speciic spot.
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="localFile"></param>
        /// <param name="statusUpdate"></param>
        /// <returns></returns>
        public async Task<ISSHConnection> CopyRemoteFileLocallyAsync(string lx, FileInfo localFile, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Reset the global error value to null.
            _scpError = null;

            // Update, but only when something interesting changes.
            string oldMessage = "";
            void updateStatus(object o, Renci.SshNet.Common.ScpDownloadEventArgs args)
            {
                if (args.Filename != oldMessage)
                {
                    statusUpdate($"Copying {args.Filename} via SCP");
                    oldMessage = args.Filename;
                }
            }

            if (statusUpdate != null)
            {
                _scp.Value.Downloading += updateStatus;
            }

            // Do the download, but protect ourselves against exceptions,
            // which always happen with the network.
            try
            {
                var lxFname = lx.Split('/').Last();
                await Task.Factory.StartNew(() => _scp.Value.Download(lx, localFile));
                if (_scpError != null)
                {
                    throw _scpError;
                }
                return this;
            }
            finally
            {
                if (statusUpdate != null)
                {
                    _scp.Value.Downloading -= updateStatus;
                }
            }
        }

        /// <summary>
        /// Copy a local file to a remote location synchronosously.
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="linuxPath"></param>
        /// <param name="statusUpdate"></param>
        /// <param name="failNow"></param>
        /// <returns></returns>
        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return CopyLocalFileRemotelyAsync(localFile, linuxPath, statusUpdate, failNow).Result;
        }

        /// <summary>
        /// Copy a file from the local directory up into the cloud
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="linuxPath"></param>
        public async Task<ISSHConnection> CopyLocalFileRemotelyAsync(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Rest the global error catcher.
            _scpError = null;

            // Send a message only when the message changes.
            string oldMessage = "";
            void updateStatus(object o, Renci.SshNet.Common.ScpUploadEventArgs args)
            {
                if (args.Filename != oldMessage)
                {
                    statusUpdate(args.Filename);
                    oldMessage = args.Filename;
                }
            }

            if (statusUpdate != null)
            {
                _scp.Value.Uploading += updateStatus;
            }

            // Run the download, pretected against the network breaking.
            try
            {
                await Task.Factory.StartNew(() => _scp.Value.Upload(localFile, linuxPath));
                if (_scpError != null)
                {
                    throw _scpError;
                }
                return this;
            }
            finally
            {
                if (statusUpdate != null)
                {
                    _scp.Value.Uploading -= updateStatus;
                }
            }
        }

        /// <summary>
        /// We make sure to shut down everything attached to us
        /// </summary>
        public void Dispose()
        {
            if (_shell.IsStarted)
            {
                DisposeShell().Wait();
            }

            if (_scp.IsValueCreated)
            {
                _scp.Value.Dispose();
            }

            if (_client.IsValueCreated)
            {
                _client.Value.Dispose();
            }
        }

        /// <summary>
        /// Helper function to dispose of the shell variable.
        /// </summary>
        /// <returns></returns>
        private async Task DisposeShell()
        {
            (await _shell).Dispose();
        }
    }
}
