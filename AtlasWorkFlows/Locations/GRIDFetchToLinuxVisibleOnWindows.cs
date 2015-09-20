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
        const string DatasetFileList = "aa_dataset_complete_file_list.txt";

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

            // Ok, we are going to have to go the full route, unfortunately. Till we are done, mark this as partial.
            // That way, if there is a crash, it will be understood later on when we come back.
            MarkAsPartialDownload(info.Name);

            // If we have not yet cached the list of files in this dataset, then fetch it.
            if (TotalFilesInDataset(info.Name) == -1)
            {
                SaveListOfDSFiles(info.Name, LinuxFetcher.GetListOfFiles(info.Name));
            }
            LinuxFetcher.Fetch(info.Name, string.Format("{0}/{1}", LinuxRootDSDirectory, info.Name.SantizeDSName()), statusUpdate, fileFilter);

            // And then the files should all be down! If we got them all, then don't mark it as partial.
            var result = FindDSFiles(info.Name, fileFilter, returnWhatWeHave: true);
            if (result.Length == TotalFilesInDataset(info.Name))
                RemovePartialDownloadMark(info.Name);

            return result;
        }

        /// <summary>
        /// Return a list of URi's to the files that are part of this dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns></returns>
        private Uri[] FindDSFiles(string dsname, Func<string[], string[]> fileFilter = null, bool returnWhatWeHave = false)
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

                // This is tricky. We must see if we get back the same files from the full dataset list and from the search of the files
                // that are already downloaded. If they aren't, then we don't have the files the user wants, so we return null.
                var namesOfLocalFiles = fullList.Select(f => Path.GetFileName(f)).ToArray();
                namesOfLocalFiles = fileFilter(namesOfLocalFiles);

                var namesOfRemoteFiles = fileFilter(ListOfDSFiles(dsname));

                var namedHashSet = new HashSet<string>();
                namedHashSet.AddRange(namesOfLocalFiles);
                namedHashSet.AddRange(namesOfRemoteFiles);
                if (!returnWhatWeHave && (namedHashSet.Count != namesOfLocalFiles.Length
                    || namedHashSet.Count != namesOfRemoteFiles.Length))
                {
                    return null;
                }

                var fullList1 = (from nOnly in namesOfLocalFiles
                            select (from f in fullList where Path.GetFileName(f) == nOnly select f).First()).ToArray();
                fullList = fullList1;
            }
            else
            {
                // If we are doing the full download, and this is a partial dataset, then we need to go back and re-do it.
                if (!returnWhatWeHave && CheckIfPartial(dsname))
                {
                    return null;
                }
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
        /// Marks a dataset as being a partial download
        /// </summary>
        /// <param name="dsname"></param>
        private void MarkAsPartialDownload(string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, PartialDownloadTokenFilename));
            if (!f.Directory.Exists)
            {
                f.Directory.Create();
            }
            using (var wr = f.CreateText()) { }
        }

        /// <summary>
        /// Remove the downloading mark.
        /// </summary>
        /// <param name="dsname"></param>
        private void RemovePartialDownloadMark (string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, PartialDownloadTokenFilename));
            if (f.Exists)
                f.Delete();
        }

        /// <summary>
        /// Count the number of files that we have "locally".
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        internal int CheckNumberOfFiles(string dsname)
        {
            var flist = FindDSFiles(dsname, returnWhatWeHave: true);
            if (flist == null)
                return 0;
            return flist.Length;
        }

        /// <summary>
        /// Save the list of files from the dataset.
        /// </summary>
        /// <param name="filenames"></param>
        private void SaveListOfDSFiles(string dsname, string[] filenames)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetFileList));
            using (var wr = f.CreateText())
            {
                foreach (var fn in filenames)
                {
                    wr.WriteLine(fn);
                }
                wr.Close();
            }
        }

        /// <summary>
        /// Count the total number of files that are in this dataset by looking at what we have cached.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private int TotalFilesInDataset(string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetFileList));
            if (!f.Exists)
                return -1;

            int index = 0;

            using (var rd = f.OpenText())
            {
                while (!rd.EndOfStream)
                {
                    if (!string.IsNullOrWhiteSpace(rd.ReadLine()))
                    {
                        index++;
                    }
                }
            }

            return index;
        }

        /// <summary>
        /// Return the list of dataset files.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private string[] ListOfDSFiles(string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetFileList));
            if (!f.Exists)
                return null;

            int index = 0;

            var files = new List<string>();
            using (var rd = f.OpenText())
            {
                while (!rd.EndOfStream)
                {
                    var line = rd.ReadLine();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        files.Add(line);
                    }
                }
            }

            return files.ToArray();
        }

    }
}
