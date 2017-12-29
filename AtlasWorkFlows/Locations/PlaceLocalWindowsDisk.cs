using AtlasWorkFlows.Utils;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// A local windows dataset directory
    /// </summary>
    class PlaceLocalWindowsDisk : IPlace
    {
        /// <summary>
        /// Initialize a windows repo for grid datasets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repoDir"></param>
        /// <param name="needsConfirmationOfCopy">If true, then the destination needs to be explicitly indicated, rather than part of the default route finding.</param>
        public PlaceLocalWindowsDisk(string name, DirectoryInfo repoDir, bool needsConfirmationOfCopy = true)
        {
            Name = name;
            _locationOfLocalRepro = repoDir;
            NeedsConfirmationCopy = needsConfirmationOfCopy;
        }

        /// <summary>
        /// Reset any connections we have - null operation for us as we have none!
        /// </summary>
        public Task ResetConnectionsAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Get the data tier - which is super fast and local.
        /// </summary>
        public int DataTier { get { return 1; } }

        /// <summary>
        /// By definition, if this is loaded, then it is pointing to a local directory.
        /// </summary>
        public bool IsLocal { get { return true; } }

        public string Name { get; private set; }

        /// <summary>
        /// By default, one can't copy to this location without explicitly mentioning it as
        /// a destination point.
        /// </summary>
        public bool NeedsConfirmationCopy { get; private set; }

        /// <summary>
        /// Location of the repro we are using
        /// </summary>
        private DirectoryInfo _locationOfLocalRepro;

        /// <summary>
        /// We can copy files from other locations.
        /// </summary>
        /// <param name="destination">The destination we need to copy from</param>
        /// <returns>true if a copy can be sourced from here or to here</returns>
        /// <remarks>
        /// We know how to do direct disk copies and also SCP's from visible machines.
        /// </remarks>
        public bool CanSourceCopy(IPlace destination)
        {
            // As long as it is another active location
            return destination is PlaceLocalWindowsDisk
                || ((destination is ISCPTarget) && ((ISCPTarget)destination).SCPIsVisibleFrom(IPLocationTests.FindLocalIpName()));
        }

        /// <summary>
        /// Copy from the other location. We must have already agreed to do the copy, so we know
        /// how to do the copy.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public async Task CopyFromAsync(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            if (origin is PlaceLocalWindowsDisk)
            {
                await CopyFromLocalDiskAsync(uris, origin as PlaceLocalWindowsDisk, statusUpdate, failNow);
            }
            else if (origin is ISCPTarget)
            {
                await CopyFromSCPTargetAsync(origin, uris, statusUpdate, failNow);
            }
            else
            {
                throw new ArgumentException($"Can CopyFrom only another PlaceLocalWindowsDisk or PlaceLinuxRemote - '{origin.Name}' isn't either - this is an internal error.");
            }
        }

        /// <summary>
        /// Copy vis SCP from the remote target.
        /// </summary>
        /// <param name="iSCPTarget"></param>
        /// <param name="uris"></param>
        private async Task CopyFromSCPTargetAsync(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            foreach (var dsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // Move the catalog over.
                var files = await DatasetManager.ListOfFilenamesInDatasetAsync(dsGroup.Key, statusUpdate, failNow, probabalLocation: origin);
                await CopyDataSetInfoAsync(dsGroup.Key, files, statusUpdate, failNow);

                // Now, do the files via SCP.
                var ourpath = new DirectoryInfo(Path.Combine(BuildDSRootDirectory(dsGroup.Key).FullName, "copied"));
                if (!ourpath.Exists)
                {
                    ourpath.Create();
                }
                await (origin as ISCPTarget).CopyFromRemoteToLocalAsync(dsGroup.Key, uris.Select(u => u.DatasetFilename()).ToArray(), ourpath, statusUpdate, failNow);
            }
        }

        /// <summary>
        /// Copy files from another local disk to this local disk.
        /// </summary>
        /// <param name="uris"></param>
        /// <param name="source"></param>
        private async Task CopyFromLocalDiskAsync(Uri[] uris, PlaceLocalWindowsDisk source, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Do the copy by dataset, which is our primary way of storing things here.
            var groupedByDS = uris.GroupBy(u => u.DatasetName());
            foreach (var dsFileListing in groupedByDS)
            {
                // Copy over the dataset info
                await CopyDataSetInfoAsync(dsFileListing.Key,
                    await DatasetManager.ListOfFilenamesInDatasetAsync(dsFileListing.Key, statusUpdate, failNow, probabalLocation: this),
                    statusUpdate, failNow);

                // For each file we don't have, do the copy. Checking for existance shouldn't
                // be necessary - it should have been done - but it is so cheap compared to the cost
                // of copying a 2 GB file...
                foreach (var f in dsFileListing)
                {
                    if (failNow.PCall(false))
                    {
                        break;
                    }
                    if (!(await HasFileAsync(f, statusUpdate, failNow)))
                    {
                        var destPath = new FileInfo(Path.Combine(BuildDSRootDirectory(f.DatasetName()).FullName, "copied", f.DatasetFilename()));
                        if (!destPath.Directory.Exists)
                        {
                            destPath.Directory.Create();
                        }
                        var destPathPart = new FileInfo($"{destPath.FullName}.part");
                        var sourcePath = new FileInfo((await source.GetLocalFileLocationsAsync(new Uri[] { f })).First().LocalPath);
                        statusUpdate.PCall($"Copying {sourcePath.Name}: {source.Name} -> {Name}");
                        int count = 0;
                        await Policy
                            .Handle<IOException>()
                            .WaitAndRetryForeverAsync((i, ctx) =>
                            {
                                count++;
                                statusUpdate.PCall($"Copying {sourcePath.Name}: {source.Name} -> {Name} (retry {count})");
                                return TimeSpan.FromSeconds(2);
                            })
                            .ExecuteAsync(async () => {
                                destPathPart.Refresh();
                                if (destPathPart.Exists)
                                {
                                    destPathPart.Delete();
                                }
                                await Task.Factory.StartNew(() => sourcePath.CopyTo(destPathPart.FullName));
                                });
                        if (destPath.Exists)
                        {
                            // No idea why it is already here - but replacing it anyway...
                            destPath.Delete();
                        }
                        destPathPart.MoveTo(destPath.FullName);
                    }
                }
            }
        }

        /// <summary>
        /// Copy the data to the other location.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public async Task CopyToAsync(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            if (destination is PlaceLocalWindowsDisk)
            {
                // It is symmetric when both have windows paths, so lets just do it the other way around.
                await (destination as PlaceLocalWindowsDisk).CopyFromAsync(this, uris, statusUpdate, failNow);
            }
            else if (destination is ISCPTarget)
            {
                await CopyToSCPTargetAsync(destination, uris, statusUpdate, failNow);
            }
            else
            {
                throw new ArgumentException($"Can CopyTo only another PlaceLocalWindowsDisk or PlaceLinuxRemote - '{destination.Name}' isn't either - this is an internal error.");
            }
        }

        /// <summary>
        /// Copy via SCP to the remote machine!
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        private async Task CopyToSCPTargetAsync(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            foreach (var dsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // Move the catalog over.
                var files = await DatasetManager.ListOfFilenamesInDatasetAsync(dsGroup.Key, statusUpdate, failNow, probabalLocation: this);
                await destination.CopyDataSetInfoAsync(dsGroup.Key, files);

                // Now, do the files via SCP.
                var localFiles = await GetLocalFileLocationsAsync(uris);
                await ((destination as ISCPTarget).CopyFromLocalToRemoteAsync(dsGroup.Key, localFiles.Select(u => new FileInfo(u.LocalPath)), statusUpdate, failNow));
            }
        }

        /// <summary>
        /// Look to see if we know about the dataset in our library, and if so, fetch the file list.
        /// </summary>
        /// <param name="dsname">Name of datest</param>
        /// <param name="statusUpdate">Update messages for long operations - but ignored here because we only open a file.</param>
        /// <returns>List of files, null if the dataset is not known.</returns>
        public Task<string[]> GetListOfFilesForDatasetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetGlobalConstants.DatasetFileList));
            if (!f.Exists)
            {
                return Task.FromResult<string[]>(null);
            }

            var files = new List<string>();
            statusUpdate.PCall($"Reading catalog file ({Name})");
            int count = 0;
            Policy
                .Handle<IOException>()
                .WaitAndRetryForever((i, ctx) =>
                {
                    count++;
                    statusUpdate.PCall($"Reading catalog file ({Name}) (retry count {count})");
                    return TimeSpan.FromSeconds(2);
                })
                .Execute(() =>
                {
                    using (var rd = f.OpenText())
                    {
                        while (!rd.EndOfStream)
                        {
                            var line = rd.ReadLine();
                            if (!string.IsNullOrWhiteSpace(line))
                            {
                                line = line.Contains(":") ? line.Substring(line.IndexOf(":") + 1) : line;
                                files.Add(line);
                            }
                        }
                    }
                });
            return Task.FromResult(files.ToArray());
        }

        /// <summary>
        /// Make this dataset known to the repository.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public Task CopyDataSetInfoAsync(string dsName, string[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsName).FullName, DatasetGlobalConstants.DatasetFileList));
            if (!f.Directory.Exists)
            {
                f.Directory.Create();
            }

            statusUpdate.PCall($"Updating dataset catalog ({Name})");
            using (var write = f.CreateText())
            {
                foreach (var dsFile in files)
                {
                    write.WriteLine(dsFile);
                }
                write.Close();
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Returns true if we at least know about this dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private bool HasDS(string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetGlobalConstants.DatasetFileList));
            return f.Exists;
        }

        /// <summary>
        /// Return the root directory for a particular dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns>Directory where files can be written and read. Does not create the directory if it doesn't exist.</returns>
        private DirectoryInfo BuildDSRootDirectory(string dsname)
        {
            return new DirectoryInfo(Path.Combine(_locationOfLocalRepro.FullName, dsname.SantizeDSName()));
        }

        /// <summary>
        /// Get the absolute file locations from the source dataset. Throws if the file does not exist locally.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public Task<IEnumerable<Uri>> GetLocalFileLocationsAsync(IEnumerable<Uri> uris)
        {
            // Do it by dataset.
            var dsGroups = (from u in uris
                            let ds = u.DatasetName()
                            let fname = u.DatasetFilename()
                            group new { FileName = fname } by ds)
                           .Throw(g => !HasDS(g.Key), g => new DatasetDoesNotExistInThisReproException($"Dataset '{g.Key}' does not exists in repro {Name}"));

            var fileUris = from dsFiles in dsGroups
                           let flist = FindAllFilesOnDisk(dsFiles.Key)
                           from u in dsFiles
                           select new { matchedUri = flist.Where(fu => fu.Segments.Last() == u.FileName).FirstOrDefault(), OrigUri = u };

            return Task.FromResult(fileUris
                .Throw(o => o.matchedUri == null, o => new DatasetFileNotLocalException($"File {o.OrigUri} does not exist locally!"))
                .Select(o => o.matchedUri));
        }

        /// <summary>
        /// Return all files in the dataset. This will include everything on disk (including parts and other things!).
        /// NOTE: Do not return 
        /// </summary>
        /// <param name="dsName"></param>
        /// <returns>List of every file</returns>
        private Uri[] FindAllFilesOnDisk(string dsName, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            var dir = BuildDSRootDirectory(dsName);
            return dir.EnumerateFiles("*", SearchOption.AllDirectories)
                .Select(f => new UriBuilder() { Scheme = "file", Host="", Path = f.FullName }.Uri)
                .ToArray();
        }

        /// <summary>
        /// Do we have access to this specific file in this dataset? If this dataset is not here, then we throw an exception.
        /// </summary>
        /// <param name="u">URI of the file in gridds:// format. It is assumed this file is part of the dataset</param>
        /// <returns>true if the file exists on the local disk. False otherwise</returns>
        public Task<bool> HasFileAsync(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simple checks.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"Uri {u} does not have a gridds:// scheme!");
            }

            var ds = u.DatasetName();
            if (!HasDS(ds))
            {
                return Task.FromResult(false);
            }

            // See if any of the files on disk match the file we need to look at.
            var allLocalFiles = FindAllFilesOnDisk(ds, statusUpdate, failNow);
            return Task.FromResult(allLocalFiles.Select(lf => lf.Segments.Last() == u.DatasetFilename()).Any(t => t));
        }
    }
}
