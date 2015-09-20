using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Represents a cache of files on the local machine
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
                int nfiles = 0;
                bool ispartial = false;
                var d = FindDataset(dirCacheLocations, name);
                if (d != null)
                {
                    var w = new WindowsDataset(d.Parent);
                    nfiles = w.CheckNumberOfFiles(name);
                    ispartial = w.CheckIfPartial(name);
                }
                return new DSInfo()
                {
                    Name = name,
                    NumberOfFiles = nfiles,
                    IsLocal = nfiles > 0,
                    CanBeGeneratedAutomatically = false,
                    IsPartial = ispartial
                };
            };

            // Even though we claim we can't download a data file locally - we can. It is just that we won't do it automatically.
            l.GetDS = null;

            l.HasAllFiles = (dsinfo, filter) =>
            {
                var d = FindDataset(dirCacheLocations, dsinfo.Name);
                if (d == null)
                    return false;
                var w = new WindowsDataset(d.Parent);
                var files = w.FindDSFiles(dsinfo.Name, filter);
                return files != null;
            };

            return l;
        }

        /// <summary>
        /// Find the dataset in one of the directories.
        /// </summary>
        /// <param name="dirs"></param>
        /// <returns>The DirectoryInfo object of the dataset directory if found. Null otherwise.</returns>
        private static DirectoryInfo FindDataset(DirectoryInfo[] dirs, string dsname)
        {
            return dirs
                .Select(d => new DirectoryInfo(Path.Combine(d.FullName, dsname)))
                .Where(d => d.Exists)
                .FirstOrDefault();
        }
    }
}
