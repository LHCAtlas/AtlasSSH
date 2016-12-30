using CredentialManagement;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            _shell = new Lazy<ShellStream>(() => {
                var s = _client.Value.CreateShellStream("Commands", TerminalWidth, 200, 132, 80, 240 * 200);
                s.WaitTillPromptText();
                s.WriteLine("# this initialization");
                DumpTillFind(s, "# this initialization");
                //s.ReadLine(); // Get past the "new-line" that is at the end of the printed comment above
                _prompt = s.ReadRemainingText(1000);
                if (_prompt == null || _prompt.Length == 0)
                {
                    throw new LinuxCommandErrorException("Did not find the shell prompt! I will not be able to properly interact with this shell!");
                }
                Trace.WriteLine("Initialization: prompt=" + _prompt, "SSHConnection");
                return s;
            });
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
        private void DumpTillFind(ShellStream s, string matchText, 
            Action<string> ongo = null, 
            int secondsTimeout = 60*60, 
            bool refreshTimeout = false,
            Func<bool> failNow = null,
            bool crlfExpectedAtEnd = false
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

            // Run until we hit a timeout. Timeout is finegraned so that we can
            // deal with the sometimes very slow GRID.
            var timeout = DateTime.Now + TimeSpan.FromSeconds(secondsTimeout);
            var expectedMatchText = matchText + (crlfExpectedAtEnd ? LineBuffer.CrLf : "");
            while (timeout > DateTime.Now)
            {
                s.Expect(TimeSpan.FromMilliseconds(100), new ExpectAction(expectedMatchText, l => { lb.Add(l); gotmatch = true; }));
                if (!crlfExpectedAtEnd)
                {
                    gotmatch = gotmatch || lb.Match(matchText);
                }
                if (gotmatch)
                    break;

                var data = s.ReadLine(TimeSpan.FromSeconds(1));
                if (data != null && data.Length > 0)
                {
                    // Archive the line
                    lb.Add(data + LineBuffer.CrLf);

                    // We got something real back - perhaps refresh it?
                    if (refreshTimeout)
                    {
                        timeout = DateTime.Now + TimeSpan.FromSeconds(secondsTimeout);
                    }
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
        /// Execute a single command. Output from that command will be sent back via the output
        /// action.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="failNow">If this ever returns true, fail as fast as possible.</param>
        /// <returns></returns>
        public ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 60*60, bool refreshTimeout = false, Func<bool> failNow = null, bool dumpOnly = false)
        {
            Trace.WriteLine("ExecuteCommand: " + command, "SSHConnection");
            if (!dumpOnly)
            {
                _shell.Value.WriteLine(command);
                DumpTillFind(_shell.Value, command.Substring(0, Math.Min(TerminalWidth - 30, command.Length)), crlfExpectedAtEnd: true, secondsTimeout: 10, failNow: failNow); // The command is (normally) repeated back to us...
                //_shell.Value.ReadLine(TimeSpan.FromSeconds(10)); // Read back the end of line after the command is sent out.
                DumpTillFind(_shell.Value, _prompt, output, secondsTimeout: secondsTimeout, refreshTimeout: refreshTimeout, failNow: failNow);
            }
            return this;
        }

#if false
        // Dead code, but it might be useful sometime.
        /// <summary>
        /// Run a command. Scan the input for text and when we see it, send response strings.
        /// </summary>
        /// <param name="command">The command to start things off</param>
        /// <param name="seeAndRespond">When we see a bit of text (a Regex), then respond with another bit of text.</param>
        /// <param name="output">Called with each output line we read back</param>
        /// <returns></returns>
        public SSHConnection ExecuteCommandWithInput(string command, Dictionary<string, string> seeAndRespond, Action<string> output = null)
        {
            // Run the command
            Trace.WriteLine("ExecuteCommand: " + command, "SSHConnection");
            _shell.Value.WriteLine(command);
            DumpTillFind(_shell.Value, command); // The command is (normally) repeated back to us...
            _shell.Value.ReadLine(); // And the new line at the end of the command.

            // Now, we don't know what order things will come back in, so...
            var actions = seeAndRespond
                .Select(p => new ExpectAction(p.Key, s =>
                {
                    if (output != null)
                        output(s);
                    _shell.Value.WriteLine(p.Value);
                }))
                .ToArray();

            _shell.Value.Expect(TimeSpan.FromSeconds(5), actions);
            DumpTillFind(_shell.Value, _prompt, output);
            return this;
        }
#endif

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
            System.EventHandler<Renci.SshNet.Common.ScpDownloadEventArgs> updateStatus = (o, args) => statusUpdate(args.Filename);
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
