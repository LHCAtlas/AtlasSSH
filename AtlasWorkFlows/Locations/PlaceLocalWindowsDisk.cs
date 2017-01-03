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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copy the data to the other location.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            throw new NotImplementedException();
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
            return files;
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
                            let ds = u.Authority
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

            var ds = u.Authority;
            if (!_rootLocation.HasDS(ds))
            {
                throw new DatasetDoesNotExistInThisReproException($"The dataset '{ds}' does not exist in the local repository '{Name}'");
            }

            var filename = u.Segments.Last();
            return _rootLocation.HasFile(ds, filename);
        }
    }
}
