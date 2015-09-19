using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Location info for CERN
    /// </summary>
    class LinuxWithWindowsReflector
    {
        /// <summary>
        /// Return the location for CERN
        /// </summary>
        /// <param name="props">Property bag to configure the object</param>
        /// <returns></returns>
        public static Location GetLocation(Dictionary<string, string> props)
        {
            var l = new Location();
            l.Name = props["Name"];
            l.LocationTests.Add(() => IPLocationTests.FindLocalIpName().EndsWith(props["DNSEndString"]));
            l.GetDSInfo = name => new DSInfo()
            {
                Name = name,
                NumberOfFiles = 0,
                IsLocal = false,
                CanBeGenerated = true
            };

            var fetcher = new LinuxFetcher(props["LinuxHost"], props["LinuxPath"]);

            var dsfinder = new GRIDFetchToLinuxVisibleOnWindows(new DirectoryInfo(props["WindowsPath"]), fetcher, props["LinuxPath"]);
            l.GetDS = dsfinder.GetDS;

            return l;
        }
    }
}
