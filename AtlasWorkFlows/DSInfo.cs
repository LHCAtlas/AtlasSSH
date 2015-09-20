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
        /// True if some files are available locally (eg, on local disk, or close by network disk).
        /// </summary>
        /// <remarks>
        /// This is true even if the dataset is a partial dataset, which means some are local and some aren't!
        /// </remarks>
        public bool IsLocal { get; set; }

        /// <summary>
        /// The dataset can be "generated" at this location automatically (without user intervention/request).
        /// </summary>
        /// <remarks>
        /// This is not an absolute test - this makes the assumption that the dataset does exist somewhere in the world
        /// </remarks>
        public bool CanBeGeneratedAutomatically { get; set; }

        /// <summary>
        /// The number of files in the data set with some special meanings:
        ///   If CanBeGenerated is false, then this field has no meaning.
        ///   0: Unknown number of files are present
        ///   n: n>0 - a subset of all the files, and this contains the number.
        /// </summary>
        public int NumberOfFiles { get; set; }

        /// <summary>
        /// Some files have been downloaded already, but not everything.
        /// </summary>
        public bool IsPartial { get; set; }
    }
}
