using CredentialManagement;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Nito.AsyncEx.Synchronous;
using System.Threading.Tasks;
using Nito.AsyncEx;

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
    public class DataSetDoesNotExistException : Exception
    {
        public DataSetDoesNotExistException() { }
        public DataSetDoesNotExistException(string message) : base(message) { }
        public DataSetDoesNotExistException(string message, Exception inner) : base(message, inner) { }
        protected DataSetDoesNotExistException(
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
        /// Run the setup ATLAS command.
        /// </summary>
        /// <param name="connection">The connection that will understand the setupATLAS command</param>
        /// <param name="dumpOnly">If true, then tex tis dumpped to standard logging</param>
        /// <returns>A reconfigured SSH shell connection (same as what went in)</returns>
        public static ISSHConnection setupATLAS(this ISSHConnection connection, bool dumpOnly = false)
        {
            return setupATLASAsync(connection, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Run the setup ATLAS command asyncronously.
        /// </summary>
        /// <param name="connection">The connection that will understand the setupATLAS command</param>
        /// <param name="dumpOnly">If true, then tex tis dumpped to standard logging</param>
        /// <returns>A reconfigured SSH shell connection (same as what went in)</returns>
        public static async Task<ISSHConnection> setupATLASAsync(this ISSHConnection connection, bool dumpOnly = false)
        {
            bool badCommand = false;
            var r = await connection
                .ExecuteLinuxCommandAsync("setupATLAS", dumpOnly: dumpOnly, processLine: l => badCommand = badCommand || l.Contains("command not found"), secondsTimeout: 120);

            if (badCommand)
            {
                throw new LinuxConfigException($"Unable to setupATLAS - command is not known! ({connection.MachineName})");
            }

            return r;
        }

        /// <summary>
        /// Setup Rucio. ATLAS must have been previously configured, or this will "crash".
        /// </summary>
        /// <param name="connection">Connection on-which we will set everything up</param>
        /// <param name="rucioUserName">The user alias used on the grid</param>
        /// <param name="dumpOnly">Dump output to the logging interface rather than actually executing anything</param>
        /// <returns>A shell on-which rucio has been setup (the same connection that went in)</returns>
        public static ISSHConnection setupRucio(this ISSHConnection connection, string rucioUserName, bool dumpOnly = false)
        {
            return connection.setupRucioAsync(rucioUserName, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Setup Rucio. ATLAS must have been previously configured, or this will "crash". Execute Asyncrohonously.
        /// </summary>
        /// <param name="connection">Connection on-which we will set everything up</param>
        /// <param name="rucioUserName">The user alias used on the grid</param>
        /// <param name="dumpOnly">Dump output to the logging interface rather than actually executing anything</param>
        /// <returns>A shell on-which rucio has been setup (the same connection that went in)</returns>
        public static async Task<ISSHConnection> setupRucioAsync(this ISSHConnection connection, string rucioUserName, bool dumpOnly = false)
        {
            int hashCount = 0;
            var r = await connection.ExecuteLinuxCommandAsync(string.Format("export RUCIO_ACCOUNT={0}", rucioUserName), dumpOnly: dumpOnly, secondsTimeout: 30);
            r = await r.ExecuteLinuxCommandAsync("lsetup rucio", dumpOnly: dumpOnly, secondsTimeout: 30);
            r = await r.ExecuteLinuxCommandAsync("hash rucio", l => hashCount += 1, dumpOnly: dumpOnly, secondsTimeout: 30);

            if (hashCount != 0 && !dumpOnly)
            {
                throw new LinuxConfigException("Unable to setup Rucio... did you forget to setup ATLAS first?  ({connection.MachineName})");
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
        /// <param name="dumpOnly">Only print out proposed commands</param>
        /// <returns>Connection on which the grid is setup and ready to go</returns>
        public static ISSHConnection VomsProxyInit(this ISSHConnection connection, string voms, Func<bool> failNow = null, bool dumpOnly = false)
        {
            return connection.VomsProxyInitAsync(voms, failNow, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Initializes the VOMS proxy for use on the GRID. The connection must have already been configured so that
        /// the command voms-proxy-init works.
        /// </summary>
        /// <param name="connection">The configured SSH shell</param>
        /// <param name="GRIDUsername">The username to use to fetch the password for the voms proxy file</param>
        /// <param name="voms">The name of the voms to connect to</param>
        /// <param name="dumpOnly">Only print out proposed commands</param>
        /// <returns>Connection on which the grid is setup and ready to go</returns>
        public static async Task<ISSHConnection> VomsProxyInitAsync(this ISSHConnection connection, string voms, Func<bool> failNow = null, bool dumpOnly = false)
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

            var r = await connection
                .ExecuteLinuxCommandAsync(string.Format("echo {0} | voms-proxy-init -voms {1}", passwordInfo.Password, voms),
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
                error.AppendLine($"Failed to get the proxy ({connection.MachineName}): ");
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
        /// <param name="dataSetName">The rucio dataset name</param>
        /// <param name="localDirectory">The local directory (on Linux) where the file should be downloaded</param>
        /// <param name="fileStatus">Gets updates as new files are downloaded. This will contain just the filename.</param>
        /// <param name="fileNameFilter">Filter function to alter the files that are to be downloaded</param>
        /// <param name="failNow">Checked periodically, if ever returns true, then bail out</param>
        /// <param name="timeout">How long before we should timeout in seconds</param>
        /// <returns>The connections used so you can chain</returns>
        public static ISSHConnection DownloadFromGRID(this ISSHConnection connection, string dataSetName, string localDirectory,
            Action<string> fileStatus = null,
            Func<string[], string[]> fileNameFilter = null,
            Func<bool> failNow = null,
            int timeout = 3600)
        {
            return connection.DownloadFromGRIDAsync(dataSetName, localDirectory, fileStatus, fileNameFilter, failNow, timeout)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Fetch a dataset from the grid using Rucio to a local directory.
        /// </summary>
        /// <param name="connection">A previously configured connection with everything ready to go for GRID access.</param>
        /// <param name="dataSetName">The rucio dataset name</param>
        /// <param name="localDirectory">The local directory (on Linux) where the file should be downloaded</param>
        /// <param name="fileStatus">Gets updates as new files are downloaded. This will contain just the filename.</param>
        /// <param name="fileNameFilter">Filter function to alter the files that are to be downloaded</param>
        /// <param name="failNow">Checked periodically, if ever returns true, then bail out</param>
        /// <param name="timeout">How long before we should timeout in seconds</param>
        /// <returns>The connections used so you can chain</returns>
        public static async Task<ISSHConnection> DownloadFromGRIDAsync(this ISSHConnection connection, string dataSetName, string localDirectory,
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
            await connection.ExecuteLinuxCommandAsync(string.Format("rucio ls {0}", dataSetName), l => response.Add(l), secondsTimeout: 60, failNow: failNow);

            var dsnames = response
                .Where(l => l.Contains("DATASET") | l.Contains("CONTAINER"))
                .Select(l => l.Split(' '))
                .Where(sl => sl.Length > 1)
                .Select(sl => sl[1])
                .ToArray();

            if (!dsnames.Where(n => n.SantizeDSName() == dataSetName.SantizeDSName()).Any())
            {
                throw new ArgumentException(string.Format("Unable to find any datasets on the GRID (in rucuio) with the name '{0}'.", dataSetName));
            }

            // Get the complete list of files in the dataset.
            if (fileStatus != null)
            {
                fileStatus("Getting the complete list of files from the dataset");
            }
            var fileNameList = await connection.FilelistFromGRIDAsync(dataSetName, failNow: failNow);

            // Filter them if need be.
            var goodFiles = fileNameFilter != null
                ? fileNameFilter(fileNameList)
                : fileNameList;

            // Create a file that contains all the files we want to download up on the host.
            var fileListName = string.Format("/tmp/{0}.filelist", dataSetName.SantizeDSName());
            await connection.ExecuteLinuxCommandAsync("rm -rf " + fileListName);
            await connection.ApplyAsync(goodFiles, async (c, fname) => await c.ExecuteLinuxCommandAsync(string.Format("echo {0} >> {1}", fname, fileListName)));

            // We good on creating the directory?
            await connection.ExecuteLinuxCommandAsync(string.Format("mkdir -p {0}", localDirectory),
                l => { throw new ArgumentException($"Error trying to create directory {0} for dataset on remote machine ({connection.MachineName}).", localDirectory); },
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
            fileStatus?.Invoke($"Starting GRID download of {dataSetName}...");

            await Policy
                .Handle<ClockSkewException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(10),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1),
                    TimeSpan.FromMinutes(1)
                }, (e, ts) => { fileStatus?.Invoke("Clock Skew error - wait and re-try"); })
                .ExecuteAsync(() => DoRucioDownloadAsync(connection, localDirectory, fileStatus, failNow, timeout, fileListName));
            return connection;
        }

        private static async Task DoRucioDownloadAsync(ISSHConnection connection, string localDirectory, Action<string> fileStatus, Func<bool> failNow, int timeout, string fileListName)
        {
            string filesThatFailedToDownload = "";
            bool foundClockSkewMessage = false;
            var messageNameMatch = new Regex(@"Starting the download of ([\w\.:]+)");
            await connection.ExecuteLinuxCommandAsync($"rucio -T {timeout} download --dir {localDirectory} `cat {fileListName}`", l =>
            {
                // Look for something that indicates which file we are currently getting from the GRID.
                if (fileStatus != null)
                {
                    var m = messageNameMatch.Match(l);
                    if (m.Success)
                    {
                        fileStatus(m.Groups[1].Value);
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
                    throw new ClockSkewException($"Failed to download {filesThatFailedToDownload} files due to clock skew. Please double check ({connection.MachineName})!");
                }

                // Something else - will likely require a human to get involved.
                throw new FileFailedToDownloadException($"Failed to download all the files from the GRID - {filesThatFailedToDownload} files failed to download ({connection.MachineName})!");
            }
        }

        /// <summary>
        /// Returns the name of the files in a GRID dataset.
        /// </summary>
        /// <param name="connection">The already setup SSH connection</param>
        /// <param name="dataSetName">The dataset that we are to query</param>
        /// <param name="failNow">Return true if long-running commands should quit right away</param>
        /// <param name="dumpOnly">If we are to only test-run, but not actually run.</param>
        /// <returns>List of filenames for this GRID dataset</returns>
        public static string[] FilelistFromGRID(this ISSHConnection connection, string dataSetName, Func<bool> failNow = null, bool dumpOnly = false)
        {
            return connection.FilelistFromGRIDAsync(dataSetName, failNow, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Returns the name of the files in a GRID dataset.
        /// </summary>
        /// <param name="connection">The already setup SSH connection</param>
        /// <param name="dataSetName">The dataset that we are to query</param>
        /// <param name="failNow">Return true if long-running commands should quit right away</param>
        /// <param name="dumpOnly">If we are to only test-run, but not actually run.</param>
        /// <returns>List of filenames for this GRID dataset</returns>
        public static async Task<string[]> FilelistFromGRIDAsync(this ISSHConnection connection, string dataSetName, Func<bool> failNow = null, bool dumpOnly = false)
        {
            return (await connection.FileInfoFromGRIDAsync(dataSetName, failNow, dumpOnly))
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
        /// <param name="dataSetName"></param>
        /// <param name="dumpOnly">Dump only commands that are issues to standard logging interface.</param>
        /// <param name="failNow">Returns true to bail out as quickly as possible</param>
        /// <returns></returns>
        public static IReadOnlyList<GRIDFileInfo> FileInfoFromGRID(this ISSHConnection connection, string dataSetName, Func<bool> failNow = null, bool dumpOnly = false) => 
            FileInfoFromGRIDAsync(connection, dataSetName, failNow, dumpOnly)
                .WaitAndUnwrapException();

        /// <summary>
        /// Returns the list of files associated with a dataset, as fetched from the grid.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dataSetName"></param>
        /// <param name="dumpOnly">Dump only commands that are issues to standard logging interface.</param>
        /// <param name="failNow">Returns true to bail out as quickly as possible</param>
        /// <returns></returns>
        public static async Task<List<GRIDFileInfo>> FileInfoFromGRIDAsync(this ISSHConnection connection, string dataSetName, Func<bool> failNow =null, bool dumpOnly = false)
        {
            // If we have a cache hit, then avoid the really slow lookup.
            var c = _GRIDFileInfoCache.Value[dataSetName] as GRIDFileInfo[];
            if (c != null)
            {
                return c.ToList(); ;
            }

            // Run it in rucio and bring back our answers.
            var fileNameList = new List<GRIDFileInfo>();
            var filenameMatch = new Regex(@"\| +(?<fname>\S*) +\| +[^\|]+\| +[^\|]+\| +(?<fsize>[0-9\.]*) *(?<fsizeunits>[kMGTB]*) *\| +(?<events>[0-9]*) +\|");
            bool bad = false;
            try
            {
                await connection.ExecuteLinuxCommandAsync(string.Format("rucio list-files {0}", dataSetName), l =>
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
            }
            catch (LinuxCommandErrorException e)
                when (e.Message.Contains("status error") && bad)
            {
                // Swallow a command status error that we "sort-of" know about.
            }

            if (bad & !dumpOnly)
            {
                throw new DataSetDoesNotExistException($"Dataset '{dataSetName}' does not exist - can't get its list of files ({connection.MachineName}).");
            }

            _GRIDFileInfoCache.Value[dataSetName] = fileNameList.ToArray();
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
            if (string.IsNullOrWhiteSpace(number))
            {
                return 0;
            }
            var n = double.Parse(number);
            if (string.IsNullOrWhiteSpace(units))
            {
                return (n / 1024 / 1024);
            } else if (units == "kB")
            {
                return n / 1024;
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
        /// <param name="dumpOnly">Dump commands to standard logging, but do not execute anything</param>
        /// <returns></returns>
        public static ISSHConnection SetupRcRelease(this ISSHConnection connection, string linuxLocation, string releaseName, bool dumpOnly = false)
        {
            return connection.SetupRcReleaseAsync(linuxLocation, releaseName, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Setup a release. If it can't be found, error will be thrown
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="releaseName">Full release name (e.g. 'Base,2.3.30')</param>
        /// <param name="linuxLocation">Directory where the linux location can be found</param>
        /// <param name="dumpOnly">Dump commands to standard logging, but do not execute anything</param>
        /// <returns></returns>
        public static async Task<ISSHConnection> SetupRcReleaseAsync(this ISSHConnection connection, string linuxLocation, string releaseName, bool dumpOnly = false)
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
            await connection.ExecuteLinuxCommandAsync(string.Format("mkdir {0}", linuxLocation), l =>
            {
                dirCreated = !dirCreated ? false : string.IsNullOrWhiteSpace(l);
                dirAlreadyExists = dirAlreadyExists ? true : l.Contains("File exists");
            }, dumpOnly: dumpOnly);
            if (dirAlreadyExists)
                throw new LinuxMissingConfigurationException($"Release directory '{linuxLocation}' already exists - we need a fresh start ({connection.MachineName})");
            if (!dirCreated)
                throw new LinuxMissingConfigurationException(string.Format($"Unable to create release directory '{linuxLocation}' ({connection.MachineName})."));

            // Next, put our selves there
            await connection.ExecuteLinuxCommandAsync(string.Format("cd {0}", linuxLocation), dumpOnly: dumpOnly);

            // And then do the setup
            bool found = false;
            await connection.ExecuteLinuxCommandAsync(string.Format("rcSetup {0}", releaseName), l => found = found ? true : l.Contains("Found ASG release with"), dumpOnly: dumpOnly);
            if (!found && !dumpOnly)
                throw new LinuxMissingConfigurationException($"Unable to find release '{releaseName}' ({connection.MachineName})");

            // Return the connection to make it a functional interface.
            return connection;
        }

        /// <summary>
        /// Execute a kinit
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="dumpOnly">Dump commands to logging interface rather than execute them</param>
        /// <returns></returns>
        public static ISSHConnection Kinit(this ISSHConnection connection, string userName, string password, bool dumpOnly = false)
        {
            return connection.KinitAsync(userName, password, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Execute a kinit
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="dumpOnly">Dump commands to logging interface rather than execute them</param>
        /// <returns></returns>
        public static async Task<ISSHConnection> KinitAsync (this ISSHConnection connection, string userName, string password, bool dumpOnly = false)
        {
            var allStrings = new List<string>();
            await connection.ExecuteLinuxCommandAsync(string.Format("echo {0} | kinit {1}", password, userName), l => allStrings.Add(l), dumpOnly: dumpOnly);
            var errorStrings = allStrings.Where(l => l.StartsWith("kinit:")).ToArray();
            if (errorStrings.Length > 0)
            {
                throw new LinuxCommandErrorException($"Failed to execute kinit command: {errorStrings[0]} ({connection.MachineName})");
            }
            if (!allStrings.Where(l => l.StartsWith("Password for")).Any() && !dumpOnly)
            {
                throw new LinuxCommandErrorException($"Failed to execute kinit command: {allStrings[0]} ({connection.MachineName})");
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
            return connection.CheckoutPackageAsync(scPackagePath, scRevision, failNow, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Check out SVN or a GIT package.
        /// </summary>
        /// <param name="connection">Connection on which we should be checking this out on</param>
        /// <param name="scPackagePath">The svn path to the package. Basically what you would hand to the rc checkout command. Nohting like "tags" or "trunk" is permitted.</param>
        /// <param name="scRevision">The revision number. A SVN revision number. If blank, then the version associated with the build is checked out.</param>
        /// <returns></returns>
        public static Task<ISSHConnection> CheckoutPackageAsync(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
        {
            if (string.IsNullOrWhiteSpace(scPackagePath))
            {
                throw new ArgumentNullException("scPackagePath", "Package must be a valid path to a source control repro!");
            }
            var isGit = scPackagePath.EndsWith(".git");
            return isGit
                ? connection.CheckoutPackageGitAsync(scPackagePath, scRevision, failNow, dumpOnly)
                : connection.CheckoutPackageSVNAsync(scPackagePath, scRevision, failNow, dumpOnly);
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
            return connection.CheckoutPackageGitAsync(scPackagePath, scRevision, failNow, dumpOnly)
                .WaitAndUnwrapException();
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
        public static async Task<ISSHConnection> CheckoutPackageGitAsync(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
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
            await connection.ExecuteLinuxCommandAsync($"git clone --recursive {scPackagePath}", refreshTimeout: true, secondsTimeout: 8*60, failNow: failNow, dumpOnly: dumpOnly);

            // Now, we have to move to that revision.
            var pkgName = Path.GetFileNameWithoutExtension(scPackagePath.Split('/').Last());

            string error = "";
            await connection.ExecuteLinuxCommandAsync($"cd {pkgName}; git checkout {scRevision}; cd ..", processLine: l => error = l.Contains("error") ? l : error);
            if (error.Length > 0)
            {
                throw new LinuxCommandErrorException($"Unable to check out package {scPackagePath} with SHA {scRevision} ({connection.MachineName}): {error}");
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
            return CheckoutPackageAsync(connection, scPackagePath, scRevision, failNow, dumpOnly)
                .WaitAndUnwrapException();
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
        public static async Task<ISSHConnection> CheckoutPackageSVNAsync(this ISSHConnection connection, string scPackagePath, string scRevision, Func<bool> failNow = null, bool dumpOnly = false)
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
            await connection.ExecuteLinuxCommandAsync(string.Format("rc checkout_pkg {0}", fullPackagePath), l => sawRevisionMessage = sawRevisionMessage ? true : l.Contains("Checked out revision"), secondsTimeout: 120, failNow: failNow, dumpOnly: dumpOnly);
            if (!sawRevisionMessage && !dumpOnly)
            {
                throw new LinuxCommandErrorException($"Unable to check out svn package {scPackagePath} ({connection.MachineName}).");
            }

            // If this was checked out to trunk, then we need to fix it up.
            if (!string.IsNullOrWhiteSpace(scRevision))
            {
                var packageName = scPackagePath.Split('/').Last();
                var checkoutName = fullPackagePath.Split('/').Last();
                bool lineSeen = false;
                await connection.ExecuteLinuxCommandAsync(string.Format("mv {0} {1}", checkoutName, packageName), l => lineSeen = true, dumpOnly: dumpOnly);
                if (lineSeen && !dumpOnly)
                {
                    throw new LinuxCommandErrorException($"Unable to rename the downloaded trunk directory for package '{scPackagePath}' ({connection.MachineName}).");
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
            int secondsTimeout = 60 * 60,
            bool refreshTimeout = false,
            Dictionary<string, string> seeAndRespond = null)
        {
            return connection.ExecuteLinuxCommandAsync(command, processLine, failNow, dumpOnly, secondsTimeout, refreshTimeout, seeAndRespond)
                .WaitAndUnwrapException();
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
        public static async Task<ISSHConnection> ExecuteLinuxCommandAsync(this ISSHConnection connection,
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
            try
            {
                await connection.ExecuteCommandAsync(command, processLine, failNow: failNow, dumpOnly: dumpOnly, seeAndRespond: seeAndRespond, refreshTimeout: refreshTimeout, secondsTimeout: secondsTimeout);
                await connection.ExecuteCommandAsync("echo $?", l => rtnValue = l, dumpOnly: dumpOnly);
            } catch (TimeoutException te)
            {
                throw new TimeoutException($"{te.Message} - While executing command {command} on {connection.MachineName}.", te);
            }

            if (rtnValue != "0" && !dumpOnly)
            {
                throw new LinuxCommandErrorException($"The remote command '{command}' return status error code '{rtnValue}' on {connection.MachineName}");
            }
            return connection;
        }

        /// <summary>
        /// Build the work area
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dumpOnly">Dump commands only to standard logging interface rather than execute them</param>
        /// <param name="failNow">Return true to abort right away</param>
        /// <returns></returns>
        public static ISSHConnection BuildWorkArea(this ISSHConnection connection, Func<bool> failNow = null, bool dumpOnly = false)
        {
            return connection.BuildWorkAreaAsync(failNow, dumpOnly)
                .WaitAndUnwrapException();
        }

        /// <summary>
        /// Build the work area
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="dumpOnly">Dump commands only to standard logging interface rather than execute them</param>
        /// <param name="failNow">Return true to abort right away</param>
        /// <returns></returns>
        public static async Task<ISSHConnection> BuildWorkAreaAsync(this ISSHConnection connection, Func<bool> failNow = null, bool dumpOnly = false)
        {
            string findPkgError = null;
            var buildLines = new List<string>();
            try
            {
                await connection.ExecuteLinuxCommandAsync("rc find_packages", l => findPkgError = l, failNow: failNow, dumpOnly: dumpOnly);
                return await connection.ExecuteLinuxCommandAsync("rc compile", l => buildLines.Add(l), failNow: failNow, dumpOnly: dumpOnly);
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
