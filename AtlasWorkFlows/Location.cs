using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// Represents a location that the program is running in
    /// </summary>
    public class Location
    {
        /// <summary>
        /// Simple name like "CERN" or "UW"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The list of tests that must be satisfied to be in this location.
        /// </summary>
        /// <remarks>
        /// Location is independent of dataset name.
        /// </remarks>
        public List<Func<bool>> LocationTests { get; private set; }

        public bool LocationIsGood()
        {
            return LocationTests.Select(t => t()).All(p => p);
        }

        /// <summary>
        /// Delegate to return information given a dataset name
        /// </summary>
        public Func<string, DSInfo> GetDSInfo { get; set; }

        /// <summary>
        /// Given a DS Info, this function will return a list of URI's that are accessible by
        /// ROOT on the machine we are currently running on.
        /// </summary>
        /// <remarks>
        /// This will return all files it can get it. If it is in complete for some reason, and
        /// the request can't be filled out, it will still return whatever files it can find.
        /// Use HasAllFiles to make sure.
        /// </remarks>
        public Func<DSInfo, Action<string>, Func<string[], string[]>, Func<bool>, Uri[]> GetDS { get; set; }

        /// <summary>
        /// If you have more than one store and they will be able to answer similar with others,
        /// this is a way of doing it. Higher priority is better.
        /// </summary>
        /// <remarks>
        /// As an example, if you have the same dataset downloaded remotely and also locally, you want the
        /// local one to win out (usually...), so make sure it has a higher priority.
        /// </remarks>
        public int Priority { get; set; }

        /// <summary>
        /// Setup default object configuration
        /// </summary>
        public Location()
        {
            LocationTests = new List<Func<bool>>();
        }
    }
}
