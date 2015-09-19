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
        public Uri[] GetDS (DSInfo info)
        {
            // First, we attempt to get the files from the downloaded directory.
            var flist = FindDSFiles(info.Name);
            if (flist != null)
            {
                return flist;
            }

            // Ok, we are going to have to go the full route, unfortunately.
            LinuxFetcher.Fetch(info.Name, string.Format("{0}/{1}", LinuxRootDSDirectory, info.Name));

            // And then the files should all be down!
            return FindDSFiles(info.Name);
        }

        /// <summary>
        /// Return a list of URi's to the files that are part of this dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns></returns>
        private Uri[] FindDSFiles(string dsname)
        {
            // The layout is fixed. If the top level directory doesn't exist, then we assume
            // that nothing good is going on.
            var dinfo = new DirectoryInfo(Path.Combine(LocationOfLocalCache.FullName, dsname.SantizeDSName()));
            if (!dinfo.Exists)
                return null;

            return dinfo.EnumerateFiles("*.root.*", SearchOption.AllDirectories)
                .Select(f => new Uri(string.Format("file://" + f.FullName)))
                .ToArray();
        }
    }
}
