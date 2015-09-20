using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Represents a cache of files on the local machine
    /// </summary>
    class LocalMachine
    {
        public static Location GetLocation(Dictionary<string, string> props)
        {
            var l = new Location();
            l.Name = props["Name"];

            var dirCacheLocations = props["Paths"].Split(',')
                .Select(dirname => new DirectoryInfo(dirname.Trim()))
                .ToArray();

            // We are always good - and we test for directory locations on the fly
            l.LocationTests.Add(() => true);

            l.GetDSInfo = name =>
            {
                var files = GetListOfFiles(dirCacheLocations, name);
                return new DSInfo()
                {
                    Name = name,
                    NumberOfFiles = files.Length,
                    IsLocal = files.Any(),
                    CanBeGenerated = false,
                    IsPartial = IsPartial(dirCacheLocations, name)
                };
            };

            // Even though we claim we can't download a data file locally - we can. It is just that we won't do it automatically.
            l.GetDS = null;

            return l;
        }

        /// <summary>
        /// Return true if the dataset has been downloaded locally
        /// </summary>
        /// <param name="dirCacheLocations"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private static bool IsPartial(DirectoryInfo[] dirCacheLocations, string name)
        {
            return false;
        }

        /// <summary>
        /// Scan all directories for a list of files. We will combine as if in the same directory.
        /// </summary>
        /// <param name="dirs"></param>
        /// <param name="dsname"></param>
        private static FileInfo[] GetListOfFiles(DirectoryInfo[] dirs, string dsname)
        {
            return null;
        }
    }
}
