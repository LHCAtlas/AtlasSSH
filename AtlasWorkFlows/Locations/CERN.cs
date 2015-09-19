using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Location info for CERN
    /// </summary>
    class CERN
    {
        /// <summary>
        /// Return the location for CERN
        /// </summary>
        /// <returns></returns>
        public static Location GetLocation()
        {
            var l = new Location();
            l.Name = "CERN";
            l.LocationTests.Add(() => IPLocationTests.FindLocalIpName().EndsWith(".cern.ch"));
            l.GetDSInfo = name => new DSInfo()
            {
                Name = name,
                NumberOfFiles = 0,
                IsLocal = false,
                CanBeGenerated = true
            };
            var dsfinder = new GRIDFetchToLinuxVisibleOnWindows(null, null, null);
            l.GetDS = dsfinder.GetDS;

            return l;
        }
    }
}
