using CredentialManagement;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AtlasSSH
{
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
    public class SSHConnection : IDisposable, ISSHConnection
    {
        const int TerminalWidth = 240;


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

        public SSHConnection(string host, string username)
        {
            // Fetch the username and password.
            var sclist = new CredentialSet(host);
            var passwordInfo = sclist.Load().Where(c => c.Username == username).FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, username));
            }

            // Create the connection, but do it lazy so we don't do anything if we aren't used.
            _client = new Lazy<SshClient>(() => {
                var c = new SshClient(host, username, passwordInfo.Password);
                c.Connect();
                return c;
            });

            // Create the scp client for copying things
            _scp = new Lazy<ScpClient>(() =>
            {
                var c = new ScpClient(host, username, passwordInfo.Password);
                c.Connect();
                c.ErrorOccurred += (sender, error) => _scpError = error.Exception;
                return c;
            });

            // And create a shell stream. Initialize to find the prompt so we can figure out, later, when
            // a task has finished.
            _shell = new Lazy<ShellStream>(() =>
            {
                var s = _client.Value.CreateShellStream("Commands", TerminalWidth, 200, 132, 80, 240 * 200);
                _prompt = ExtractPromptText(s);
                return s;
            });
        }

        /// <summary>
        /// Waits until there is some sort of prompt text, and extracts it.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string ExtractPromptText(ShellStream s)
        {
            s.WaitTillPromptText();
            s.WriteLine("# this initialization");
            DumpTillFind(s, "# this initialization");
            var prompt = s.ReadRemainingText(1000);
            if (prompt == null || prompt.Length == 0)
            {
                throw new LinuxCommandErrorException("Did not find the shell prompt! I will not be able to properly interact with this shell!");
            }
            Trace.WriteLine("Initialization: prompt=" + prompt, "SSHConnection");
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
        private Lazy<ShellStream> _shell;

        private Lazy<ScpClient> _scp;

        /// <summary>
        /// What we've determined is the command prompt. We have to use it b.c. we want to make sure not to get
        /// lost as to what command is being worked on.
        /// </summary>
        private string _prompt;

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
        private void DumpTillFind(ShellStream s,
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
                            .Select(sr => new ExpectAction(sr.Key, whatMatched => { Trace.WriteLine($"DumpTillFill: Found seeAndRespond: {whatMatched}"); lb.Add(whatMatched); _shell.Value.WriteLine(sr.Value); }))
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
                s.Expect(TimeSpan.FromMilliseconds(100), expect_actions);
                if (!crlfExpectedAtEnd)
                {
                    gotmatch = gotmatch || lb.Match(matchText);
                }
                if (gotmatch)
                    break;

                // Reset the timeout if data has come in and we are doing that.
                if (refreshTimeout && streamDataLength.HasChanged)
                {
                    timeout = DateTime.Now + TimeSpan.FromSeconds(secondsTimeout);
                }

                var data = s.ReadLine(TimeSpan.FromSeconds(1));
                if (data != null && data.Length > 0)
                {
                    // Archive the line
                    Trace.WriteLine($"DumpTillFind: Read text: {data}");
                    lb.Add(data + LineBuffer.CrLf);
                }

                if (failNow != null && failNow())
                {
                    throw new SSHCommandInterruptedException("Calling routine requested termination of command");
                }
            }
            if (!gotmatch)
            {
                throw new TimeoutException(string.Format("Waiting for '{0}' back from host and it was not seen inside of {1} seconds.", matchText, secondsTimeout));
            }
            lb.DumpRest();
        }

        /// <summary>
        /// Generate a summarization version of the text...
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private string SummarizeText(string str)
        {
            return str.Length > 50
                ? str.Substring(0, 25) + ".." + str.Substring(str.Length - 25)
                : str;
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
        public ISSHConnection ExecuteCommand(string command, 
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
                _shell.Value.WriteLine(command);
                DumpTillFind(_shell.Value, command.Substring(0, Math.Min(TerminalWidth - 30, command.Length)), crlfExpectedAtEnd: true, secondsTimeout: 10, failNow: failNow); // The command is (normally) repeated back to us...
                if (WaitForCommandResult)
                {
                    DumpTillFind(_shell.Value, _prompt, output, secondsTimeout: secondsTimeout, refreshTimeout: refreshTimeout, failNow: failNow, seeAndRespond: seeAndRespond);
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

        /// <summary>
        /// Ssh to another machine. This implies we have to deal with a new prompt.
        /// </summary>
        /// <param name="host">host of remote machine to ssh to</param>
        /// <param name="username">username to use when we ssh there</param>
        /// <returns>A context item. Dispose and it will exit the remote shell. If you let it garbage collect you might be caught by unexpected behavior!!</returns>
        public IDisposable SSHTo(string host, string username)
        {
            // Archive the prompt for later use
            // Since getting the connection runing is lazy, the prompt won't be set
            // unless a command has been issued. So if this is the first command,
            // prompt won't be set yet.
            if (_prompt == null)
            {
                var bogus = ExecuteCommand("pwd");
            }
            var r = new SSHSubShellContext(_prompt, this);

            // Issue the ssh command... Since this isn't coming back, we have to do it a little differently.
            ExecuteCommand($"ssh -oStrictHostKeyChecking=no {username}@{host}", WaitForCommandResult: false);
            _prompt = null;
            while (_prompt == null)
            {
                _shell.Value.WaitTillPromptText();
                var text = _shell.Value.Read();
                if (text.StartsWith("Password"))
                {
                    var sclist = new CredentialSet(host);
                    var passwordInfo = sclist.Load().Where(c => c.Username == username).FirstOrDefault();
                    if (passwordInfo == null)
                    {
                        throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, username));
                    }
                    _shell.Value.WriteLine(passwordInfo.Password);
                } else if (!string.IsNullOrWhiteSpace(text))
                {
                    _prompt = text;
                    Trace.WriteLine($"SSHTo: Setting prompt to '{_prompt}'.");
                }
            }

            // Return the old context so we can restore ourselves when we exit the thing.
            return r;
        }

        /// <summary>
        /// Copy a remote directory locally
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="remotedir"></param>
        /// <param name="localDir"></param>
        /// <returns></returns>
        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            _scpError = null;
            EventHandler<Renci.SshNet.Common.ScpDownloadEventArgs> updateStatus = (o, args) => statusUpdate(args.Filename);
            if (statusUpdate != null)
            {
                _scp.Value.Downloading += updateStatus;
            }
            try
            {
                _scp.Value.Download(remotedir, localDir);
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
        public ISSHConnection CopyRemoteFileLocally(string lx, DirectoryInfo ourpath, Action<string> statusUpdate = null)
        {
            var lxFname = lx.Split('/').Last();
            return CopyRemoteFileLocally(lx, new FileInfo(Path.Combine(ourpath.FullName, lxFname)), statusUpdate);
        }

        /// <summary>
        /// Copy a file from a remote location to our local directory at a speciic spot.
        /// </summary>
        /// <param name="lx"></param>
        /// <param name="localFile"></param>
        /// <param name="statusUpdate"></param>
        /// <returns></returns>
        public ISSHConnection CopyRemoteFileLocally(string lx, FileInfo localFile, Action<string> statusUpdate = null)
        {
            _scpError = null;
            EventHandler<Renci.SshNet.Common.ScpDownloadEventArgs> updateStatus = (o, args) => statusUpdate(args.Filename);
            if (statusUpdate != null)
            {
                _scp.Value.Downloading += updateStatus;
            }
            try
            {
                var lxFname = lx.Split('/').Last();
                _scp.Value.Download(lx, localFile);
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
        /// Copy a file from the local directory up into the cloud
        /// </summary>
        /// <param name="localFile"></param>
        /// <param name="linuxPath"></param>
        public ISSHConnection CopyLocalFileRemotely(FileInfo localFile, string linuxPath, Action<string> statusUpdate = null)
        {
            _scpError = null;
            EventHandler<Renci.SshNet.Common.ScpUploadEventArgs> updateStatus = (o, args) => statusUpdate(args.Filename);
            if (statusUpdate != null)
            {
                _scp.Value.Uploading += updateStatus;
            }
            try
            {
                _scp.Value.Upload(localFile, linuxPath);
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
            if (_shell.IsValueCreated)
            {
                _shell.Value.Dispose();
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
    }
}
