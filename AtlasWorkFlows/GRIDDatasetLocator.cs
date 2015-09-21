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

            // Get the info for each dataset.
            var allDSInfo = (from loc in locationList
                             let dsi = loc.GetDSInfo(datasetname)
                             select new
                             {
                                 loc = loc,
                                 dsinfo = dsi,
                                 islocal = dsi.IsLocal(fileFilter)
                             }).ToArray();

            // First, if anyone has something local, then we should grab that.
            var localDS = allDSInfo.Where(d => d.islocal).OrderByDescending(d => d.loc.Priority).ToArray();

            // Ok, now we need the ones that can generate it.
            if (localDS.Length == 0)
            {
                localDS = allDSInfo.Where(d => d.dsinfo.CanBeGeneratedAutomatically).OrderByDescending(d => d.loc.Priority).ToArray();
            }

            // Did we strike out?
            var locationInfo = localDS.FirstOrDefault();
            if (locationInfo == null)
            {
                var locationListText = "";
                foreach (var l in locationList)
                {
                    locationListText += l.Name + " ";
                }
                throw new ArgumentException(string.Format("Do not know how to generate the dataset '{0}' at any of the locations {1} (which are the only ones working where the computer is located)", datasetname, locationListText));
            }

            // And delegate all the rest of our work to fetching.
            return locationInfo.loc.GetDS(locationInfo.dsinfo, statusUpdate, fileFilter);
        }
    }
}
