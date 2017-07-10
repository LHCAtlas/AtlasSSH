using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;
using System.IO;
using System.Net.Sockets;
using System.Diagnostics;

namespace AtlasWorkFlows.Locations
{

    [Serializable]
    public class MissingLinuxFileException : Exception
    {
        public MissingLinuxFileException() { }
        public MissingLinuxFileException(string message) : base(message) { }
        public MissingLinuxFileException(string message, Exception inner) : base(message, inner) { }
        protected MissingLinuxFileException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// This is a remote linux machine.
    /// - Files are accessible only as a repository - copy in or out
    /// - If the disks are available on SAMBA, then create a WindowsLocal as well
    /// - If GRID can download here, then that should also create a new place.
    /// </summary>
    class PlaceLinuxRemote : IPlace, ISCPTarget, IDisposable
    {
        /// <summary>
        /// How to get to the end point we are talking to. Might require some tunneling (if there are more than one).
        /// </summary>
        public SSHUtils.SSHHostPair[] RemoteHostInfo { get; private set; }

        /// <summary>
        /// Path in the Linux side of things where everything is.
        /// </summary>
        private string _remote_path;
        
        /// <summary>
        /// Create a new repro located on a Linux machine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remote_ipaddr"></param>
        /// <param name="username"></param>
        /// <param name="remote_path"></param>
        public PlaceLinuxRemote(string name, string remote_ipaddr, string username, string remote_path)
            : this(name, remote_path, new SSHUtils.SSHHostPair[] { new SSHUtils.SSHHostPair() { Host = remote_ipaddr, Username = username } })
        {
        }

        /// <summary>
        /// Create the connection based on 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remote_path"></param>
        /// <param name="remoteHostAndTunnels"></param>
        public PlaceLinuxRemote(string name, string remote_path, SSHUtils.SSHHostPair[] remoteHostAndTunnels)
        {
            Name = name;
            _remote_path = remote_path;
            DataTier = 50;

            RemoteHostInfo = remoteHostAndTunnels;
            _connection = null;
            ResetConnections();
        }

        /// <summary>
        /// Track any sub-ssh shells we've formed.
        /// </summary>
        private List<IDisposable> _tunnelConnections = null;

        /// <summary>
        /// Close and reset the connection
        /// </summary>
        public void ResetConnections(bool reAlloc)
        {
            if (_tunnelConnections != null)
            {
                foreach (var c in _tunnelConnections.Reverse<IDisposable>())
                {
                    c.Dispose();
                }
                _tunnelConnections = null;
            }
            if (_connection != null && _connection.IsValueCreated)
            {
                _connection.Value.Dispose();
                _connection = null;
            }
            if (reAlloc)
            {
                _connection = new Lazy<SSHConnection>(() =>
                {
                    var r = RemoteHostInfo.MakeConnection();
                    _tunnelConnections = r.Item2;
                    return r.Item1;
                });
            }
        }
        public void ResetConnections()
        {
            ResetConnections(true);
        }

        /// <summary>
        /// When we are done, make sure to clean up all the resources we are holding on to.
        /// </summary>
        public void Dispose()
        {
            ResetConnections(false);
        }

        /// <summary>
        /// Get/Set the data tier. We default to 50 as we have no local access.
        /// </summary>
        public int DataTier { get; set; }

        /// <summary>
        /// We do not implement any local access from this end point
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// Get the name of this repro.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Does not need confirmation to use.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return false; } }

        /// <summary>
        /// The machine name for accessing this SCP end point.
        /// </summary>
        public string SCPMachineName { get { return RemoteHostInfo.Last().Host; } }

        /// <summary>
        /// The username for accessing this machine via SCP
        /// </summary>
        public string SCPUser { get { return RemoteHostInfo.Last().Username; } }

        /// <summary>
        /// We can start a copy from here to other places that have a SSH destination available.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            var scpTarget = destination as ISCPTarget;
            return !(scpTarget == null || !scpTarget.SCPIsVisibleFrom(SCPMachineName));
        }

