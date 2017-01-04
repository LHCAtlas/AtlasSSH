using AtlasWorkFlows.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private string v;
        private string _remote_name;
        private string _remote_path;

        public PlaceLinuxRemote(string name)
        {
            DataTier = 50;
            Name = name;
        }

        public PlaceLinuxRemote(string v, string _remote_name, string _remote_path)
        {
            this.v = v;
            this._remote_name = _remote_name;
            this._remote_path = _remote_path;
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
        /// The full list of all files that belong to a particular dataset. This is regardless
        /// of weather or not the files are in this repro.
        /// </summary>
        /// <param name="dsname">Name of the dataset we are looking at</param>
        /// <returns>List of the files in the dataset, or null if the dataset is not known in this repro</returns>
        public string[] GetListOfFilesForDataset(string dsname)
        {
            throw new NotImplementedException();
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
        /// See if we have a particular file in our dataset
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u)
        {
            throw new NotImplementedException();
        }
    }
}
