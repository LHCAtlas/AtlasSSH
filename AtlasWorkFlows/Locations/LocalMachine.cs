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
        const string PartialDownloadTokenFilename = "aa_download_not_finished.txt";
        const string DatasetFileList = "aa_dataset_complete_file_list.txt";

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
                var files = GetListOfFiles(dirCacheLocations, name);
                return new DSInfo()
                {
                    Name = name,
                    NumberOfFiles = files.Length,
                    IsLocal = files.Any(),
                    CanBeGeneratedAutomatically = false,
                    IsPartial = IsPartial(dirCacheLocations, name)
                };
            };

            // Even though we claim we can't download a data file locally - we can. It is just that we won't do it automatically.
            l.GetDS = null;

            return l;
        }

        /// <summary>
        /// Return true if the dataset has been downloaded locally
        /// </summary>
        /// <param name="dirCacheLocations"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool IsPartial(DirectoryInfo[] dirCacheLocations, string name)
        {
            var d = FindDataset(dirCacheLocations, name);
            if (d == null)
                return false;

            return File.Exists(Path.Combine(d.FullName, PartialDownloadTokenFilename));
        }

        /// <summary>
        /// Scan all directories for a list of files. We will combine as if in the same directory.
        /// </summary>
        /// <param name="dirs"></param>
        /// <param name="dsname"></param>
        private static FileInfo[] GetListOfFiles(DirectoryInfo[] dirs, string dsname)
        {
            var d = FindDataset(dirs, dsname);
            if (d == null)
                return new FileInfo[0];
            return d.EnumerateFiles("*.root.*", SearchOption.AllDirectories)
                .Where(f => !f.FullName.EndsWith(".part"))
                .ToArray();
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
