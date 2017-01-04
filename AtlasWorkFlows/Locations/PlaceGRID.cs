using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    // getting data from the GRID.
    class PlaceGRID : IPlace
    {
        private PlaceLinuxRemote _linuxRemote;

        /// <summary>
        /// Initialize the GRID location.
        /// </summary>
        /// <param name="name">Name of the GRID re-pro</param>
        /// <param name="linuxRemote">All GRID sites are paired with some sort of Linux local access where their downloads are sent to.</param>
        public PlaceGRID(string name, PlaceLinuxRemote linuxRemote)
        {
            Name = name;
            _linuxRemote = linuxRemote;
        }

        /// <summary>
        /// We are a GRID site, so a high data tier number.
        /// </summary>
        public int DataTier { get { return 100; } }

        /// <summary>
        /// No local files can be reached here!
        /// </summary>
        public bool IsLocal { get { return false; } }

        /// <summary>
        /// The name of the GRID access point.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// We can't be copied to... yet.
        /// </summary>
        public bool NeedsConfirmationCopy { get { return true; } }

        /// <summary>
        /// We can only source a copy to our partner place!
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public bool CanSourceCopy(IPlace destination)
        {
            return destination == _linuxRemote;
        }

        /// <summary>
        /// Since we can't create a GRID dataset, this is not supported!
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="files"></param>
        public void CopyDataSetInfo(string dsName, string[] files)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// We can't copy data into the GRID.
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        public void CopyFrom(IPlace origin, Uri[] uris)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be copied to.");
        }

        /// <summary>
        /// Download files from the grid to the linux local site.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="uris"></param>
        public void CopyTo(IPlace destination, Uri[] uris)
        {
            throw new NotImplementedException();
        }

        public string[] GetListOfFilesForDataset(string dsname)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Since we aren't visible on windows, this is just not possible.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        public IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris)
        {
            throw new NotSupportedException($"GRID Place {Name} can't be directly accessed from Windows - so no local path names!");
        }

        /// <summary>
        /// Check to see if a particular file exists. As long as that file is a member
        /// of the dataset, then it will as the GRID has EVERYTHING. ;-)
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public bool HasFile(Uri u)
        {
            throw new NotImplementedException();
        }
    }
}
