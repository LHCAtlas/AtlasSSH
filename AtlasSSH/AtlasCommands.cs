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
                .ExecuteCommand(string.Format("echo {0} | voms-proxy-init -voms {1}", passwordInfo.Password, voms),
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

        /// <summary>
        /// Fetch a dataset from the grid using Rucio to a local directory.
        /// </summary>
        /// <param name="connection">A previously configured connection with everything ready to go for GRID access.</param>
        /// <param name="datasetName">The rucio dataset name</param>
        /// <param name="localDirectory"></param>
        /// <returns></returns>
        public static SSHConnection DownloadFromGRID(this SSHConnection connection, string datasetName, string localDirectory)
        {
            // Does the dataset exist?
            var response = new List<string>();
            connection.ExecuteCommand(string.Format("rucio ls {0}", datasetName), l => response.Add(l));

            var dsnames = response
                .Where(l => l.Contains("DATASET"))
                .Select(l => l.Split(' '))
                .Where(sl => sl.Length > 1)
                .Select(sl => sl[1])
                .ToArray();

            if (!dsnames.Where(n => n == datasetName).Any())
            {
                throw new ArgumentException(string.Format("Unable to find any datasets with the name '{0}'.", datasetName));
            }

            // We good on creating the directory?
            connection.ExecuteCommand(string.Format("mkdir -p {0}", localDirectory), l => { throw new ArgumentException("Error trying to create directory {0} for dataset on remote machine.", localDirectory); });

            // Next, do the download
            response.Clear();
            connection.ExecuteCommand(string.Format("rucio download {0} --dir {1}", datasetName, localDirectory));

            return connection;
        }
    }
}
