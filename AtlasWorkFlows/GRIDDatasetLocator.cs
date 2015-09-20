using AtlasWorkFlows.Locations;
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
        public static Uri[] FetchDatasetUris(string datasetname, Action<string> statusUpdate = null, Func<string[],string[]> fileFilter = null)
        {
            // Basic parameter checks
            if (string.IsNullOrWhiteSpace(datasetname))
            {
                throw new ArgumentException("Dataset name is empty");
            }

            // Get a location so we can see if we can fetch the dataset
            var locator = new Locator();
            var locationList = locator.FindBestLocations();
            if (locationList.Length == 0)
            {
                throw new InvalidOperationException(string.Format("There are no valid ways to download dataset '{0}' from your current location.", datasetname));
            }

            // Next, determine the location we will use to fetch the dataset. We get the dsInfo
            // from each, and we have to ask if the dataset is local or not.
            var location = locationList.First();

            // And delegate all the rest of our work to fetching.
            var dsinfo = location.GetDSInfo(datasetname);
            return location.GetDS(dsinfo, statusUpdate, fileFilter);
        }
    }
}
