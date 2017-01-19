using AtlasWorkFlows;
using AtlasWorkFlows.Locations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands.Utils
{
    static class IPlaceUtils
    {
        /// <summary>
        /// Convert a location name to a IPlace reference.
        /// Throws if it can't be found.
        /// </summary>
        /// <param name="placeName"></param>
        /// <returns></returns>
        public static IPlace AsIPlace(this string placeName)
        {
            var loc = string.IsNullOrWhiteSpace(placeName)
                ? (IPlace)null
                : DatasetManager.FindLocation(placeName);

            if (loc == null && !string.IsNullOrWhiteSpace(placeName))
            {
                var err = new ArgumentException($"Location {placeName} is now known to us!");
                throw err;
            }

            return loc;
        }
    }
}
