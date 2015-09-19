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
    public class SSHConnection
    {
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
                var s = _client.Value.CreateShellStream("Commands", 240, 200, 132, 80, 240*200);
                s.WaitTillPromptText();
                s.WriteLine("# this initialization");
                DumpTillFind(s, "# this initialization");
                s.ReadLine(); // Get past the "new-line" that is at the end of the printed comment above
                _prompt = s.ReadRemainingText(100);
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

        private class LineBuffer
        {
            public LineBuffer(Action<string> actionOnLine)
            {
                _actinoOnLine = actionOnLine;
            }

            string _text = "";
            private Action<string> _actinoOnLine;

            public void Add(string line)
            {
                _text += line;
                Flush();
            }

            private void Flush()
            {
                while (true)
                {
                    var lend = _text.IndexOf("\r\n");
                    if (lend < 0)
                        break;

                    var line = _text.Substring(0, lend);
                    ActOnLine(line);
                    _text = _text.Substring(lend + 2);
                }
            }

            public void DumpRest()
            {
                Flush();
                ActOnLine(_text);
            }

            List<string> stringsToSuppress = new List<string>();

            /// <summary>
            /// Any line containing these strings will not be printed out
            /// </summary>
            /// <param name="whenLineContains"></param>
            public void Suppress(string whenLineContains)
            {
                stringsToSuppress.Add(whenLineContains);
            }

            /// <summary>
            /// Dump out a line, safely.
            /// </summary>
            /// <param name="line"></param>
            private void ActOnLine(string line)
            {
                Trace.WriteLine("ReturnedLine: " + line, "SSHConnection");
                if (_actinoOnLine == null)
                    return;
                if (!stringsToSuppress.Any(s => line.Contains(s)))
                {
                    _actinoOnLine(line);
                }
            }
        }

        /// <summary>
        /// Dump the input until we see a particular string in the returning text.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="p"></param>
        private void DumpTillFind(ShellStream s, string matchText, Action<string> ongo = null, bool dontdumplineofmatch = true)
        {
            var lb = new LineBuffer(ongo);
            if (dontdumplineofmatch)
            {
                lb.Suppress(matchText);
            }
            s.Expect(TimeSpan.FromMinutes(60), new ExpectAction(matchText, l => lb.Add(l)));
            lb.DumpRest();
        }

        /// <summary>
        /// Execute a single command. Output from that command will be sent back via the output
        /// action.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <returns></returns>
        public SSHConnection ExecuteCommand(string command, Action<string> output = null)
        {
            Trace.WriteLine("ExecuteCommand: " + command, "SSHConnection");
            _shell.Value.WriteLine(command);
            DumpTillFind(_shell.Value, command); // The command is (normally) repeated back to us...
            _shell.Value.ReadLine(); // Read back the end of line after the command is sent out.
            DumpTillFind(_shell.Value, _prompt, output);
            return this;
        }

        /// <summary>
        /// Run a command. Scan the input for text and when we see it, send response strings.
        /// </summary>
        /// <param name="command">The command to start things off</param>
        /// <param name="seeAndRespond">When we see a bit of text (a regex), then respond with another bit of text.</param>
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

        /// <summary>
        /// Copy a remote directory locally
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="remotedir"></param>
        /// <param name="localDir"></param>
        /// <returns></returns>
        public SSHConnection CopyRemoteDirectoryLocally(string remotedir, DirectoryInfo localDir)
        {
            _scpError = null;
            _scp.Value.Download(remotedir, localDir);
            if (_scpError != null)
            {
                throw _scpError;
            }
            return this;
        }
    }
}
