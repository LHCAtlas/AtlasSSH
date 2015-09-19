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
        /// Return a list of all locations
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Location> GetAllLocations()
        {
            if (_allLocations == null)
            {
                _allLocations = new List<Location>();
                _allLocations.Add(CERN.GetLocation());
            }
            return _allLocations;
        }
    }
}
