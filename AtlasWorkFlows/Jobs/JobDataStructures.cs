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

        internal Package Clone()
        {
            return new Package() { Name = this.Name, SCTag = this.SCTag };
        }
    }

    public class Command
    {
        public string CommandLine { get; set; }

        internal Command Clone()
        {
            return new Command() { CommandLine = this.CommandLine };
        }
    }

    public class Submit
    {
        public Command SubmitCommand { get; set; }
        internal Submit Clone()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Holds a submit pattern that is good only for a match to a specific search string.
    /// </summary>
    public class SubmitPattern
    {
        public string RegEx { get; set; }

        public Submit SubmitCommand { get; set; }

        internal SubmitPattern Clone()
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Code release
    /// </summary>
    public class Release
    {
        public string Name { get; set; }

        internal Release Clone()
        {
            return new Release() { Name = this.Name };
        }
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
        public SubmitPattern[] SubmitPatternCommands { get; set; }

        /// <summary>
        /// Do a deep copy of the job.
        /// </summary>
        /// <returns></returns>
        internal AtlasJob Clone()
        {
            var r = new AtlasJob ();

            r.Name = Name;
            r.Version = Version;
            r.Release = Release?.Clone();
            r.Packages = Packages?.Select(p => p.Clone()).ToArray();
            r.Commands = Commands?.Select(c => c.Clone()).ToArray();
            r.SubmitCommand = SubmitCommand?.Clone();
            r.SubmitPatternCommands = SubmitPatternCommands?.Select(sp => sp.Clone()).ToArray();
            return r;
        }
    }

    /// <summary>
    /// Name of the machine and user we should use to log in for submissions.
    /// </summary>
    public class SubmissionMachine
    {
        public string MachineName { get; set; }
        public string Username { get; set; }
    }

    /// <summary>
    /// Everything that can be contained in a single file
    /// </summary>
    public class JobFile
    {
        public AtlasJob[] Jobs { get; set; }
        public SubmissionMachine[] machines { get; set; }
    }

    public static class JobDataStructuresUtils
    {
        /// <summary>
        /// Set the name and version of the job.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public static AtlasJob NameVersionRelease(this AtlasJob job, string name, int version, string release)
        {
            job = job == null ? new AtlasJob() : job;
            job.Name = name;
            job.Version = version;
            job.Release = new Release() { Name = release };
            return job;
        }

        /// <summary>
        /// Add a SC package onto the list so it can be checked out before running.
        /// </summary>
        /// <param name="job">The job we should add a package to</param>
        /// <param name="packageName">The source control path to the package</param>
        /// <param name="SCTag">The source control tag. If it is blank, then the version associated with the release is checked out</param>
        /// <returns></returns>
        public static AtlasJob Package(this AtlasJob job, string packageName, string SCTag = "")
        {
            job = job == null ? new AtlasJob() : job;
            job.Packages = job.Packages
                .Append(new Package() { Name = packageName, SCTag = SCTag });

            return job;
        }

        /// <summary>
        /// Add a new command into the job.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static AtlasJob Command(this AtlasJob job, string commandLine)
        {
            job = job == null ? new AtlasJob() : job;
            job.Commands = job.Commands
                .Append(new Command() { CommandLine = commandLine });
            return job;
        }

        /// <summary>
        /// Add the submit command to the atlas job.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="commandLine"></param>
        /// <returns></returns>
        public static AtlasJob SubmitCommand(this AtlasJob job, string commandLine)
        {
            job = job == null ? new AtlasJob() : job;
            job.SubmitCommand = new Submit() { SubmitCommand = new Jobs.Command() { CommandLine = commandLine } };
            return job;
        }
    }


}
