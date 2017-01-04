using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// This is a remote linux machine.
    /// - Files are accessible only as a repository - copy in or out
    /// - If the disks are availible on SAMBA, then create a WindowsLocal as well
    /// - If GRID can download here, then that should also create a new place.
    /// </summary>
    class PlaceLinuxRemote : IPlace
    {
        private string _remote_name;
        private string _remote_path;
        private string _user_name;

        /// <summary>
        /// Create a new repro located on a Linux machine.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="remote_ipaddr"></param>
        /// <param name="username"></param>
        /// <param name="remote_path"></param>
        public PlaceLinuxRemote(string name, string remote_ipaddr, string username, string remote_path)
        {
            Name = name;
            this._remote_name = remote_ipaddr;
            this._remote_path = remote_path;
            this._user_name = username;
            DataTier = 50;

            _connection = new Lazy<SSHConnection>(() => new SSHConnection(_remote_name, _user_name));
        }

        /// <summary>
        /// Get/Set the data tier. We default to 50 as we have no local access.
        /// </summary>
        public int DataTier { get; set; }

        /// <summary>
        /// We do not implement any local access from this end point
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// Get the name of this repro.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Does not need confirmation to use.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return false; } }

        /// <summary>
        /// We can start a copy from here to other places that have a SSH destination availible.
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start a copy from the <paramref name="origin"/> via SSH.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Start a copy pushing data to a particular destination.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Track the connection to the remote computer. Once opened we keep it open.
        /// </summary>
        private Lazy<SSHConnection> _connection;

        /// <summary>
        /// The full list of all files that belong to a particular dataset. This is regardless
        /// of weather or not the files are in this repro.
        /// </summary>
        /// <param name="dsname">Name of the dataset we are looking at</param>
        /// <returns>List of the files in the dataset, or null if the dataset is not known in this repro</returns>
        public string[] GetListOfFilesForDataset(string dsname)
        {
            // The list of files for a dataset can't change over time, so we can
            // cache it locally.
            return NonNullCacheInDisk("PlaceLinuxDatasetFileList", dsname, () =>
            {
                var files = new List<string>();
                try
                {
                    _connection.Value.ExecuteLinuxCommand($"cat {_remote_path}/{dsname}/aa_dataset_complete_file_list.txt", l => files.Add(l));
                } catch (LinuxCommandErrorException) when (files.Count > 0 && files[0].Contains("aa_dataset_complete_file_list.txt: No such file or directory"))
                {
                    // if there was an error accessing it, then it isn't there... Lets hope.
                    return null;
                }
                return files.ToArray();
            });
        }

        /// <summary>
        /// Since we aren't local, we never return file locations. Always throw.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Track the locations of all the files for a particular dataset.
        /// </summary>
        private InMemoryObjectCache<string[]> _filePaths = new InMemoryObjectCache<string[]>();

        /// <summary>
        /// Fetch and return the full file paths of every file in the dataset.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private string[] GetAbosluteLinuxFilePaths(string dsname)
        {
            return _filePaths.GetOrCalc(dsname, () =>
            {
                var files = new List<string>();
                _connection.Value.ExecuteLinuxCommand($"find {_remote_path}/{dsname} -print", l => files.Add(l));
                return files.ToArray();
            });
        }

        /// <summary>
        /// See if we have a particular file in our dataset
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u)
        {
            // Simpmle check.
            if (u.Scheme != "gridds")
            {
                throw new UnknownUriSchemeException($"The uri '{u.OriginalString}' is not a gridds:// uri - can't map it to a file!");
            }

            return GetAbosluteLinuxFilePaths(u.Authority)
                .Select(f => f.Split('/').Last())
                .Where(f => f == u.Segments.Last())
                .Any();
        }
    }
}
