using CredentialManagement;
using Renci.SshNet;
using System;
using System.Collections.Generic;
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

            // And create a shell stream. Initialize to find the prompt so we can figure out, later, when
            // a task has finished.
            _shell = new Lazy<ShellStream>(() => {
                var s = _client.Value.CreateShellStream("Commands", 240, 200, 132, 80, 240*200);
                s.WriteLine("# this initialization");
                DumpTillFind(s, "# this initialization");
                _prompt = s.ReadLine();
                return s;
            });
        }

        /// <summary>
        /// Hold onto the client. By referencing it, a connection will be made.
        /// </summary>
        private Lazy<SshClient> _client;

        /// <summary>
        /// Hold onto a shell stream. By referencing it the first time it will be created.
        /// </summary>
        private Lazy<ShellStream> _shell;

        /// <summary>
        /// What we've determined is the command prompt. We have to use it b.c. we want to make sure not to get
        /// lost as to what command is being worked on.
        /// </summary>
        private string _prompt;

        /// <summary>
        /// Dump the input until we see a particular string in the returning text.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="p"></param>
        private void DumpTillFind(ShellStream s, string matchText, Action<string> ongo = null)
        {
            while (true)
            {
                var l = s.ReadLine();
                if (l == null)
                {
                    Thread.Sleep(100);
                }
                else
                {
                    if (l.Contains(matchText))
                    {
                        return;
                    }
                    else
                    {
                        if (ongo != null)
                        {
                            ongo(l);
                        }
                    }
                }
            }
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
            _shell.Value.WriteLine(command);
            DumpTillFind(_shell.Value, command); // The command is (normally) repeated back to us...
            DumpTillFind(_shell.Value, _prompt, output);
            return this;
        }
    }
}
