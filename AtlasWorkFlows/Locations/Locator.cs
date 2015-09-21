using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Determine what the best location to use
    /// </summary>
    class Locator
    {
        /// <summary>
        /// Evaluate the various locations and return a list of valid ones.
        /// </summary>
        /// <returns></returns>
        public Location[] FindBestLocations()
        {
            return GetAllLocations()
                .Where(l => l.LocationIsGood())
                .ToArray();
        }

        /// <summary>
        /// Return a named location. Note that this doesn't mean the location is good. You must ask if the location is good before using it!
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Location FindLocation (string name)
        {
            return GetAllLocations()
                .Where(l => l.Name == name)
                .FirstOrDefault();
        }

        /// <summary>
        /// All possible locations
        /// </summary>
        private List<Location> _allLocations = null;

        /// <summary>
        /// This is used for tests, a way to inject a non-production configuration into the system.
        /// </summary>
        internal static Func<Dictionary<string, Dictionary<string, string>>> _getLocations = null;

        /// <summary>
        /// Return a list of all locations
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Location> GetAllLocations()
        {
            if (_allLocations == null)
            {
                _allLocations = new List<Location>();

                var infoOnLocations = _getLocations == null ? Config.GetLocationConfigs() : _getLocations();
                foreach (var loc in infoOnLocations)
                {
                    Location newLocation = null;
                    if (loc.Value["LocationType"] == "LinuxWithWindowsReflector")
                    {
                        newLocation = LinuxWithWindowsReflector.GetLocation(loc.Value);
                    }
                    else if (loc.Value["LocationType"] == "LocalWindowsFilesystem")
                    {
                        newLocation = LocalMachine.GetLocation(loc.Value);
                    }
                    else
                    {
                        throw new InvalidOperationException(string.Format("Location '{0}' requires a setup of type '{1}' which we don't understand how to do.", loc.Key, loc.Value["LocationType"]));
                    }
                    newLocation.Priority = int.Parse(loc.Value["Priority"]);
                    _allLocations.Add(newLocation);
                }

            }
            return _allLocations;
        }
    }
}
