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
    /// Holds code to enable a download of a dataset to a location on a grid connected node on Linux
    /// and transfer that code to a place that is visible on Windows (via a NAS or x-mount).
    /// </summary>
    class GRIDFetchToLinuxVisibleOnWindows
    {
        /// <summary>
        /// This file is left behind to indicate a dataset hasn't been fully downloaded.
        /// </summary>
        const string PartialDownloadTokenFilename = "aa_download_not_finished.txt";

        /// <summary>
        /// Initialized the fetch to Linux code, where the Linux stuff is visible on windows.
        /// </summary>
        /// <param name="windowsFilesLocation"></param>
        /// <param name="fetcher"></param>
        /// <param name="rootLinuxLocation"></param>
        public GRIDFetchToLinuxVisibleOnWindows(DirectoryInfo windowsFilesLocation, IFetchToRemoteLinuxDir fetcher, string rootLinuxLocation)
        {
            LocationOfLocalCache = windowsFilesLocation;
            LinuxFetcher = fetcher;
            LinuxRootDSDirectory = rootLinuxLocation;
        }

        /// <summary>
        /// Where are the files, visible on windows, going to be located?
        /// </summary>
        public DirectoryInfo LocationOfLocalCache { get; private set; }

        /// <summary>
        /// Methods to fetch the dataset from the GRID to some local LinuxDirectory.
        /// </summary>
        public IFetchToRemoteLinuxDir LinuxFetcher { get; private set; }

        /// <summary>
        /// ROOT directory on Linux where these files are to be located.
        /// </summary>
        public string LinuxRootDSDirectory { get; set; }

        /// <summary>
        /// Top level routine that will return a set of URI's for the files.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public Uri[] GetDS (DSInfo info, Action<string> statusUpdate = null, Func<string[], string[]> fileFilter = null)
        {
            // First, we attempt to get the files from the downloaded directory.
            var flist = FindDSFiles(info.Name, fileFilter);
            if (flist != null)
            {
                return flist;
            }

            // Ok, we are going to have to go the full route, unfortunately.
            LinuxFetcher.Fetch(info.Name, string.Format("{0}/{1}", LinuxRootDSDirectory, info.Name.SantizeDSName()), statusUpdate, fileFilter);

            // And then the files should all be down!
            return FindDSFiles(info.Name, fileFilter);
        }

        /// <summary>
        /// Return a list of URi's to the files that are part of this dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns></returns>
        private Uri[] FindDSFiles(string dsname, Func<string[], string[]> fileFilter = null)
        {
            // The layout is fixed. If the top level directory doesn't exist, then we assume
            // that nothing good is going on.
            var dinfo = BuildDSRootDirectory(dsname);
            if (!dinfo.Exists)
                return null;

            var fullList = dinfo.EnumerateFiles("*.root.*", SearchOption.AllDirectories)
                .Where(f => !f.FullName.EndsWith(".part"))
                .Select(f => f.FullName)
                .ToArray();

            if (fileFilter != null) {
                var nameOnlyList = fullList.Select(f => Path.GetFileName(f)).ToArray();
                nameOnlyList = fileFilter(nameOnlyList);
                var fullList1 = (from nOnly in nameOnlyList
                            select (from f in fullList where Path.GetFileName(f) == nOnly select f).First()).ToArray();
                fullList = fullList1;
            }

            return fullList
                .Select(f => new Uri(string.Format("file://" + f)))
                .ToArray();
        }

        /// <summary>
        /// Gets the dataset root directory.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private DirectoryInfo BuildDSRootDirectory(string dsname)
        {
            return new DirectoryInfo(Path.Combine(LocationOfLocalCache.FullName, dsname.SantizeDSName()));
        }

        /// <summary>
        /// Look for the partial marker file in the download.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        internal bool CheckIfPartial(string dsname)
        {
            return File.Exists(Path.Combine(BuildDSRootDirectory(dsname).FullName, PartialDownloadTokenFilename));
        }

        /// <summary>
        /// Count the number of files that we have "locally".
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        internal int CheckNumberOfFiles(string dsname)
        {
            var flist = FindDSFiles(dsname);
            if (flist == null)
                return 0;
            return flist.Length;
        }
    }
}
