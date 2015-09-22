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
    /// Represents a dataset on a windows share or local drive. The code here just deals
    /// with a dataset in the standard layout. Supports queries, etc.
    /// </summary>
    class WindowsDataset
    {
        /// <summary>
        /// This file is left behind to indicate a dataset hasn't been fully downloaded.
        /// </summary>
        const string PartialDownloadTokenFilename = "aa_download_not_finished.txt";

        /// <summary>
        /// The file that contains a listing of all the files in the full dataset on the GRID.
        /// </summary>
        const string DatasetFileList = "aa_dataset_complete_file_list.txt";

        /// <summary>
        /// Where are the files, visible on windows, going to be located?
        /// </summary>
        public DirectoryInfo LocationOfLocalCache { get; private set; }

        /// <summary>
        /// Initialize this dataset pointing to a particular location visible
        /// directly to the windows file system.
        /// </summary>
        /// <param name="location"></param>
        public WindowsDataset(DirectoryInfo location)
        {
            LocationOfLocalCache = location;
        }

        /// <summary>
        /// Return a list of URi's to the files that are part of this dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns></returns>
        public Uri[] FindDSFiles(string dsname, Func<string[], string[]> fileFilter = null, bool returnWhatWeHave = false)
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

            // If there is no filter, we keep it as no filter.
            if (fileFilter == null)
            {
                fileFilter = alist => alist;
            }

            // This is tricky. We must see if we get back the same files from the full dataset list and from the search of the files
            // that are already downloaded. If they aren't, then we don't have the files the user wants, so we return null.
            var namesOfLocalFiles = fullList.Select(f => Path.GetFileName(f)).ToArray();
            namesOfLocalFiles = fileFilter(namesOfLocalFiles);

            var namesOfRemoteFiles = fileFilter(ListOfDSFiles(dsname).Select(fn => fn.SantizeDSName()).ToArray());

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
        /// Return the local directory of a particular dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public DirectoryInfo LocationOfDataset(string dsname)
        {
            return BuildDSRootDirectory(dsname);
        }

        /// <summary>
        /// Return the list of dataset files from the locally stored text file.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public string[] ListOfDSFiles(string dsname)
        {
            var f = new FileInfo(Path.Combine(BuildDSRootDirectory(dsname).FullName, DatasetFileList));
            if (!f.Exists)
                return ListOfFilesWeKnowAbout(dsname);

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

        /// <summary>
        /// If someone else downloads the files, they won't always put all the files in there.
        /// If that is the case, we will just assume they have done a full dataset download.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private string[] ListOfFilesWeKnowAbout(string dsname)
        {
            var loc = BuildDSRootDirectory(dsname);
            if (!loc.Exists)
                return new string[0];

            return loc.EnumerateFiles("*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")).Select(f => f.Name).ToArray();
        }

        /// <summary>
        /// Marks a dataset as being a partial download
        /// </summary>
        /// <param name="dsname"></param>
        public void MarkAsPartialDownload(string dsname)
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
        public void RemovePartialDownloadMark(string dsname)
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
        public int CheckNumberOfFiles(string dsname)
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
        public void SaveListOfDSFiles(string dsname, string[] filenames)
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
        public int TotalFilesInDataset(string dsname)
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
    }
}
