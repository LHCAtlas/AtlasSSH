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
                    };
                }
                else
                {
                    var w = new WindowsDataset(d.Parent);
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
            l.GetDS = (dsinfo, status, filter) =>
                {
                    var d = FindDataset(dirCacheLocations, dsinfo.Name);
                    if (d == null)
                    {
                        var dsdir = new DirectoryInfo(Path.Combine(dirCacheLocations[0].FullName, dsinfo.Name));
                        dsdir.Create();
                        return LoadDatasetFromOtherSource(new WindowsDataset(dsdir.Parent), dsinfo, status, filter, l.Name);
                    }
                    var w = new WindowsDataset(d.Parent);
                    var result = w.FindDSFiles(dsinfo.Name, filter);
                    if (result != null)
                    {
                        return result;
                    }
                    return LoadDatasetFromOtherSource(w, dsinfo, status, filter, l.Name);
                };

            return l;
        }

        /// <summary>
        /// Attempt to load the dataset from another location.
        /// </summary>
        /// <param name="dsinfo"></param>
        /// <param name="status"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static Uri[] LoadDatasetFromOtherSource(WindowsDataset dsLocalLocation, DSInfo dsinfo, Action<string> status, Func<string[], string[]> filter, string locName)
        {
            // First, attempt to find the Uri's from somewhere that we can see and easily copy.
            var files = GRIDDatasetLocator.FetchDatasetUris(dsinfo.Name, status, filter, locationFilter: locname => locname != locName);
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

                // The files are all local. So...
                return dsLocalLocation.FindDSFiles(dsinfo.Name, filter);
            }

            // Ok, nothing could get them. We need to fall back on our secondary copy method.
            throw new NotImplementedException();
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
