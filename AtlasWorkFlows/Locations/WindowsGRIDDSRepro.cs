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
    class WindowsGRIDDSRepro
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
        public WindowsGRIDDSRepro(DirectoryInfo location)
        {
            LocationOfLocalCache = location;
        }

        /// <summary>
        /// Do we have a particular file in this dataset?
        /// </summary>
        /// <param name="dsname"></param>
        /// <param name="fname"></param>
        /// <returns></returns>
        public bool HasFile (string dsname, string fname)
        {
            // The layout is fixed. If the top level directory doesn't exist, then we assume
            // that nothing good is going on.
            var dinfo = BuildDSRootDirectory(dsname);
            if (!dinfo.Exists)
                return false;

            var fullList = dinfo.EnumerateFiles("*.root.*", SearchOption.AllDirectories)
                .Where(f => !f.FullName.EndsWith(".part"))
                .Select(f => f.Name)
                .Where(f => f == fname)
                .FirstOrDefault();

            return fullList != null;
        }

        /// <summary>
        /// Does this dataset exist in this reposetory?
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public bool HasDS(string dsname)
        {
            var dinfo = BuildDSRootDirectory(dsname);
            return dinfo.Exists;
        }

        /// <summary>
        /// Return a list of URi's to the files that are part of this dataset.
        /// </summary>
        /// <param name="dsname">Dataset name</param>
        /// <returns></returns>
        /// <remarks>
        /// TODO: In the new placed based system, this code may be obsolete. At least all the filter code!
        /// </remarks>
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

            var filesInCompleteDataset = ListOfDSFiles(dsname);
            if (filesInCompleteDataset.Length == 0)
            {
                // If the local dataset thinks there are no files in the dataset,
                // then assume something went wrong (e.g. crash during dataset download).
                return null;
            }
            var namesOfRemoteFiles = fileFilter(filesInCompleteDataset.Select(fn => fn.SantizeDSName()).ToArray());

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
            ;
            var f = new FileInfo(Path.Combine(AssureDatasetDirectoryCreated(dsname).FullName, PartialDownloadTokenFilename));
            using (var wr = f.CreateText()) { }
        }

        /// <summary>
        /// Make sure the dataset directory has been created. If it hasn't, then create it.
        /// Also, manage the updating of the contents.txt file.
        /// </summary>
        /// <param name="dsname"></param>
        private DirectoryInfo AssureDatasetDirectoryCreated(string dsname)
        {
            var d = BuildDSRootDirectory(dsname);
            if (!d.Exists)
            {
                try {
                    d.Create();
                    UpdateContentsFile(dsname);
                } catch (Exception e)
                {
                    throw new IOException($"I/O Failure while trying to create dataset directory ({d.FullName}) and update file content: {e.Message}", e);
                }
            }
            return d;
        }

        /// <summary>
        /// Write whatever we need for the dataset name to the contents.txt dataset catalog.
        /// </summary>
        /// <param name="dsname"></param>
        private void UpdateContentsFile(string dsname)
        {
            var contentsFile = new FileInfo(Path.Combine(LocationOfLocalCache.FullName, "contents.txt"));
            if (!contentsFile.Exists) {
                using (var wr = contentsFile.CreateText()){}
            }

            // Scan through to see if this dataset is already in the file.
            var outputContents = new FileInfo(Path.Combine(LocationOfLocalCache.FullName, "contents.txt.tmp"));
            bool foundOldReference = false;
            using (var wr = outputContents.CreateText()) {
                using (var rd = contentsFile.OpenText()) {
                    string line = "";
                    while ((line = rd.ReadLine()) != null) {
                        if (line.StartsWith(dsname + " ")) {
                            foundOldReference = true;
                            break;
                        }
                        wr.WriteLine(line);
                    }
                }

                if (!foundOldReference) {
                    wr.WriteLine(string.Format("{0} {1}", dsname, System.Security.Principal.WindowsIdentity.GetCurrent().Name));
                }
            }

            // Either put in the new contents.txt file, or delete this temp one if not needed.
            if (foundOldReference)
            {
                outputContents.Delete();
            }
            else
            {
                contentsFile.Delete();
                outputContents.CopyTo(contentsFile.FullName, true);
                outputContents.Delete();
            }
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
