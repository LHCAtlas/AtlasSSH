using AtlasSSH;
using AtlasWorkFlows.Utils;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// We represent a GRID endpoint, that we can access via an ssh connection.
    /// We are paired with a local Linux end point - basically a place where we can
    /// copy files and store them.
    /// </summary>
    class PlaceGRID : IPlace, IDisposable
    {
        private readonly AsyncLockedResourceHolder<PlaceLinuxRemote> _linuxRemote = new AsyncLockedResourceHolder<PlaceLinuxRemote>(null);

        /// <summary>
        /// Initialize the GRID location.
        /// </summary>
        /// <param name="name">Name of the GRID re-pro</param>
        /// <param name="linuxRemote">All GRID sites are paired with some sort of Linux local access where their downloads are sent to.</param>
        public PlaceGRID(string name, PlaceLinuxRemote linuxRemote)
        {
            Name = name;
            _linuxRemote.ResetValueAsync(l => linuxRemote).Wait();
            ResetConnectionsAsync()
                .Wait();
        }

        /// <summary>
        /// The connection to our remote end-point
        /// </summary>
        private readonly AsyncLockedResourceHolder<AsyncLazy<ISSHConnection>> _connection = new AsyncLockedResourceHolder<AsyncLazy<ISSHConnection>>(null);

        /// <summary>
        /// Close and reset the connection
        /// </summary>
        public async Task ResetConnectionsAsync(bool reAlloc)
        {
            await _connection.ResetValueAsync(async c =>
            {
                if (c != null && c.IsStarted)
                {
                    (await c).Dispose();
                }
                return reAlloc
                ? new AsyncLazy<ISSHConnection>(() => InitConnection(null), AsyncLazyFlags.RetryOnFailure)
                : null;
            });
        }

        /// <summary>
        /// Reset and reallocate the connection.
        /// </summary>
        /// <returns></returns>
        public async Task ResetConnectionsAsync()
        {
            await ResetConnectionsAsync(true);
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
        /// Track the other tunnel connections.
        /// </summary>
        private async Task<ISSHConnection> InitConnection(Action<string> statusUpdater, Func<bool> failNow = null)
        {
            if (statusUpdater != null)
                statusUpdater("Setting up GRID Environment");
            var r = await _linuxRemote.ApplyAsync(l => l.CloneConnection());
            await r.setupATLASAsync();
            await r.setupRucioAsync(r.UserName);
            await r.VomsProxyInitAsync("atlas", failNow: failNow);
            return r;
        }

        /// <summary>
        /// We are a GRID site, so a high data tier number.
        /// </summary>
        public int DataTier { get { return 100; } }

        /// <summary>
        /// No local files can be reached here!
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// The name of the GRID access point.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// We can't be copied to... yet.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return true; } }

        /// <summary>
        /// We can only source a copy to our partner place!
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            return _linuxRemote.ApplyAsync(l => destination == l).Result;
        }

        /// <summary>
        /// Since we can't create a GRID dataset, this is not supported!
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public Task CopyDataSetInfoAsync(string dsName, string[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// We can't copy data into the GRID.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public Task CopyFromAsync(IPlace origin, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// Download files from the grid to the linux local site.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public async Task CopyToAsync(IPlace destination, Uri[] uris, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeoutMinutes = 60)
        {
            if (await _linuxRemote.ApplyAsync(l => destination != l))
            {
                throw new ArgumentException($"Place {destination.Name} is not the correct partner for {Name}. Was expecting place {_linuxRemote.ApplyAsync(c => c.Name)}.");
            }

            // Look at all the files from each dataset.
            foreach (var dsGroup in uris.GroupBy(u => u.DatasetName()))
            {
                // First, move the catalog over
                var catalog = (await DatasetManager.ListOfFilenamesInDatasetAsync(dsGroup.Key, statusUpdate, failNow, probabalLocation: this))
                    .ThrowIfNull(() => new DataSetDoesNotExistException($"Dataset '{dsGroup.Key}' was not found in place {Name}."));
                await _linuxRemote.ApplyAsync(l => l.CopyDataSetInfoAsync(dsGroup.Key, catalog, statusUpdate));
                if (failNow.PCall(false))
                {
                    break;
                }

                // Next, run the download into the directory in the linux area where
                // everything should happen.
                var remoteLocation = await _linuxRemote.ApplyAsync(l => l.GetLinuxDatasetDirectoryPath(dsGroup.Key));
                var filesList = dsGroup.Select(u => u.DatasetFilename()).ToArray();
                await _connection.ApplyAsync(async c =>
                {
                    await (await c).DownloadFromGRIDAsync(dsGroup.Key, remoteLocation,
                        fileStatus: fname => statusUpdate($"Downloading {fname} from {Name}"),
                        failNow: failNow,
                        fileNameFilter: fdslist => fdslist.Where(f => filesList.Where(mfs => f.Contains(mfs)).Any()).ToArray(),
                        timeout: timeoutMinutes * 60
                        );
                    await _linuxRemote.ApplyAsync(l => l.DatasetFilesChanged(dsGroup.Key));
                });
            }
        }

        /// <summary>
        /// Get a list of files from the GRID for a dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns>List of the files, with namespace removed.</returns>
        /// <remarks>
        /// Consider all datasets on the GRID frozen, so once they have been downloaded
        /// we cache them locally.
        /// </remarks>
        public async Task<string[]> GetListOfFilesForDatasetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            try
            {
                return await NonNullCacheInDiskAsync("PlaceGRIDDSCatalog", dsname, async () =>
                {
                    try
                    {
                        statusUpdate.PCall($"Listing files in dataset ({Name})");
                        try
                        {
                            var files = await _connection.ApplyAsync(async c => await (await c).FilelistFromGRIDAsync(dsname, failNow: failNow));
                            return files
                                .Select(f => f.Split(':').Last())
                                .ToArray();
                        }
                        finally
                        {
                            if (failNow.PCall(false))
                            {
                                throw new Exception("Interrupt by user.");
                            }
                        }
                    }
                    catch (DataSetDoesNotExistException)
                    {
                        return null;
                    }
                });
            } catch (SocketException e)
            {
                // This means the remote machine is offline. So pretend we know nothing here.
                Trace.WriteLine($"Unable to connect to {Name}: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Since we aren't visible on windows, this is just not possible.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public Task<IEnumerable<Uri>> GetLocalFileLocationsAsync(IEnumerable<Uri> uris)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be directly accessed from Windows - so no local path names!");
        }

        /// <summary>
        /// Check to see if a particular file exists. As long as that file is a member
        /// of the dataset, then it will as the GRID has EVERYTHING. ;-)
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public async Task<bool> HasFileAsync(Uri u, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Get the list of files for the dataset and just look.
            // We use the GRID fetch explicitly here - so we can make sure that
            // we know if the files are there or are now gone from the GRID.
            var files = await GetListOfFilesForDatasetAsync(u.DatasetName(), statusUpdate, failNow);
            return files == null
                ? false
                : files.Contains(u.DatasetFilename());
        }
    }
}
