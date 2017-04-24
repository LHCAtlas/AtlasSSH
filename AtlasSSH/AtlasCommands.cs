using CredentialManagement;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
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
    public class DatasetDoesNotExistException : Exception
    {
        public DatasetDoesNotExistException() { }
        public DatasetDoesNotExistException(string message) : base(message) { }
        public DatasetDoesNotExistException(string message, Exception inner) : base(message, inner) { }
        protected DatasetDoesNotExistException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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


    [Serializable]
    public class FileFailedToDownloadException : Exception
    {
        public FileFailedToDownloadException() { }
        public FileFailedToDownloadException(string message) : base(message) { }
        public FileFailedToDownloadException(string message, Exception inner) : base(message, inner) { }
        protected FileFailedToDownloadException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class ClockSkewException : Exception
    {
        public ClockSkewException() { }
        public ClockSkewException(string message) : base(message) { }
        public ClockSkewException(string message, Exception inner) : base(message, inner) { }
        protected ClockSkewException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
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
        public static ISSHConnection setupATLAS(this ISSHConnection connection, bool dumpOnly = false)
        {
            bool badCommand = false;
            var r = connection
                .ExecuteCommand("setupATLAS", dumpOnly: dumpOnly, output: l => badCommand = badCommand || l.Contains("command not found"));
            if (badCommand)
            {
                throw new LinuxConfigException("Unable to setupATLAS - command is not known!");
            }

            return r;
        }

        /// <summary>
        /// Setup Rucio. ATLAS must have been previously configured, or this will "crash".
        /// </summary>
        /// <param name="connection">Connection on-which we will set everything up</param>
        /// <param name="rucioUsername">The user alias used on the grid</param>
        /// <returns>A shell on-which rucio has been setup (the same connection that went in)</returns>
        public static ISSHConnection setupRucio(this ISSHConnection connection, string rucioUsername, bool dumpOnly = false)
        {
            int hashCount = 0;
            connection
                .ExecuteCommand(string.Format("export RUCIO_ACCOUNT={0}", rucioUsername), dumpOnly: dumpOnly)
                .ExecuteCommand("lsetup rucio", dumpOnly: dumpOnly)
                .ExecuteCommand("hash rucio", l => hashCount += 1, dumpOnly: dumpOnly);

            if (hashCount != 0 && !dumpOnly)
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
        public static ISSHConnection VomsProxyInit(this ISSHConnection connection, string voms, Func<bool> failNow = null, bool dumpOnly = false)
        {
            // Get the GRID VOMS password
            var sclist = new CredentialSet("GRID");
            var passwordInfo = sclist.Load().FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException("There is no generic windows credential targeting the network address 'GRID' for username. This password should be your cert pass phrase and your on-the-grid username. Please create one on this machine.");
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
                    secondsTimeout: 20, failNow: failNow, dumpOnly: dumpOnly
                    );

            // If we failed to get the proxy, then build an error message that can be understood. Since this
            // could be for a large range of reasons, we are going to pass back a lot of info to the user
            // so they can figure it out (not likely a program will be able to sort this out).

            if (goodProxy == false && !dumpOnly)
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
            Func<string[], string[]> fileNameFilter = null,
            Func<bool> failNow = null,
            int timeout = 3600)
        {
            // Does the dataset exist?
            if (fileStatus != null)
            {
                fileStatus("Checking the dataset exists");
            }
            var response = new List<string>();
            connection.ExecuteCommand(string.Format("rucio ls {0}", datasetName), l => response.Add(l), secondsTimeout: 60, failNow: failNow);

            var dsnames = response
                .Where(l => l.Contains("DATASET") | l.Contains("CONTAINER"))
                .Select(l => l.Split(' '))
                .Where(sl => sl.Length > 1)
                .Select(sl => sl[1])
                .ToArray();

            if (!dsnames.Where(n => n.SantizeDSName() == datasetName.SantizeDSName()).Any())
            {
                throw new ArgumentException(string.Format("Unable to find any datasets on the GRID (in rucuio) with the name '{0}'.", datasetName));
            }

            // Get the complete list of files in the dataset.
            if (fileStatus != null)
            {
                fileStatus("Getting the complete list of files from the dataset");
            }
            var fileNameList = connection.FilelistFromGRID(datasetName, failNow: failNow);

            // Filter them if need be.
            var goodFiles = fileNameFilter != null
                ? fileNameFilter(fileNameList)
                : fileNameList;

            // Create a file that contains all the files we want to download up on the host.
            var fileListName = string.Format("/tmp/{0}.filelist", datasetName.SantizeDSName());
            connection.ExecuteCommand("rm -rf " + fileListName);
            connection.Apply(goodFiles, (c, fname) => c.ExecuteCommand(string.Format("echo {0} >> {1}", fname, fileListName)));

            // We good on creating the directory?
            connection.ExecuteCommand(string.Format("mkdir -p {0}", localDirectory),
                l => { throw new ArgumentException("Error trying to create directory {0} for dataset on remote machine.", localDirectory); },
                secondsTimeout: 20);

            // If we have no files to download, then we are totally done!
            // We do this after the directory is created, so if there are no files, a check still
            // works.
            if (goodFiles.Length == 0)
            {
                return connection;
            }

            // Next, do the download
            response.Clear();
            fileStatus?.Invoke($"Starting GRID download of {datasetName}...");

            Policy
                .Handle<ClockSkewException>()
                .WaitAndRetry(new[]
                {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1)
                }, (e, ts) => { fileStatus?.Invoke("Clock Skew error - wait and re-try"); })
                .Execute(() => DoRucioDownload(connection, localDirectory, fileStatus, failNow, timeout, fileListName));
            return connection;
        }

        private static void DoRucioDownload(ISSHConnection connection, string localDirectory, Action<string> fileStatus, Func<bool> failNow, int timeout, string fileListName)
        {
            string filesThatFailedToDownload = "";
            bool foundClockSkewMessage = false;
            connection.ExecuteCommand($"rucio -T {timeout} download --dir {localDirectory} `cat {fileListName}`", l =>
            {
                // Look for something that indicates which file we are currently getting from the GRID.
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

                // Watch for the end to see the overall status
                if (l.Contains("Files that cannot be downloaded :"))
                {
                    filesThatFailedToDownload = l.Split(' ').Where(i => !string.IsNullOrWhiteSpace(i)).Last();
                }
                foundClockSkewMessage |= l.Contains("check clock skew between hosts.");
            },
            refreshTimeout: true, failNow: failNow, secondsTimeout: timeout);

            // Check for errors that happened while running the command.
            if (filesThatFailedToDownload != "0")
            {
                // Special case - there was a clock skew error.
                if (foundClockSkewMessage)
                {
                    throw new ClockSkewException($"Failed to download {filesThatFailedToDownload} files due to clock skew. Please double check!");
                }

                // Something else - will likely require a human to get involved.
                throw new FileFailedToDownloadException($"Failed to download all the files from the GRID - {filesThatFailedToDownload} files failed to download!");
            }
        }

        /// <summary>
        /// Info about a single file on the internet.
        /// </summary>
        [Serializable]
        public class GRIDFileInfo
        {
            /// <summary>
            /// Fully qualified name of the file
            /// </summary>
            public string name;

            /// <summary>
            /// Size (in MB) of the file
            /// </summary>
            public double size;

            /// <summary>
            /// Number of events in the file
            /// </summary>
            public int eventCount;
        }

        /// <summary>
        /// Returns the name of the files in a GRID dataset.
        /// </summary>
        /// <param name="connection">The already setup SSH connection</param>
        /// <param name="datasetName">The dataset that we are to query</param>
        /// <param name="failNow">Return true if long-running commands should quit right away</param>
        /// <param name="dumpOnly">If we are to only test-run, but not actually run.</param>
        /// <returns></returns>
        public static string[] FilelistFromGRID(this ISSHConnection connection, string datasetName, Func<bool> failNow = null, bool dumpOnly = false)
        {
            return connection.FileInfoFromGRID(datasetName, failNow, dumpOnly)
                .Select(i => i.name)
                .ToArray();
        }

        /// <summary>
        /// Store the data we get back.
        /// </summary>
        static Lazy<DiskCacheTyped<GRIDFileInfo[]>> _GRIDFileInfoCache = new Lazy<DiskCacheTyped<GRIDFileInfo[]>>(() => new DiskCacheTyped<GRIDFileInfo[]>("GRIDFileInfoCache"));

        /// <summary>
        /// Returns the list of files associated with a dataset, as fetched from the grid.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="datasetName"></param>
        /// <returns></returns>
        public static List<GRIDFileInfo> FileInfoFromGRID(this ISSHConnection connection, string datasetName, Func<bool> failNow =null, bool dumpOnly = false)
        {
            // If we have a cache hit, then avoid the really slow lookup.
            var c = _GRIDFileInfoCache.Value[datasetName] as GRIDFileInfo[];
            if (c != null)
            {
                return c.ToList(); ;
            }

            // Run it in rucio and bring back our answers.
            var fileNameList = new List<GRIDFileInfo>();
            var filenameMatch = new Regex(@"\| +(?<fname>\S*) +\| +[^\|]+\| +[^\|]+\| +(?<fsize>[0-9\.]*) *(?<fsizeunits>[MGTB]*) *\| +(?<events>[0-9]*) +\|");
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
                            // Parse it all out
                            var gi = new GRIDFileInfo()
                            {
                                name = fname,
                                eventCount = m.Groups["events"].Value == "" ? 0 : int.Parse(m.Groups["events"].Value),
                                size = ConvertToMB(m.Groups["fsize"].Value, m.Groups["fsizeunits"].Value)
                            };
                            fileNameList.Add(gi);
                        }
                    }
                }
            },
            failNow: failNow, dumpOnly: dumpOnly
            );

            if (bad & !dumpOnly)
            {
                throw new DatasetDoesNotExistException(string.Format("Dataset '{0}' does not exist - can't get its list of files.", datasetName));
            }

            _GRIDFileInfoCache.Value[datasetName] = fileNameList.ToArray();
            return fileNameList;
        }

        /// <summary>
        /// Convert a file size to MB
        /// </summary>
        /// <param name="number"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        private static double ConvertToMB(string number, string units)
        {
            if (number == "")
            {
                return 0;
            }
            var n = double.Parse(number);
            if (units == "")
            {
                return (n/1024/1024);
            } else if (units == "MB")
            {
                return n;
            } else if (units == "GB")
            {
                return (n * 1024);
            } else if (units == "TB")
            {
                return (n * 1024 * 1024);
            }
            throw new InvalidOperationException($"Do not know how to convert {units} to MB in fetching dataset file info!");
        }

        /// <summary>
        /// Setup a release. If it can't be found, error will be thrown
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="releaseName">Full release name (e.g. 'Base,2.3.30')</param>
        /// <param name="linuxLocation">Directory where the linux location can be found</param>
        /// <returns></returns>
        public static ISSHConnection SetupRcRelease(this ISSHConnection connection, string linuxLocation, string releaseName, bool dumpOnly = false)
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
            }, dumpOnly: dumpOnly);
            if (dirAlreadyExists)
                throw new LinuxMissingConfigurationException(string.Format("Release directory '{0}' already exists - we need a fresh start", linuxLocation));
            if (!dirCreated)
                throw new LinuxMissingConfigurationException(string.Format("Unable to create release directory '{0}'.", linuxLocation));

            // Next, put our selves there
            connection.ExecuteCommand(string.Format("cd {0}", linuxLocation), dumpOnly: dumpOnly);

            // And then do the setup
            bool found = false;
            connection.ExecuteCommand(string.Format("rcSetup {0}", releaseName), l => found = found ? true : l.Contains("Found ASG release with"), dumpOnly: dumpOnly);
            if (!found && !dumpOnly)
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
        public static ISSHConnection Kinit (this ISSHConnection connection, string username, string password, bool dumpOnly = false)
        {
            var allStrings = new List<string>();
            connection.ExecuteCommand(string.Format("echo {0} | kinit {1}", password, username), l => allStrings.Add(l), dumpOnly: dumpOnly);
            var errorStrings = allStrings.Where(l => l.StartsWith("kinit:")).ToArray();
            if (errorStrings.Length > 0)
            {
                throw new LinuxCommandErrorException(string.Format("Failed to execute kinit command: {0}", errorStrings[0]));
            }
            if (!allStrings.Where(l => l.StartsWith("Password for")).Any() && !dumpOnly)
            {
                throw new LinuxCommandErrorException(string.Format("Failed to execute kinit command: {0}", allStrings[0]));
            }
            return connection;
        }

        /// <summary>
        /// Check out SVN or a GIT package.
        /// </summary>
        /// <param name="connection">Connection on which we should be checking this out on</param>
        /// <param name="scPackagePath">The svn path to the package. Basically what you would hand to the rc checkout command. Nohting like "tags" or "trunk" is permitted.</param>
        /// <param name="scRevision">The revision number. A SVN revision number. If blank, then the version associated with the build is checked out.</param>
        /// <returns></returns>
        public static ISSHConnection CheckoutPackage(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
        {
            var isGit = scPackagePath.EndsWith(".git");
            return isGit
                ? connection.CheckoutPackageGit(scPackagePath, scRevision, failNow, dumpOnly)
                : connection.CheckoutPackageSVN(scPackagePath, scRevision, failNow, dumpOnly);
        }

        /// <summary>
        /// Check out a Git package from the CERN git repro, of what is pecified in the package.
        /// </summary>
        /// <param name="connection">Connection on which to do the checkout</param>
        /// <param name="scPackagePath">Path to the package, perhaps a short-cut (see remarks)</param>
        /// <param name="scRevision">Revision for the package, or empty to grab the head</param>
        /// <param name="failNow">Fuction that returns true if we should bail right away</param>
        /// <param name="dumpOnly">Just dump the commands output - don't actually do anything.</param>
        /// <returns></returns>
        public static ISSHConnection CheckoutPackageGit(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
        {
            // The revision must be specified.
            if (string.IsNullOrWhiteSpace(scRevision))
            {
                throw new ArgumentException($"The commit hash for {scPackagePath} must be specified - it may well move with time");
            }
            if (scRevision.Length < 15)
            {
                throw new ArgumentException($"The SHA {scRevision} for git package {scPackagePath} doesn't look like a full git SHA (e.g. 77658117c62ac99610068228668563d29baa3912).");
            }

            // Determine if this was short-hand, and we need to put the gitlab specification in front.
            if (!scPackagePath.Contains("://"))
            {
                scPackagePath = $"https://:@gitlab.cern.ch:8443/{scPackagePath}";
            }

            // Do the check out
            connection.ExecuteCommand($"git clone {scPackagePath}", secondsTimeout: 120, failNow: failNow, dumpOnly: dumpOnly);

            // Now, we have to move to that revision.
            var pkgName = Path.GetFileNameWithoutExtension(scPackagePath.Split('/').Last());

            string error = "";
            connection.ExecuteCommand($"cd {pkgName}; git checkout {scRevision}; cd ..", output: l => error = l.Contains("error") ? l : error);
            if (error.Length > 0)
            {
                throw new LinuxCommandErrorException($"Unable to check out package {scPackagePath} with SHA {scRevision}: {error}");
            }

            return connection;
        }

        /// <summary>
        /// Check out a SVN package
        /// </summary>
        /// <param name="connection">Connection on which to check this guy out</param>
        /// <param name="scPackagePath">Path to the package</param>
        /// <param name="scRevision">tag/svn revision to check out</param>
        /// <param name="failNow">Function retursn true when we should quit</param>
        /// <param name="dumpOnly">Are we just dumping commands, or doing work?</param>
        /// <returns></returns>
        public static ISSHConnection CheckoutPackageSVN(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
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
            connection.ExecuteCommand(string.Format("rc checkout_pkg {0}", fullPackagePath), l => sawRevisionMessage = sawRevisionMessage ? true : l.Contains("Checked out revision"), secondsTimeout: 120, failNow: failNow, dumpOnly: dumpOnly);
            if (!sawRevisionMessage && !dumpOnly)
            {
                throw new LinuxCommandErrorException(string.Format("Unable to check out svn package {0}.", scPackagePath));
            }

            // If this was checked out to trunk, then we need to fix it up.
            if (!string.IsNullOrWhiteSpace(scRevision))
            {
                var packageName = scPackagePath.Split('/').Last();
                var checkoutName = fullPackagePath.Split('/').Last();
                bool lineSeen = false;
                connection.ExecuteCommand(string.Format("mv {0} {1}", checkoutName, packageName), l => lineSeen = true, dumpOnly: dumpOnly);
                if (lineSeen && !dumpOnly)
                {
                    throw new LinuxCommandErrorException("Unable to rename the downloaded trunk directory for package '" + scPackagePath + "'.");
                }
            }

            return connection;
        }

        /// <summary>
        /// Execute a Linux command. Throw if the command does not return 0 to the shell. Provides for ways to capture (or ignore)
        /// the output of the command.
        /// </summary>
        /// <param name="connection">The connection onwhich to execute the command.</param>
        /// <param name="command">The command to execute</param>
        /// <param name="dumpOnly">If true, then only print out the commands</param>
        /// <param name="failNow">If true, attempt to bail out of the command early.</param>
        /// <param name="processLine">A function called for each line read back while the command is executing</param>
        /// <param name="seeAndRespond">If a string is seen in the output, then the given response is sent</param>
        /// <param name="refreshTimeout">If we see text, reset the timeout counter</param>
        /// <param name="secondsTimeout">How many seconds with no output or since beginning of command before we declare failure?</param>
        /// <returns>The connection we ran this on. Enables fluent progreamming</returns>
        /// <remarks>
        /// We check the status by echoing the shell variable $? - so this actually runs two commands.
        /// </remarks>
        public static ISSHConnection ExecuteLinuxCommand(this ISSHConnection connection,
            string command, 
            Action<string> processLine = null,
            Func<bool> failNow = null,
            bool dumpOnly = false,
            int secondsTimeout = 60*60,
            bool refreshTimeout = false,
            Dictionary<string, string> seeAndRespond = null)
        {
            string rtnValue = "";
            processLine = processLine == null ? l => { } : processLine; 
            connection
                .ExecuteCommand(command, processLine, failNow: failNow, dumpOnly: dumpOnly, seeAndRespond: seeAndRespond, refreshTimeout: refreshTimeout, secondsTimeout: secondsTimeout)
                .ExecuteCommand("echo $?", l => rtnValue = l, dumpOnly: dumpOnly);

            if (rtnValue != "0" && !dumpOnly)
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
        public static ISSHConnection BuildWorkArea(this ISSHConnection connection, Func<bool> failNow = null, bool dumpOnly = false)
        {
            string findPkgError = null;
            var buildLines = new List<string>();
            try
            {
                return connection
                    .ExecuteLinuxCommand("rc find_packages", l => findPkgError = l, failNow: failNow, dumpOnly: dumpOnly)
                    .ExecuteLinuxCommand("rc compile", l => buildLines.Add(l), failNow: failNow, dumpOnly: dumpOnly);
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
