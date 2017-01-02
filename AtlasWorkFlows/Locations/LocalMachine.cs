using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AtlasWorkFlows.Utils;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Represents a cache of files on the local machine. If a GetDS is issued on this repo, it will look for other
    /// sources to download the files.
    /// </summary>
    /// <remarks>
    /// Though multiple directories can exist that hold the data cache, be warned:
    /// 1. Datasets can't be split over multiple directories. They must all exist on a single disk.
    /// 2. Downloads will happen to the first disk in the list of Paths.
    /// </remarks>
    class LocalMachine
    {
        public static Location GetLocation(Dictionary<string, string> props)
        {
            var l = new Location();
            l.Name = props["Name"];

            var dirCacheLocations = props["Paths"].Split(',')
                .Select(dirname => new DirectoryInfo(dirname.Trim()))
                .ToArray();

            // We are always good - and we test for directory locations on the fly
            l.LocationTests.Add(() => true);

            l.GetDSInfo = name =>
            {
                var d = FindDataset(dirCacheLocations, name);
                if (d == null)
                {
                    return new DSInfo()
                    {
                        Name = name,
                        IsLocal = filter => false,
                        CanBeGeneratedAutomatically = false,
                        ListOfFiles = () => new string[0],
                        LocationProvider = l,
                    };
                }
                else
                {
                    var w = new WindowsGRIDDSRepro(d.Parent);
                    return new DSInfo()
                    {
                        Name = name,
                        IsLocal = filter => w.FindDSFiles(name, filter) != null,
                        CanBeGeneratedAutomatically = false,
                        ListOfFiles = () => w.ListOfDSFiles(name),
                        LocationProvider = l,
                    };
                }
            };

            // Even though we claim we can't download a data file locally - we can. It is just that we won't do it automatically.
            var linuxFinder = FetchToRemoteLinuxDirInstance.FetchRemoteLinuxInstance(props);
            l.GetDS = (dsinfo, status, filter, failNow, timeout) =>
                {
                    var d = FindDataset(dirCacheLocations, dsinfo.Name);
                    if (d == null)
                    {
                        var validLocalCache = FindLocalCache(dirCacheLocations);
                        if (validLocalCache == null)
                        {
                            throw new InvalidOperationException(string.Format("No local cache directory has been created; we can't copy any files locally until it has. See the {0}.Paths property in the configuration", l.Name));
                        }
                        var dsdir = new DirectoryInfo(Path.Combine(validLocalCache.FullName, dsinfo.Name.SantizeDSName()));
                        dsdir.Create();
                        return LoadDatasetFromOtherSource(new WindowsGRIDDSRepro(dsdir.Parent), dsinfo, status, filter, l.Name, linuxFinder, props["LinuxTempLocation"], failNow, timeout);
                    }
                    var w = new WindowsGRIDDSRepro(d.Parent);
                    var result = w.FindDSFiles(dsinfo.Name, filter);
                    if (result != null)
                    {
                        return result;
                    }
                    return LoadDatasetFromOtherSource(w, dsinfo, status, filter, l.Name, linuxFinder, string.Format("{0}/{1}", props["LinuxTempLocation"], dsinfo.Name.SantizeDSName()), failNow, timeout);
                };

            return l;
        }

        /// <summary>
        /// Find a local directory that has been created and is, thus, accessible and good where we can
        /// cache a dataset.
        /// </summary>
        /// <param name="dirCacheLocations">List of locations to look at</param>
        /// <returns></returns>
        private static DirectoryInfo FindLocalCache(DirectoryInfo[] dirCacheLocations)
        {
            return dirCacheLocations
                .Where(d => d.Exists)
                .FirstOrDefault();
        }

        /// <summary>
        /// Attempt to load the dataset from another location.
        /// </summary>
        /// <param name="dsinfo"></param>
        /// <param name="status"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static Uri[] LoadDatasetFromOtherSource(WindowsGRIDDSRepro dsLocalLocation,
            DSInfo dsinfo,
            Action<string> status,
            Func<string[], string[]> filter, 
            string locName, 
            IFetchToRemoteLinuxDir fetcher,
            string linuxLocation,
            Func<bool> failNow,
            int timeout)
        {
            // First, attempt to find the Uri's from somewhere that we can see and easily copy.
            try
            {
                var files = GRIDDatasetLocator.FetchDatasetUris(dsinfo.Name, status, filter, locationFilter: locname => locname != locName, failNow: failNow);
                var dsinfoRemote = GRIDDatasetLocator.FetchDSInfo(dsinfo.Name, filter, locationFilter: locname => locname != locName);
                if (files != null)
                {
                    dsLocalLocation.MarkAsPartialDownload(dsinfo.Name);
                    var flookup = new HashSet<string>(dsLocalLocation.FindDSFiles(dsinfo.Name, returnWhatWeHave: true).Select(u => Path.GetFileName(u.LocalPath)));
                    foreach (var fremote in files)
                    {
                        if (!flookup.Contains(Path.GetFileName(fremote.LocalPath)))
                        {
                            var f = new FileInfo(fremote.LocalPath);
                            if (status != null)
                            {
                                status(string.Format("Copying file {0}.", f.Name));
                            }
                            f.CopyTo(Path.Combine(dsLocalLocation.LocationOfDataset(dsinfo.Name).FullName, f.Name));
                        }
                    }

                    // And update the meta data.
                    dsLocalLocation.SaveListOfDSFiles(dsinfo.Name, dsinfoRemote.ListOfFiles());
                    dsLocalLocation.RemovePartialDownloadMark(dsinfo.Name);

                    // The files are all local. So we are going to have to see if there is a backup to do the download for us.
                    return dsLocalLocation.FindDSFiles(dsinfo.Name, filter);
                }
            }
            catch (InvalidOperationException)
            {
                // No worries - we weren't able to run one of the data finders.
                // We go onto the next step.
            }

            // OK, nothing could get them. We need to fall back on our secondary copy method. So copy everything to a local Linux directory, and then
            // copy from there down to here.
            dsLocalLocation.MarkAsPartialDownload(dsinfo.Name);
            var allfiles = fetcher.GetListOfFiles(dsinfo.Name, status, failNow: failNow);
            var linuxLocationPerDS = $"{linuxLocation}/{dsinfo.Name}";
            fetcher.Fetch(dsinfo.Name, linuxLocationPerDS, status, filter, failNow: failNow, timeout: timeout);

            // Next, copy the files from there down to our location.
            dsLocalLocation.SaveListOfDSFiles(dsinfo.Name, allfiles);
            fetcher.CopyFromRemote(linuxLocationPerDS, dsLocalLocation.LocationOfDataset(dsinfo.Name), status, removeDirectoryWhenDone: true);
            dsLocalLocation.RemovePartialDownloadMark(dsinfo.Name);

            return dsLocalLocation.FindDSFiles(dsinfo.Name, filter);
        }

        /// <summary>
        /// Find the dataset in one of the directories.
        /// </summary>
        /// <param name="dirs"></param>
        /// <returns>The DirectoryInfo object of the dataset directory if found. Null otherwise.</returns>
        private static DirectoryInfo FindDataset(DirectoryInfo[] dirs, string dsname)
        {
            var sanitizedname = dsname.SantizeDSName();
            return dirs
                .Select(d => new DirectoryInfo(Path.Combine(d.FullName, sanitizedname)))
                .Where(d => d.Exists)
                .FirstOrDefault();
        }
    }
}
