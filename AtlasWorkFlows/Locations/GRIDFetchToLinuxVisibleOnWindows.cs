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
        /// Initialized the fetch to Linux code, where the Linux stuff is visible on windows.
        /// </summary>
        /// <param name="windowsFilesLocation"></param>
        /// <param name="fetcher"></param>
        /// <param name="rootLinuxLocation"></param>
        public GRIDFetchToLinuxVisibleOnWindows(DirectoryInfo windowsFilesLocation, IFetchToRemoteLinuxDir fetcher, string rootLinuxLocation)
        {
            _winDataset = new WindowsDataset(windowsFilesLocation);
            LinuxFetcher = fetcher;
            LinuxRootDSDirectory = rootLinuxLocation;
        }

        /// <summary>
        /// Track the files on the local disk.
        /// </summary>
        private WindowsDataset _winDataset = null;

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
        public Uri[] GetDS (string dsname, Action<string> statusUpdate = null, Func<string[], string[]> fileFilter = null)
        {
            // First, we attempt to get the files from the downloaded directory.
            var flist = _winDataset.FindDSFiles(dsname, fileFilter);
            if (flist != null)
            {
                return flist;
            }

            // Ok, we are going to have to go the full route, unfortunately. Till we are done, mark this as partial.
            // That way, if there is a crash, it will be understood later on when we come back.
            _winDataset.MarkAsPartialDownload(dsname);

            // If we have not yet cached the list of files in this dataset, then fetch it.
            if (_winDataset.TotalFilesInDataset(dsname) == -1)
            {
                _winDataset.SaveListOfDSFiles(dsname, LinuxFetcher.GetListOfFiles(dsname, statusUpdate));
            }
            LinuxFetcher.Fetch(dsname, string.Format("{0}/{1}", LinuxRootDSDirectory, dsname.SantizeDSName()), statusUpdate, fileFilter);

            // And then the files should all be down! If we got them all, then don't mark it as partial.
            var result = _winDataset.FindDSFiles(dsname, fileFilter, returnWhatWeHave: true);
            if (result.Length == _winDataset.TotalFilesInDataset(dsname))
                _winDataset.RemovePartialDownloadMark(dsname);

            return result;
        }
        public Uri[] GetDS (DSInfo info, Action<string> statusUpdate = null, Func<string[], string[]> fileFilter = null)
        {
            return GetDS(info.Name, statusUpdate, fileFilter);
        }

        /// <summary>
        /// Return the number of files in the dataset.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal int CheckNumberOfFiles(string name)
        {
            return _winDataset.CheckNumberOfFiles(name);
        }

        /// <summary>
        /// Check to see if the files listed actually match the full dataset that we have
        /// on disk.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        internal bool CheckIfLocal(string dsname, Func<string[], string[]> filter)
        {
            return _winDataset.FindDSFiles(dsname, filter) != null;
        }

        /// <summary>
        /// Return a list of files that are in the complete dataset.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string[] ListOfFiles(string name)
        {
            return _winDataset.ListOfDSFiles(name);
        }
    }
}
