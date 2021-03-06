﻿using AtlasSSH;
using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using static AtlasSSH.CredentialUtils;
using static AtlasSSH.DiskCacheTypedHelpers;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// File is missign up on Linux where we expected it.
    /// </summary>
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
        /// Path in the Linux side of things where everything is.
        /// </summary>
        private string _remote_path;

        /// <summary>
        /// Track the connection string to get to the remote host.
        /// </summary>
        private string _connection_string;

        /// <summary>
        /// Track the connection to the remote computer. Once opened we keep it open.
        /// </summary>
        /// <remarks>
        /// Access is locked as there are times where multiple things are run (like many HasFileAsync's!).
        /// </remarks>
        private readonly AsyncLockedResourceHolder<Lazy<ISSHConnection>> _connection = new AsyncLockedResourceHolder<Lazy<ISSHConnection>>(null);

        /// <summary>
        /// Create the connection based on 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remote_path"></param>
        /// <param name="connection_string">String we should connect to "user@host -> user@host"</param>
        public PlaceLinuxRemote(string name, string remote_path, string connection_string)
        {
            Name = name;
            _remote_path = remote_path;
            DataTier = 50;

            _connection_string = connection_string;
            ResetConnectionsAsync()
                .Wait();
        }

        /// <summary>
        /// Return a new SSH connection that points to the same place we are using
        /// for the grid internally.
        /// </summary>
        /// <returns></returns>
        internal ISSHConnection CloneConnection()
        {
            return new SSHConnectionTunnel(_connection_string);
        }

        /// <summary>
        /// Close and reset the connection
        /// </summary>
        public async Task ResetConnectionsAsync(bool reAlloc)
        {
            await _connection.ResetValueAsync(c =>
            {
                if (c != null && c.IsValueCreated)
                {
                    c.Value.Dispose();
                }

                if (reAlloc)
                {
                    return Task.FromResult(
                        new Lazy<ISSHConnection>(() =>
                            {
                                return new SSHRecoveringConnection(() => new SSHConnectionTunnel(_connection_string));
                            }));
                }
                else
                {
                    return Task.FromResult<Lazy<ISSHConnection>>(null);
                }
            });
        }

        public Task ResetConnectionsAsync()
        {
            return ResetConnectionsAsync(true);
        }

        /// <summary>
        /// When we are done, make sure to clean up all the resources we are holding on to.
        /// </summary>
        public void Dispose()
        {
            ResetConnectionsAsync(false)
                .Wait();
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
        public string SCPMachineName { get { return _connection.ApplyAsync(c => c.Value.MachineName).Result; } }

        /// <summary>
        /// The username for accessing this machine via SCP
        /// </summary>
        public string SCPUser { get { return _connection.ApplyAsync(c => c.Value.UserName).Result; } }

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
        public async Task CopyFromAsync(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            // Make sure we have a target we can deal with for the copy.
            var scpTarget = origin as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {origin.Name} is not an SCP target.");
            }

            // Do things a single dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DataSetName()))
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
                await CopyDataSetInfoAsync(fsGroup.Key,
                    await DataSetManager.ListOfFilenamesInDatasetAsync(fsGroup.Key, statusUpdate, failNow, probabalLocation: origin),
                    statusUpdate, failNow);

                // The file path where we will store it all
                var destLocation = await GetPathToCopyFilesAsync(fsGroup.Key);

                // Next, queue up and run the copies, one at a time. 
                foreach (var f in fsGroup)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }
                    var remoteLocation = await scpTarget.GetSCPFilePathAsync(f);
                    var fname = f.DataSetFileName();
                    var pname = $"{fname}.part";
                    statusUpdate.PCall($"Copy file {fname}: {origin.Name} -> {Name}");
                    await _connection.ApplyAsync(async c =>
                    {
                        await c.Value.ExecuteLinuxCommandAsync($"scp {remoteUser}@{remoteMachine}:{remoteLocation} {destLocation}/{pname}",
                            seeAndRespond: new Dictionary<string, string>() { { "password:", passwd } },
                            secondsTimeout: 5 * 60, refreshTimeout: true,
                            failNow: failNow);
                        await c.Value.ExecuteLinuxCommandAsync($"mv {destLocation}/{pname} {destLocation}/{fname}");
                    });
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
        private static string GetPasswordForHost(string host, string username)
        {
            var password = FetchUserCredentials(host, username);
            if (password == null)
            {
                throw new ArgumentException(string.Format("Please create a generic windows credential with '{0}' as the target address, '{1}' as the username, and the password for remote SSH access to that machine.", host, username));
            }

            return password;
        }

        /// <summary>
        /// Start a copy pushing data to a particular destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public async Task CopyToAsync(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            // Make sure we have something we can deal with.
            var scpTarget = destination as ISCPTarget;
            if (scpTarget == null)
            {
                throw new ArgumentException($"Place {destination.Name} is not an SCP target.");
            }

            // Do things one dataset at a time.
            foreach (var fsGroup in uris.GroupBy(u => u.DataSetName()))
            {
                // Get the remote user, path, and password.
                var remoteUser = scpTarget.SCPUser;
                var remoteMachine = scpTarget.SCPMachineName;
                var passwd = GetPasswordForHost(remoteMachine, remoteUser);

                // Get the catalog over
                await destination.CopyDataSetInfoAsync(fsGroup.Key,
                    await DataSetManager.ListOfFilenamesInDatasetAsync(fsGroup.Key, statusUpdate, failNow, probabalLocation: this),
                    statusUpdate, failNow);

                // The file path where we will store it all
                var destLocation = await scpTarget.GetPathToCopyFilesAsync(fsGroup.Key);

                // Next, queue up the copies, one at a time. Move to a part file and then do a rename.
                foreach (var f in fsGroup)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }

                    var localFilePath = await GetSCPFilePathAsync(f);
                    var fname = f.DataSetFileName();
                    var pname = $"{fname}.part";
                    statusUpdate.PCall($"Copying file {fname}: {Name} -> {destination.Name}");
                    await _connection.ApplyAsync(async c =>
                    {
                        await c.Value.ExecuteLinuxCommandAsync($"scp {localFilePath} {remoteUser}@{remoteMachine}:{destLocation}/{pname}",
                            seeAndRespond: new Dictionary<string, string>() { { "password:", passwd } },
                            secondsTimeout: 5 * 60, refreshTimeout: true, failNow: failNow);
                        await c.Value.ExecuteLinuxCommandAsync($"ssh {remoteUser}@{remoteMachine} mv {destLocation}/{pname} {destLocation}/{fname}",
                            seeAndRespond: new Dictionary<string, string>() { { "password:", passwd } },
                            secondsTimeout: 5 * 60, refreshTimeout: true, failNow: failNow);
                    });
                }
            }
        }

        /// <summary>
        /// The full list of all files that belong to a particular dataset. This is regardless
        /// of weather or not the files are in this repro.
        /// </summary>
        /// <param name="cleanDSname">Name of the dataset we are looking at</param>
        /// <returns>List of the files in the dataset, or null if the dataset is not known in this repro</returns>
        public async Task<string[]> GetListOfFilesForDataSetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            var cleanDSName = dsname.Replace("/", "");

            // The list of files for a dataset can't change over time, so we can
            // cache it locally.
            try
            {
                return await NonNullCacheInDiskAsync("PlaceLinuxDatasetFileList", cleanDSName, async () =>
                {
                    statusUpdate.PCall($"Getting dataset file catalog ({Name})");
                    var files = new List<string>();
                    try
                    {
                        await _connection.ApplyAsync(c => c.Value.ExecuteLinuxCommandAsync($"cat {_remote_path}/{cleanDSName}/aa_dataset_complete_file_list.txt", l => files.Add(l), failNow: failNow));
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
        public async Task<IEnumerable<Uri>> GetLocalFileLocationsAsync(IEnumerable<Uri> uris)
        {
            var r = uris
                .Select(async u => (await GetAbsoluteLinuxFilePathsAsync(u.DataSetName()))
                            .Where(uf => uf.Split('/').Last() == u.DataSetFileName())
                            .FirstOrDefault()
                            .ThrowIfNull(() => new ArgumentException("Unable to find one of the files at this location!")))
                .Select(async p => new Uri($"file://{Name}{await p}"));
            return await Task.WhenAll(r);
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
        private async Task<string[]> GetAbsoluteLinuxFilePathsAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            try
            {
                return await _filePaths.GetOrCalcAsync(dsname, async () =>
                {
                    try
                    {
                        statusUpdate.PCall($"Getting available files ({Name})");
                        var files = new List<string>();
                        await _connection.ApplyAsync(c => c.Value.ExecuteLinuxCommandAsync($"find {_remote_path}/{dsname} -print", l => files.Add(l), failNow: failNow));
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
        public async Task<bool> HasFileAsync(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simpmle check.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"The uri '{u.OriginalString}' is not a gridds:// uri - can't map it to a file!");
            }

            return (await GetAbsoluteLinuxFilePathsAsync(u.DataSetName(), statusUpdate, failNow))
                .Select(f => f.Split('/').Last())
                .Where(f => f == u.DataSetFileName())
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
            return _connection.ApplyAsync(c => c.Value.GloballyVisible).Result; 
        }

        /// <summary>
        /// Get a path for a file.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public async Task<string> GetSCPFilePathAsync(Uri f)
        {
            var file = (await GetAbsoluteLinuxFilePathsAsync(f.DataSetName()))
                .Where(rf => rf.EndsWith("/" + f.DataSetFileName()))
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
        public async Task CopyDataSetInfoAsync(string key, string[] filesInDataset, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            if (filesInDataset == null)
            {
                throw new ArgumentNullException("List of files for the dataset can't be null");
            }

            // Prep for running
            var dsDir = $"{_remote_path}/{key}";
            var infoFile = $"{dsDir}/aa_dataset_complete_file_list.txt";
            var infoFilePart = $"{infoFile}.Part";

            await _connection.ApplyAsync(async c =>
            {
                await c.Value.ExecuteLinuxCommandAsync($"mkdir -p {dsDir}");
                await c.Value.ExecuteLinuxCommandAsync($"rm -rf {infoFilePart}");

                // Just add every file in.
                foreach (var f in filesInDataset)
                {
                    if (failNow.PCall(false))
                    {
                        return;
                    }

                    await c.Value.ExecuteLinuxCommandAsync($"echo {f} >> {infoFilePart}");
                }

                // Done - rename it so it takes up the official name for later use.
                await c.Value.ExecuteLinuxCommandAsync($"rm -rf {infoFile}");
                await c.Value.ExecuteLinuxCommandAsync($"mv {infoFilePart} {infoFile}");
            });
        }

        /// <summary>
        /// Return the path where files can be copied. It must be ready for that too.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public async Task<string> GetPathToCopyFilesAsync(string dsname)
        {
            // Get the location, and make sure it exists.
            var copiedPath = $"{_remote_path}/{dsname}/copied";
            await _connection.ApplyAsync(c => c.Value.ExecuteLinuxCommandAsync($"mkdir -p {copiedPath}"));

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
        public async Task CopyFromRemoteToLocalAsync(string dsName, string[] files, DirectoryInfo ourpath, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Turn them into linux file locations by doing matching. The files should be identical.
            var linuxLocations = await GetAbsoluteLinuxFilePathsAsync(dsName, statusUpdate, failNow);
            var linuxFiles = files
                .Select(f => linuxLocations.Where(lx => lx.EndsWith("/" + f)).FirstOrDefault())
                .Throw<string>(s => s == null, s => new DataSetFileNotLocalException($"File '{s}' is not in place {Name}, so we can't copy it locally!"));

            // Do the files one at a time, and download them to a temp location and then move them
            // to the proper location. Saftey if we get cut off mid-move.
            await _connection.ApplyAsync(async c =>
            {
                foreach (var lx in linuxFiles)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }
                    var fname = lx.Split('/').Last();
                    var partName = $"{fname}.part";
                    var partFileInfo = new FileInfo(Path.Combine(ourpath.FullName, partName));
                    await c.Value.CopyRemoteFileLocallyAsync(lx, partFileInfo, statusUpdate, failNow);
                    partFileInfo.MoveTo(Path.Combine(ourpath.FullName, fname));
                }
            });
        }

        /// <summary>
        /// Copy a file from a local location to a remote location via SCP.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public async Task CopyFromLocalToRemoteAsync(string dsName, IEnumerable<FileInfo> files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // The the dest direction setup
            var lxlocation = GetLinuxDatasetDirectoryPath(dsName);
            var copiedLocation = $"{lxlocation}/copied";
            await _connection.ApplyAsync(async c =>
            {
                await c.Value.ExecuteLinuxCommandAsync($"mkdir -p {copiedLocation}");

                // Now copy the files
                foreach (var f in files)
                {
                    var partName = $"{f.Name}.part";
                    await c.Value.CopyLocalFileRemotelyAsync(f, $"{copiedLocation}/{partName}", statusUpdate, failNow);
                    await c.Value.ExecuteLinuxCommandAsync($"mv {copiedLocation}/{partName} {copiedLocation}/{f.Name}");
                }
            });
        }

    }
}
