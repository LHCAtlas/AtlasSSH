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
        /// True if the dataset can be fetched trivially (via a FileInfo, for example) with no work.
        /// </summary>
        /// <remarks>
        /// This should be false even if the dataset is already been downloaded... This is because it should take
        /// no time to get the DSInfo information - shouldn't be any slower than a possible local disk lookup. Network disk lookups
        /// can be expensive.
        /// </remarks>
        public bool IsLocal { get; set; }

        /// <summary>
        /// The dataset can be "generated" at this location
        /// </summary>
        /// <remarks>
        /// This is not an absolute test - this makes the assumption that the dataset does exist somewhere in the world
        /// </remarks>
        public bool CanBeGenerated { get; set; }

        /// <summary>
        /// The number of files in the data set with some special meanings:
        ///   If CanBeGenerated is false, then this field has no meaning.
        ///   0: All files
        ///   n: n>0 - a subset of all the files, and this contains the number.
        /// </summary>
        public int NumberOfFiles { get; set; }
    }
}
