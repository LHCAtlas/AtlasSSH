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
    class dummySSHConnection : ISSHConnection
    {
        private Dictionary<string, string> _responses = null;

        const string CrLf = "\r\n";

        /// <summary>
        /// If we see a given command we should return with a response.
        /// </summary>
        /// <param name="commandAndResponse"></param>
        public dummySSHConnection(Dictionary<string, string> commandAndResponse)
        {
            _responses = commandAndResponse;
        }

        /// <summary>
        /// Execute a command and return whatever we are supposed to.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="output"></param>
        /// <param name="secondsTimeout"></param>
        /// <returns></returns>
        public ISSHConnection ExecuteCommand(string command, Action<string> output = null, int secondsTimeout = 60*60)
        {
            string result;
            if (!_responses.TryGetValue(command, out result))
            {
                Console.WriteLine("Error: asked for a command '{0}' and had no response!", command);
                throw new ArgumentException("Command " + command + " not known - can't response");
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
            return this;
        }

        public ISSHConnection CopyRemoteDirectoryLocally(string remotedir, System.IO.DirectoryInfo localDir, Action<string> statusUpdate = null)
        {
            throw new NotImplementedException();
        }
    }
}
