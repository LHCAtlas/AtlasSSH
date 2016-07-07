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
            var dnsString = props["DNSEndString"];
            Func<bool> test = () => dnsString.Split(',').Select(s => s.Trim()).Select(en => IPLocationTests.FindLocalIpName().EndsWith(en)).Where(t => t).Any();
            l.LocationTests.Add(() => test());

            var fetcher = FetchToRemoteLinuxDirInstance.FetchRemoteLinuxInstance(props);
            var dsfinder = new GRIDFetchToLinuxVisibleOnWindows(new DirectoryInfo(props["WindowsPath"]), fetcher, props["LinuxPath"]);

            l.GetDSInfo = name =>
            {
                var nfiles = dsfinder.CheckNumberOfFiles(name);
                return new DSInfo()
                {
                    Name = name,
                    IsLocal = filter => dsfinder.CheckIfLocal(name, filter),
                    CanBeGeneratedAutomatically = true,
                    ListOfFiles = () => dsfinder.ListOfFiles(name),
                    LocationProvider = l,
                };
            };

            l.GetDS = dsfinder.GetDS;

            return l;
        }
    }
}
