using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// A local windows dataset directory
    /// </summary>
    class PlaceLocalWindowsDisk : IPlace
    {
        /// <summary>
        /// Initialize a windows repo for grid datasets
        /// </summary>
        /// <param name="name"></param>
        /// <param name="repoDir"></param>
        public PlaceLocalWindowsDisk(string name, DirectoryInfo repoDir)
        {
            Name = name;
            _rootLocation = new WindowsGRIDDSRepro(repoDir);
        }

        /// <summary>
        /// Get the data tier - which is super fast and local.
        /// </summary>
        public int DataTier { get { return 1; } }

        /// <summary>
        /// By definition, if this is loaded, then it is pointing to a local directory.
        /// </summary>
        public bool IsLocal { get { return true; } }

        public string Name { get; private set; }

        /// <summary>
        /// By default, one can't copy to this location without explicitly mentioning it as
        /// a destination point.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return true; } }

        /// <summary>
        /// Location of this repo.
        /// </summary>
        private WindowsGRIDDSRepro _rootLocation;

        /// <summary>
        /// We can copy files from this other location?
        /// </summary>
        /// <param name="destination">The destination we need to copy from</param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            // As long as it is another active location
            return destination is PlaceLocalWindowsDisk;
        }

        /// <summary>
        /// Copy from the other location. We must have already agreed to do the copy, so we know
        /// how to do the copy.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris)
        {
            var other = (origin as PlaceLocalWindowsDisk)
                .ThrowIfNull(() => new ArgumentException($"Can CopyFrom only another PlaceLocalWindowsDisk - '{origin.Name}' isn't - this is an internal error."));

            // Do the copy by dataset, which is our primary way of storing things here.
            var groupedByDS = uris.GroupBy(u => u.DatasetName());
            foreach (var dsFileListing in groupedByDS)
            {
                if (!_rootLocation.HasDS(dsFileListing.Key))
                {
                    CopyDSInfoFrom(other, dsFileListing.Key);
                }

                // For each file we don't have, do the copy. Checking for existance shouldn't
                // be necessary - it should have been done - but it is so cheap compared to the cost
                // of copying a 2 GB file...
                foreach (var f in dsFileListing)
                {
                    if (!HasFile(f))
                    {
                        var ourpath = new FileInfo(Path.Combine(_rootLocation.LocationOfDataset(f.DatasetName()).FullName, "copied", f.Segments.Last()));
                        if (!ourpath.Directory.Exists)
                        {
                            ourpath.Directory.Create();
                        }
                        var otherPath = new FileInfo(other.GetLocalFileLocations(new Uri[] { f }).First().LocalPath);
                        otherPath.CopyTo(ourpath.FullName);
                    }
                }
            }
        }

        /// <summary>
        /// Copy the dataset file listing from another location
        /// </summary>
        /// <param name="other"></param>
        /// <param name="key"></param>
        private void CopyDSInfoFrom(PlaceLocalWindowsDisk other, string dsName)
        {
            var filenames = other.GetListOfFilesForDataset(dsName);
            CopyDataSetInfo(dsName, filenames);
        }

        /// <summary>
        /// Copy the data to the other location.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            var other = (destination as PlaceLocalWindowsDisk)
                .ThrowIfNull(() => new ArgumentException($"Can CopyFrom only another PlaceLocalWindowsDisk - '{destination.Name}' isn't - this is an internal error."));

            // Well... since everything is symmtric...
            other.CopyFrom(this, uris);
        }

        /// <summary>
        /// Look to see if we know about the dataset in our library, and if so, fetch the file list.
        /// </summary>
        /// <param name="dsname">Name of datest</param>
        /// <returns>List of files, null if the dataset is not known.</returns>
        public string[] GetListOfFilesForDataset(string dsname)
        {
            var files = _rootLocation.ListOfDSFiles(dsname);
            if (files.Length == 0)
            {
                return null;
            }
            return files
                .Select(f => f.Contains(":") ? f.Substring(f.IndexOf(":")+1) : f)
                .ToArray();
        }

        /// <summary>
        /// Get the absolute file locations from the source dataset. Throws if the file does not exist locally.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            // Do it by dataset.
            var dsGroups = (from u in uris
                            let ds = u.DatasetName()
                            let fname = u.Segments.Last()
                            group new { FileName = fname } by ds)
                           .Throw(g => !_rootLocation.HasDS(g.Key), g => new DatasetDoesNotExistInThisReproException($"Dataset '{g.Key}' does not exists in repro {Name}"));

            var fileUris = from dsFiles in dsGroups
                           let flist = _rootLocation.FindDSFiles(dsFiles.Key, returnWhatWeHave: true)
                           from u in dsFiles
                           select new { matchedUri = flist.Where(fu => fu.Segments.Last() == u.FileName).FirstOrDefault(), OrigUri = u };

            return fileUris
                .Throw(o => o.matchedUri == null, o => new DatasetFileNotLocalException($"File {o.OrigUri} does not exist locally!"))
                .Select(o => o.matchedUri);
        }

        /// <summary>
        /// Do we have access to this specific file in this dataset? If this dataset is not here, then we throw an exception.
        /// </summary>
        /// <param name="u">URI of the file in gridds:// format. It is assumed this file is part of the dataset</param>
        /// <returns>true if the file exists on the local disk. False otherwise</returns>
        public bool HasFile(Uri u)
        {
            // Simple checks.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"Uri {u} does not have a gridds:// scheme!");
            }

            var ds = u.DatasetName();
            if (!_rootLocation.HasDS(ds))
            {
                return false;
            }

            var filename = u.Segments.Last();
            return _rootLocation.HasFile(ds, filename);
        }

        /// <summary>
        /// Copy files over
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public void CopyDataSetInfo(string dsName, string[] files)
        {
            _rootLocation.SaveListOfDSFiles(dsName, files);
        }
    }
}
