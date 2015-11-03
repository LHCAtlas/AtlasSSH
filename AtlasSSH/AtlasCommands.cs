using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AtlasSSH
{
    /// <summary>
    /// Thrown when there is a config error on the Linux side of things; and that commands that
    /// should always work on the Linux host, are not.
    /// </summary>
    [Serializable]
    public class LinuxConfigException : Exception
    {
        public LinuxConfigException() { }
        public LinuxConfigException(string message) : base(message) { }
        public LinuxConfigException(string message, Exception inner) : base(message, inner) { }
        protected LinuxConfigException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }

    /// <summary>
    /// Thrown when something on Linux was tried, but the config prevented it (e.g. missing release).
    /// </summary>
    [Serializable]
    public class LinuxMissingConfigurationException : Exception
    {
        public LinuxMissingConfigurationException() { }
        public LinuxMissingConfigurationException(string message) : base(message) { }
        public LinuxMissingConfigurationException(string message, Exception inner) : base(message, inner) { }
        protected LinuxMissingConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }


    [Serializable]
    public class LinuxCommandErrorException : Exception
    {
        public LinuxCommandErrorException() { }
        public LinuxCommandErrorException(string message) : base(message) { }
        public LinuxCommandErrorException(string message, Exception inner) : base(message, inner) { }
        protected LinuxCommandErrorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }

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
        public static ISSHConnection setupATLAS(this ISSHConnection connection)
        {
            bool foundalias = false;
            connection
                .ExecuteCommand("setupATLAS")
                .ExecuteCommand("alias", l => foundalias = foundalias || l.Contains("rcSetup"));

            if (!foundalias)
            {
                throw new LinuxConfigException("The setupATLAS command did not have the expected effect - rcSetup was not defined as an alias");
            }

            return connection;
        }

        /// <summary>
        /// Setup Rucio. ATLAS must have been previously configured, or this will "crash".
        /// </summary>
        /// <param name="connection">Connection on-which we will set everything up</param>
        /// <param name="rucioUsername">The user alias used on the grid</param>
        /// <returns>A shell on-which rucio has been setup (the same connection that went in)</returns>
        public static ISSHConnection setupRucio(this ISSHConnection connection, string rucioUsername)
        {
            int hashCount = 0;
            connection
                .ExecuteCommand(string.Format("export RUCIO_ACCOUNT={0}", rucioUsername))
                .ExecuteCommand("localSetupRucioClients")
                .ExecuteCommand("hash rucio", l => hashCount++);

            if (hashCount != 0)
            {
                throw new LinuxConfigException("Unable to setup Rucio... did you forget to setup ATLAS first?");
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
        public static ISSHConnection VomsProxyInit(this ISSHConnection connection, string voms, string GRIDUsername)
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
                    l =>
                    {
                        goodProxy = goodProxy || l.Contains("Your proxy is valid");
                        whatHappened.Add(l);
                    },
                    secondsTimeout: 20
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
        /// <param name="localDirectory">The local directory (on Linux) where the file should be downloaded</param>
        /// <param name="fileStatus">Gets updates as new files are downloaded. This will contain just the filename.</param>
        /// <param name="fileNameFilter">Filter function to alter the files that are to be downloaded</param>
        /// <returns></returns>
        public static ISSHConnection DownloadFromGRID(this ISSHConnection connection, string datasetName, string localDirectory,
            Action<string> fileStatus = null,
            Func<string[], string[]> fileNameFilter = null)
        {
            // Does the dataset exist?
            var response = new List<string>();
            connection.ExecuteCommand(string.Format("rucio ls {0}", datasetName), l => response.Add(l), secondsTimeout: 60);

            var dsnames = response
                .Where(l => l.Contains("DATASET") | l.Contains("CONTAINER"))
                .Select(l => l.Split(' '))
                .Where(sl => sl.Length > 1)
                .Select(sl => sl[1])
                .ToArray();

            if (!dsnames.Where(n => n.SantizeDSName() == datasetName).Any())
            {
                throw new ArgumentException(string.Format("Unable to find any datasets with the name '{0}'.", datasetName));
            }

            // If we are going to filter files, then the next thing we need to do is look at all files in the dataset.
            var toDownload = datasetName;
            if (fileNameFilter != null)
            {
                var fileNameList = connection.FilelistFromGRID(datasetName);
                var goodFiles = fileNameFilter(fileNameList);
                if (goodFiles.Length == 0)
                {
                    return connection;
                }

                var toDownloadBuilder = new StringBuilder();
                foreach (var f in goodFiles)
                {
                    toDownloadBuilder.Append(f + " ");
                }
                toDownload = toDownloadBuilder.ToString();
            }

            // We good on creating the directory?
            connection.ExecuteCommand(string.Format("mkdir -p {0}", localDirectory),
                l => { throw new ArgumentException("Error trying to create directory {0} for dataset on remote machine.", localDirectory); },
                secondsTimeout: 20);

            // Next, do the download
            response.Clear();
            connection.ExecuteCommand(string.Format("rucio download --dir {1} {0}", toDownload, localDirectory), l =>
            {
                if (fileStatus != null)
                {
                    const string fileNameMarker = "Starting the download of ";
                    var idx = l.IndexOf(fileNameMarker);
                    if (idx >= 0)
                    {
                        var closeBracket = l.IndexOf(']', idx);
                        var startOfFileName = idx + fileNameMarker.Length;
                        fileStatus(l.Substring(startOfFileName, closeBracket - startOfFileName));
                    }
                }
            });

            return connection;
        }

        /// <summary>
        /// Returns the list of files associated with a dataset.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        public static string[] FilelistFromGRID(this ISSHConnection connection, string datasetName)
        {
            var fileNameList = new List<string>();
            var filenameMatch = new Regex(@"\| +(?<fname>\S*) +\| +\S* +\| +\S* +\| +\S* +\| +\S* +\|");
            bool bad = false;
            connection.ExecuteCommand(string.Format("rucio list-files {0}", datasetName), l =>
            {
                if (l.Contains("Data identifier not found"))
                {
                    bad = true;
                }
                if (!bad)
                {
                    var m = filenameMatch.Match(l);
                    if (m.Success)
                    {
                        var fname = m.Groups["fname"].Value;
                        if (fname != "SCOPE:NAME")
                        {
                            fileNameList.Add(fname);
                        }
                    }
                }
            });

            if (bad)
            {
                throw new ArgumentException("Dataset '{0}' does not exist - can't get its list of files.", datasetName);
            }

            return fileNameList.ToArray();
        }

        /// <summary>
        /// Setup a release. If it can't be found, error will be thrown
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="releaseName">Full release name (e.g. 'Base,2.3.30')</param>
        /// <param name="linuxLocation">Directory where the linux location can be found</param>
        /// <returns></returns>
        public static ISSHConnection SetupRcRelease(this ISSHConnection connection, string linuxLocation, string releaseName)
        {
            // Check the arguments for something dumb
            if (string.IsNullOrWhiteSpace(releaseName))
            {
                throw new ArgumentException("A release name must be provided");
            }
            if (string.IsNullOrWhiteSpace(linuxLocation) || !linuxLocation.StartsWith("/"))
            {
                throw new ArgumentException("The release directory must be an absolute Linux path (start with a '/')");
            }

            // First we have to create the directory.
            bool dirCreated = true;
            bool dirAlreadyExists = false;
            connection.ExecuteCommand(string.Format("mkdir {0}", linuxLocation), l =>
            {
                dirCreated = !dirCreated ? false : string.IsNullOrWhiteSpace(l);
                dirAlreadyExists = dirAlreadyExists ? true : l.Contains("File exists");
            });
            if (dirAlreadyExists)
                throw new LinuxMissingConfigurationException(string.Format("Release directory '{0}' already exists - we need a fresh start", linuxLocation));
            if (!dirCreated)
                throw new LinuxMissingConfigurationException(string.Format("Unable to create release directory '{0}'.", linuxLocation));

            // Next, put our selves there
            connection.ExecuteCommand(string.Format("cd {0}", linuxLocation));

            // And then do the setup
            bool found = false;
            connection.ExecuteCommand(string.Format("rcSetup {0}", releaseName), l => found = found ? true : l.Contains("Found ASG release with"));
            if (!found)
                throw new LinuxMissingConfigurationException(string.Format("Unable to find release '{0}'", releaseName));

            // Return the connection to make it a functional interface.
            return connection;
        }

        /// <summary>
        /// Execute a kinit
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static ISSHConnection Kinit (this ISSHConnection connection, string username, string password)
        {
            var allStrings = new List<string>();
            connection.ExecuteCommand(string.Format("echo {0} | kinit {1}", password, username), l => allStrings.Add(l));
            var errorStrings = allStrings.Where(l => l.StartsWith("kinit:")).ToArray();
            if (errorStrings.Length > 0)
            {
                throw new LinuxCommandErrorException(string.Format("Failed to execute kinit command: {0}", errorStrings[0]));
            }
            if (!allStrings.Where(l => l.StartsWith("Password for")).Any())
            {
                throw new LinuxCommandErrorException(string.Format("Failed to execute kinit command: {0}", allStrings[0]));
            }
            return connection;
        }

        /// <summary>
        /// Check out a package.
        /// 
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="scPackagePath">The svn path to the package. Basically what you would hand to the rc checkout command. Nohting like "tags" or "trunk" is permitted.</param>
        /// <param name="scRevision">The revision number. A SVN revision number. If blank, then the version associated with the build is checked out.</param>
        /// <returns></returns>
        public static ISSHConnection CheckoutPackage(this ISSHConnection connection, string scPackagePath, string scRevision)
        {
            // Has the user asked us to do something we won't do?
            if (scPackagePath.EndsWith("/tags"))
            {
                // This throws because it moves with time - so we can't fully specify what package we are checking out.
                throw new ArgumentException(string.Format("The package path ({0}) can't end with a tags directory to indicate the latest tag - the tag will change over time and can't be tracked.", scPackagePath));
            }
            if (scPackagePath.EndsWith("/trunk"))
            {
                // This throws because it moves with time.
                throw new ArgumentException(string.Format("The package path ({0}) can't end with a trunk directory to indicate the HEAD version - the tag will change over time and can't be tracked.", scPackagePath));
            }
            if (scPackagePath.EndsWith("/"))
            {
                throw new ArgumentException(string.Format("The package path {0} ends with a slash - this is not allowed", scPackagePath));
            }
            if (!string.IsNullOrWhiteSpace(scRevision) && !scPackagePath.Contains("/"))
            {
                throw new ArgumentException(string.Format("If a revision is specified then the package path must be fully specified (was '{0}' - {1})", scPackagePath, scRevision));
            }

            // How we run the command will depend on if this is a release version of the package
            // or this is a revision specified.

            var fullPackagePath = scPackagePath;
            if (!string.IsNullOrWhiteSpace(scRevision))
            {
                if (fullPackagePath.Contains("/trunk/"))
                {
                    fullPackagePath += "@" + scRevision;
                }
                else
                {
                    fullPackagePath += "/trunk@" + scRevision;
                }
            }

            var sawRevisionMessage = false;            
            connection.ExecuteCommand(string.Format("rc checkout_pkg {0}", fullPackagePath), l => sawRevisionMessage = sawRevisionMessage ? true : l.Contains("Checked out revision"), secondsTimeout: 30);
            if (!sawRevisionMessage)
            {
                throw new LinuxCommandErrorException(string.Format("Unable to check out svn package {0}.", scPackagePath));
            }

            // If this was checked out to trunk, then we need to fix it up.
            if (!string.IsNullOrWhiteSpace(scRevision))
            {
                var packageName = scPackagePath.Split('/').Last();
                bool lineSeen = false;
                connection.ExecuteCommand(string.Format("mv {1}@{0} {1}", scRevision, packageName), l => lineSeen = true);
                if (lineSeen)
                {
                    throw new LinuxCommandErrorException("Unable to rename the downloaded trunk directory for package '" + scPackagePath + "'.");
                }
            }

            return connection;
        }

        /// <summary>
        /// Execute a Linux command. Throw if the command does not return 0.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static ISSHConnection ExecuteLinuxCommand(this ISSHConnection connection, string command, Action<string> processLine = null)
        {
            string rtnValue = "";
            processLine = processLine == null ? l => { } : processLine; 
            connection
                .ExecuteCommand(command, processLine)
                .ExecuteCommand("echo $?", l => rtnValue = l);

            if (rtnValue != "0")
            {
                throw new LinuxCommandErrorException(string.Format("The remote command '{0}' return status error code '{1}'", command, rtnValue));
            }
            return connection;
        }

        /// <summary>
        /// Build the work area
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static ISSHConnection BuildWorkArea(this ISSHConnection connection)
        {
            string findPkgError = null;
            var buildLines = new List<string>();
            try
            {
                return connection
                    .ExecuteLinuxCommand("rc find_packages", l => findPkgError = l)
                    .ExecuteLinuxCommand("rc compile", l => buildLines.Add(l));
            } catch (LinuxCommandErrorException lerr)
            {
                if (buildLines.Count > 0)
                {
                    var errors = buildLines.Where(ln => ln.Contains("error:"));
                    var err = new StringBuilder();
                    err.AppendLine("Unable to compile package:");
                    foreach (var errline in errors)
                    {
                        err.AppendLine("    -> " + errline);
                    }
                    throw new LinuxCommandErrorException(err.ToString(), lerr);
                }
                else
                {
                    throw new LinuxCommandErrorException(string.Format("Failed to run 'rc find_packages': {0}", findPkgError), lerr);
                }
            }
        }
    }
}
