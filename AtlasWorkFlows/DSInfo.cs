using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// Information about a particular dataset.
    /// </summary>
    public class DSInfo
    {
        /// <summary>
        /// The name of the dataset the below information is relevant for.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// True if all the files that pass the filter function are available locally. That is, no serious
        /// network time is required (e.g. downloading from GRID bad, loading from remote network location ok).
        /// Pass "null" as the filter function to test if complete dataset is local.
        /// </summary>
        /// <remarks>
        /// This is compared against the full dataset.
        /// </remarks>
        public Func<Func<string[], string[]>, bool> IsLocal { get; set; }

        /// <summary>
        /// The dataset can be "generated" at this location automatically (without user intervention/request).
        /// </summary>
        /// <remarks>
        /// This is not an absolute test - this makes the assumption that the dataset does exist somewhere in the world
        /// </remarks>
        public bool CanBeGeneratedAutomatically { get; set; }

        /// <summary>
        /// Function that will return a complete list of files in the dataset. These aren't just the files
        /// that might be IsLocal, but all files stored on the GRID.
        /// </summary>
        public Func<string[]> ListOfFiles { get; set; }

        /// <summary>
        /// The location we are connected to
        /// </summary>
        public Location LocationProvider { get; set; }
    }
}
