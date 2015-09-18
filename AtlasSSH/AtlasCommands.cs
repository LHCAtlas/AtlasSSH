using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    /// <summary>
    /// Some commands
    /// </summary>
    public static class AtlasCommands
    {
        /// <summary>
        /// Run the setup ATLAS command
        /// </summary>
        /// <param name="connection">The connection that will understand the setupATLAS command</param>
        /// <returns>A reconfigured SSH shell connection (same as what went in)</returns>
        public static SSHConnection setupATLAS(this SSHConnection connection)
        {
            bool foundalias = false;
            connection
                .ExecuteCommand("setupATLAS")
                .ExecuteCommand("alias", l => foundalias = foundalias || l.Contains("rcSetup"));

            if (!foundalias)
            {
                throw new InvalidOperationException("The setupATLAS command did not have the expected effect - rcSetup was not defined as an alias");
            }

            return connection;
        }

        /// <summary>
        /// Setup Rucio. ATLAS must have been previously configured, or this will "crash".
        /// </summary>
        /// <param name="connection">Connection on-which we will set everything up</param>
        /// <param name="rucioUsername">The user alias used on the grid</param>
        /// <returns>A shell on-which rucio has been setup (the same connection that went in)</returns>
        public static SSHConnection setupRucio(this SSHConnection connection, string rucioUsername)
        {
            int hashCount = 0;
            connection
                .ExecuteCommand(string.Format("export RUCIO_ACCOUNT={0}", rucioUsername))
                .ExecuteCommand("localSetupRucioClients")
                .ExecuteCommand("hash rucio", l => hashCount++);

            if (hashCount != 0)
            {
                throw new InvalidOperationException("Unable to setup Rucio... did you forget to setup ATLAS first?");
            }
            return connection;
        }

        /// <summary>
        /// Initializes the VOMS proxy for use on the GRID. The connection must have already been configured so that
        /// the command voms-proxy-init works.
        /// </summary>
        /// <param name="connection">The configured SSH shell</param>
        /// <param name="GRIDUsername">The username to use to fetch the password for the voms proxy file</param>
        /// <param name="voms">The name of the voms to connect to</param>
        /// <returns>Connection on which the grid is setup and ready to go</returns>
        public static SSHConnection VomsProxyInit(this SSHConnection connection, string voms, string GRIDUsername)
        {
            // Get the GRID VOMS password
            var sclist = new CredentialSet(string.Format("{0}@GRID", GRIDUsername));
            var passwordInfo = sclist.Load().Where(c => c.Username == GRIDUsername).FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException(string.Format("There is no generic windows credential targeting the network address '{0}@GRID' for username '{0}'. This password should be your cert pass phrase. Please create one on this machine.", GRIDUsername));
            }

            // Run the command
            bool goodProxy = false;
            var whatHappened = new List<string>();
            connection
                .ExecuteCommandWithInput(string.Format("voms-proxy-init -voms {0}", voms),
                    new Dictionary<string, string>() { { "identity:", passwordInfo.Password } },
                    l => { 
                        goodProxy = goodProxy || l.Contains("Your proxy is valid");
                        whatHappened.Add(l);
                    }
                    );

            // If we failed to get the proxy, then build an error message that can be understood. Since this
            // could be for a large range of reasons, we are going to pass back a lot of info to the user
            // so they can figure it out (not likely a program will be able to sort this out).

            if (goodProxy == false)
            {
                var error = new StringBuilder();
                error.AppendLine("Failed to get the proxy: ");
                foreach (var l in whatHappened)
                {
                    error.AppendLine(string.Format("  -> {0}", l));
                }
                throw new ArgumentException(error.ToString());
            }

            return connection;
        }
    }
}
