using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// An IPlace that supports a SCP target.
    /// Note: You must check to make sure IsVisible(location) is true before
    /// attempting, or you'll face a long timeout (e.g. behavior undefined).
    /// </summary>
    interface ISCPTarget
    {
        /// <summary>
        /// Return true if the machine's SCP end points can be accessed from
        /// the machine with the ip address <paramref name="internetLocation"/>.
        /// </summary>
        /// <param name="internetLocation">ip address of location that will be sourcing the copy.</param>
        /// <returns></returns>
        bool IsVisibleFrom(string internetLocation);
    }
}
