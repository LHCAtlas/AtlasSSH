using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains the data structures (but no methods!) for the
/// jobs that can be run on the grid or locally or whatever.
/// </summary>
namespace AtlasWorkFlows.Jobs
{
    public class Package
    {
        public string Name { get; set; }

        /// <summary>
        /// The source control tag
        /// </summary>
        public string SCTag { get; set; }
    }

    public class Command
    {
        public string CommandLine { get; set; }
    }

    public class Submit
    {
        public Command SubmitCommand { get; set; }
    }

    /// <summary>
    /// Code release
    /// </summary>
    public class Release
    {
        public string Name { get; set; }
    }


    /// <summary>
    /// The job data structures used to hold the definition of a job in memory.
    /// </summary>
    public class AtlasJob
    {
        public string Name { get; set; }
        public int Version { get; set; }
        public Release Release { get; set; }
        public Package[] Packages { get; set; }
        public Command[] Commands { get; set; }
        public Submit SubmitCommand { get; set; }
    }
}
