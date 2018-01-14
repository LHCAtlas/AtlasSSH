using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    /// <summary>
    /// Info about a single file on the internet.
    /// </summary>
    [Serializable]
    public class GRIDFileInfo
    {
        /// <summary>
        /// Fully qualified name of the file
        /// </summary>
        public string name { get; set; }

        /// <summary>
        /// Size (in MB) of the file
        /// </summary>
        public double size { get; set; }

        /// <summary>
        /// Number of events in the file
        /// </summary>
        public int eventCount { get; set; }
    }
}
