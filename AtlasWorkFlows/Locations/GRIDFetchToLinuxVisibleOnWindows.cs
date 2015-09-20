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
        public Uri[] GetDS (DSInfo info, Action<string> statusUpdate = null, Func<string[], string[]> fileFilter = null)
        {
            // First, we attempt to get the files from the downloaded directory.
            var flist = _winDataset.FindDSFiles(info.Name, fileFilter);
            if (flist != null)
            {
                return flist;
            }

            // Ok, we are going to have to go the full route, unfortunately. Till we are done, mark this as partial.
            // That way, if there is a crash, it will be understood later on when we come back.
            _winDataset.MarkAsPartialDownload(info.Name);

            // If we have not yet cached the list of files in this dataset, then fetch it.
            if (_winDataset.TotalFilesInDataset(info.Name) == -1)
            {
                _winDataset.SaveListOfDSFiles(info.Name, LinuxFetcher.GetListOfFiles(info.Name));
            }
            LinuxFetcher.Fetch(info.Name, string.Format("{0}/{1}", LinuxRootDSDirectory, info.Name.SantizeDSName()), statusUpdate, fileFilter);

            // And then the files should all be down! If we got them all, then don't mark it as partial.
            var result = _winDataset.FindDSFiles(info.Name, fileFilter, returnWhatWeHave: true);
            if (result.Length == _winDataset.TotalFilesInDataset(info.Name))
                _winDataset.RemovePartialDownloadMark(info.Name);

            return result;
        }

        /// <summary>
        /// Return the nubmer of files in the dataset.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal int CheckNumberOfFiles(string name)
        {
            return _winDataset.CheckNumberOfFiles(name);
        }

        /// <summary>
        /// Return true if the dataset is partial.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        internal bool CheckIfPartial(string name)
        {
            return _winDataset.CheckIfPartial(name);
        }
    }
}