        /// <summary>
        /// Start a copy from the <paramref name="origin"/> via SSH.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            // Make sure we have a target we can deal with for the copy.
            var scpTarget = origin as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {origin.Name} is not an SCP target.");
            }

            // Do things a single dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                if (failNow.PCall(false))
                {
                    break;
                }

                // Get the remote user, path, and password.
                var remoteUser = scpTarget.SCPUser;
                var remoteMachine = scpTarget.SCPMachineName;
                var passwd = GetPasswordForHost(remoteMachine, remoteUser);

                // Get the catalog over
                CopyDataSetInfo(fsGroup.Key,
                    DatasetManager.ListOfFilenamesInDataset(fsGroup.Key, statusUpdate, failNow, probabalLocation: origin),
                    statusUpdate, failNow);

                // The file path where we will store it all
                var destLocation = GetPathToCopyFiles(fsGroup.Key);

                // Next, queue up the copies, one at a time.
                foreach (var f in fsGroup)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }
                    var remoteLocation = scpTarget.GetSCPFilePath(f);
                    var fname = f.DatasetFilename();
                    var pname = $"{fname}.part";
                    statusUpdate.PCall($"Copy file {fname}: {origin.Name} -> {Name}");
                    _connection.Value.ExecuteLinuxCommand($"scp {remoteUser}@{remoteMachine}:{remoteLocation} {destLocation}/{pname}",
                        seeAndRespond: new Dictionary<string, string>() { {"password:", passwd } },
                        secondsTimeout: 5*60, refreshTimeout: true,
                        failNow: failNow);
                    _connection.Value.ExecuteLinuxCommand($"mv {destLocation}/{pname} {destLocation}/{fname}");
                }
            }
        }

        /// <summary>
        /// Returns the dataset path
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public string GetLinuxDatasetDirectoryPath(string dsname)
        {
            return $"{_remote_path}/{dsname}";
        }

        /// <summary>
        /// Extract a password in the standard way.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        private string GetPasswordForHost(string host, string username)
        {
            var sclist = new CredentialSet(host);
            var passwordInfo = sclist.Load().Where(c => c.Username == username).FirstOrDefault();
            if (passwordInfo == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, username));
            }

            return passwordInfo.Password;
        }

        /// <summary>
        /// Start a copy pushing data to a particular destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            // Make sure we have something we can deal with.
            var scpTarget = destination as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {destination.Name} is not an SCP target.");
            }

            // Do things one dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // Get the remote user, path, and password.
                var remoteUser = scpTarget.SCPUser;
                var remoteMachine = scpTarget.SCPMachineName;
                var passwd = GetPasswordForHost(remoteMachine, remoteUser);

                // Get the catalog over
                destination.CopyDataSetInfo(fsGroup.Key,
                    DatasetManager.ListOfFilenamesInDataset(fsGroup.Key, statusUpdate, failNow, probabalLocation: this),
                    statusUpdate, failNow);

                // The file path where we will store it all
                var destLocation = scpTarget.GetPathToCopyFiles(fsGroup.Key);

                // Next, queue up the copies, one at a time. Move to a part file and then do a rename.
                foreach (var f in fsGroup)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }

                    var localFilePath = GetSCPFilePath(f);
                    var fname = f.DatasetFilename();
                    var pname = $"{fname}.part";
                    statusUpdate.PCall($"Copying file {fname}: {Name} -> {destination.Name}");
                    _connection.Value.ExecuteLinuxCommand($"scp {localFilePath} {remoteUser}@{remoteMachine}:{destLocation}/{pname}",
                        seeAndRespond: new Dictionary<string, string>() { { "password:", passwd } },
                        secondsTimeout: 5 * 60, refreshTimeout: true, failNow: failNow);
                    _connection.Value.ExecuteLinuxCommand($"ssh {remoteUser}@{remoteMachine} mv {destLocation}/{pname} {destLocation}/{fname}",
                        seeAndRespond: new Dictionary<string, string>() { { "password:", passwd } },
                        secondsTimeout: 5 * 60, refreshTimeout: true, failNow: failNow);
                }
            }
        }

        /// <summary>
        /// Track the connection to the remote computer. Once opened we keep it open.
        /// </summary>
        private Lazy<SSHConnection> _connection;

        /// <summary>
        /// The full list of all files that belong to a particular dataset. This is regardless
        /// of weather or not the files are in this repro.
        /// </summary>
        /// <param name="cleanDSname">Name of the dataset we are looking at</param>
        /// <returns>List of the files in the dataset, or null if the dataset is not known in this repro</returns>
        public string[] GetListOfFilesForDataset(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            var cleanDSName = dsname.Replace("/", "");

            // The list of files for a dataset can't change over time, so we can
            // cache it locally.
            try
            {
                return NonNullCacheInDisk("PlaceLinuxDatasetFileList", cleanDSName, () =>
                {
                    statusUpdate.PCall($"Getting dataset file catalog ({Name})");
                    var files = new List<string>();
                    try
                    {
                        _connection.Value.ExecuteLinuxCommand($"cat {_remote_path}/{cleanDSName}/aa_dataset_complete_file_list.txt", l => files.Add(l), failNow: failNow);
                    }
                    catch (LinuxCommandErrorException) when (files.Count > 0 && files[0].Contains("aa_dataset_complete_file_list.txt: No such file or directory"))
                    {
                    // if there was an error accessing it, then it isn't there... Lets hope.
                    return null;
                    }

                    return files.Select(f => f.Split(':').Last().Trim()).ToArray();
                });
            } catch (SocketException e)
            {
                // Machine is offline - so just pretend we know nothing for now.
                Trace.WriteLine($"Socket error connecting to {Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Since we aren't local, we never return file locations. Always throw.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            throw new NotSupportedException($"The Linux dataset repository {Name} can't furnish local paths as it is on a remote machine!");
        }

        /// <summary>
        /// Track the locations of all the files for a particular dataset.
        /// </summary>
        private InMemoryObjectCache<string[]> _filePaths = new InMemoryObjectCache<string[]>();

        /// <summary>
        /// Someone has messed up our directory file for a dataset. We should uncache them as we will
        /// need to re-look at them.
        /// </summary>
        /// <param name="dsName"></param>
        internal void DatasetFilesChanged(string dsName)
        {
            _filePaths.Remove(dsName);
        }

        /// <summary>
        /// Fetch and return the full file paths of every file in the dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns>List of all Linux paths for files in the dataset. Returns the empty array if the dataset does not exist on the server.</returns>
        private string[] GetAbosluteLinuxFilePaths(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            try
            {
                return _filePaths.GetOrCalc(dsname, () =>
                {
                    try
                    {
                        statusUpdate.PCall($"Getting available files ({Name})");
                        var files = new List<string>();
                        _connection.Value.ExecuteLinuxCommand($"find {_remote_path}/{dsname} -print", l => files.Add(l), failNow: failNow);
                        return files.ToArray();
                    }
                    catch (LinuxCommandErrorException e) when (e.Message.Contains("return status error"))
                    {
                        return new string[0];
                    }
                });
            } catch (SocketException e)
            {
                Trace.WriteLine($"Unable to connect to {Name}: {e.Message}");
                return new string[0];
            }
        }

        /// <summary>
        /// See if we have a particular file in our dataset
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simpmle check.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"The uri '{u.OriginalString}' is not a gridds:// uri - can't map it to a file!");
            }

            return GetAbosluteLinuxFilePaths(u.DatasetName(), statusUpdate, failNow)
                .Select(f => f.Split('/').Last())
                .Where(f => f == u.DatasetFilename())
                .Any();
        }

        /// <summary>
        /// Return true if we can source a copy from this machine to or from that machine.
        /// </summary>
        /// <param name="internetLocation"></param>
        /// <returns></returns>
        public bool SCPIsVisibleFrom(string internetLocation)
        {
            // If we are globally visible. Currently we use a heuristic.
            return RemoteHostInfo.Length == 1; 
        }

        /// <summary>
        /// Get a path for a file.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public string GetSCPFilePath(Uri f)
        {
            var file = GetAbosluteLinuxFilePaths(f.DatasetName())
                .Where(rf => rf.EndsWith("/" + f.DatasetFilename()))
                .FirstOrDefault();
            if (file == null)
            {
                throw new MissingLinuxFileException($"Place {Name} was expected to have file {f}, but does not!");
            }

            return file;
        }

        /// <summary>
        /// Make this dataset known to us at the Linux repro
        /// </summary>
        /// <param name="key"></param>
        /// <param name="filesInDataset">List of files that are in this dataset.</param>
        public void CopyDataSetInfo(string key, string[] filesInDataset, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            if (filesInDataset == null)
            {
                throw new ArgumentNullException("List of files for the dataset can't be null");
            }

            // Prep for running
            var dsDir = $"{_remote_path}/{key}";
            var infoFile = $"{dsDir}/aa_dataset_complete_file_list.txt";
            var infoFilePart = $"{infoFile}.Part";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {dsDir}");
            _connection.Value.ExecuteLinuxCommand($"rm -rf {infoFilePart}");

            // Just add every file in.
            foreach (var f in filesInDataset)
            {
                if (failNow.PCall(false))
                {
                    return;
                }

                _connection.Value.ExecuteLinuxCommand($"echo {f} >> {infoFilePart}");
            }

            // Done - rename it so it takes up the official name for later use.
            _connection.Value.ExecuteLinuxCommand($"rm -rf {infoFile}");
            _connection.Value.ExecuteLinuxCommand($"mv {infoFilePart} {infoFile}");
        }

        /// <summary>
        /// Return the path where files can be copied. It must be ready for that too.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public string GetPathToCopyFiles(string dsname)
        {
            // Get the location, and make sure it exists.
            var copiedPath = $"{_remote_path}/{dsname}/copied";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {copiedPath}");

            // Assume we are going to update - so clear the internal cache.
            _filePaths.Remove(dsname);

            return copiedPath;
        }

        /// <summary>
        /// Copy files from here down.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="ourpath"></param>
        /// <remarks>
        /// Use our scp connection to do this.
        /// </remarks>
        public void CopyFromRemoteToLocal(string dsName, string[] files, DirectoryInfo ourpath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Turn them into linux file locations by doing matching. The files should be identical.
            var linuxLocations = GetAbosluteLinuxFilePaths(dsName, statusUpdate, failNow);
            var linuxFiles = files
                .Select(f => linuxLocations.Where(lx => lx.EndsWith("/" + f)).FirstOrDefault())
                .Throw<string>(s => s == null, s => new DatasetFileNotLocalException($"File '{s}' is not in place {Name}, so we can't copy it locally!"));

            // Do the files one at a time, and download them to a temp location and then move them
            // to the proper location. Saftey if we get cut off mid-move.
            foreach(var lx in linuxFiles)
            {
                if (failNow.PCall(false))
                {
                    break;
                }
                var fname = lx.Split('/').Last();
                var partName = $"{fname}.part";
                var partFileInfo = new FileInfo(Path.Combine(ourpath.FullName, partName));
                _connection.Value.CopyRemoteFileLocally(lx, partFileInfo, statusUpdate, failNow);
                partFileInfo.MoveTo(Path.Combine(ourpath.FullName, fname));
            }
        }

        /// <summary>
        /// Copy a file from a local location to a remote location via SCP.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public void CopyFromLocalToRemote(string dsName, IEnumerable<FileInfo> files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // The the dest direction setup
            var lxlocation = GetLinuxDatasetDirectoryPath(dsName);
            var copiedLocation = $"{lxlocation}/copied";
            _connection.Value.ExecuteLinuxCommand($"mkdir -p {copiedLocation}");

            // Now copy the files
            foreach (var f in files)
            {
                var partName = $"{f.Name}.part";
                _connection.Value.CopyLocalFileRemotely(f, $"{copiedLocation}/{partName}", statusUpdate, failNow);
                _connection.Value.ExecuteLinuxCommand($"mv {copiedLocation}/{partName} {copiedLocation}/{f.Name}");
            }
        }

    }
}
