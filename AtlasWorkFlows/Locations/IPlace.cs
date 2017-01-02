using System;
using System.Collections.Generic;
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
        /// <returns></returns>
        string[] GetListOfFilesForDataset(string dsname);

        /// <summary>
        /// The user must explicitly request this site as a destination that things are to be copied or cached to.
        /// </summary>
        /// <remarks>
        /// Example use: for the local disk to prevent accidental copying of files to it
        /// </remarks>
        bool NeedsConfirmationCopy { get; }
    }
}
