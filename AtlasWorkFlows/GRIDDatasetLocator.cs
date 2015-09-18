using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// Find out info about a GRID dataset, including making it available for access.
    /// </summary>
    public class GRIDDatasetLocator
    {
        /// <summary>
        /// Main routine to find all the URI's that point to a dataset.
        /// </summary>
        /// <param name="datasetname">The GRID dataset name</param>
        /// <returns></returns>
        public static Uri[] FetchDatasetUris(string datasetname)
        {
            if (string.IsNullOrWhiteSpace(datasetname))
            {
                throw new ArgumentException("Dataset name is empty");
            }
            return null;
        }
    }
}
