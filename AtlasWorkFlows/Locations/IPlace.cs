using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// A location where files can be stored and grid datasets can be looked up
    /// For example:
    ///     - The local disk
    ///     - A remote linux computer with SMB visible directory
    ///     - A remote linux computer
    ///     - the GRID
    /// </summary>
    public interface IPlace
    {
        /// <summary>
        /// The name of the place.
        /// </summary>
        /// <remarks>
        /// Must be unique
        /// </remarks>
        string Name { get; }

        /// <summary>
        /// Returns true if it can give a Uri for files that are contained in its repository
        /// that points to files accessible on this windows computer.
        /// </summary>
        bool IsLocal { get; }

        /// <summary>
        /// Returns true if a copy operation can be origniated from this place and the data sent and received
        /// at the destination place.
        /// </summary>
        /// <param name="destination">The place where the data is to be sent</param>
        /// <returns></returns>
        bool CanSourceCopy(IPlace destination);

        /// <summary>
        /// Return the actual location of a file that can be used as input to another windows program.
        /// </summary>
        /// <param name="uris"></param>
        /// <returns></returns>
        /// <remarks>If the files is not in the local repro, or this repo is not lcoal and can't render a absolute file location for windows, then an exception is to be thrown.</remarks>
        IEnumerable<Uri> GetLocalFileLocations(IEnumerable<Uri> uris);

        /// <summary>
        /// What data teir. The larger the number, the more we should try to stay away from it
        /// </summary>
        /// <remarks>
        /// Nominal:
        ///     - 10 is local disk
        ///     - 20 is a remote linux computer with SMB
        ///     - 50 are remote acces linux computers with no SMB
        ///     - 100 is the GRID
        /// </remarks>
        int DataTier { get; }

        /// <summary>
        /// Returns a list of files in a particular dataset. If the dataset is not
        /// known at this location, return null.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns>A list of the files that are part of the dataset. Null if the dataset is unkown to the repro</returns>
        /// <remarks>
        /// Any IPlace that retuns this list should return the same list. There can be only
        /// one "true" set of files in a dataset. If there is a difference, it is a bug, and
        /// results are not longer going to be... sensible!
        /// </remarks>
        string[] GetListOfFilesForDataset(string dsname);

        /// <summary>
        /// Copy the full info for this dataset into the repro
        /// </summary>
        /// <param name="dsName">Dataset name</param>
        /// <param name="files">List of files in the dataset</param>
        void CopyDataSetInfo(string dsName, string[] files);

        /// <summary>
        /// The user must explicitly request this site as a destination that things are to be copied or cached to.
        /// </summary>
        /// <remarks>
        /// Example use: for the local disk to prevent accidental copying of files to it
        /// </remarks>
        bool NeedsConfirmationCopy { get; }

        /// <summary>
        /// Return true if this place can get its hands on a particular file. If the dataset does not exist in this location, this will also return false.
        /// </summary>
        /// <param name="u">Uri of the dataset and filename we are interested in</param>
        /// <returns>True if the file exists locally, false if not. Does not check for dataset existance or file membership.</returns>
        bool HasFile(Uri u);

        /// <summary>
        /// Copy the URI's to the other place, by running the copy from this place.
        /// This is a push operation (pushing the data)
        /// </summary>
        /// <param name="item2"></param>
        /// <param name="uris"></param>
        /// <remarks>All URI's are of the same dataest.</remarks>
        void CopyTo(IPlace destination, Uri[] uris);

        /// <summary>
        /// Copy the URI's from the other location to this local location, running the copy
        /// commands at this place. This is a pull operation (pulling the data).
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="uris"></param>
        void CopyFrom(IPlace origin, Uri[] uris);
    }
}
