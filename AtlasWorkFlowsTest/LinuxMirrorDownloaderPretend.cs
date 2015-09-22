using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest
{
    /// <summary>
    /// Build a sample directory 
    /// </summary>
    public class LinuxMirrorDownloaderPretend : IFetchToRemoteLinuxDir
    {
        private DirectoryInfo _dirHere;
        private string[] _dsNames;
        public LinuxMirrorDownloaderPretend(DirectoryInfo dirHere, params string[] dsnames)
        {
            _dirHere = dirHere;
            _dsNames = dsnames;
            NumberOfTimesWeFetched = 0;
        }

        public int NumberOfTimesWeFetched { get; set; }

        /// <summary>
        /// When we fetch, we just make it looks like it exists on windows now.
        /// </summary>
        /// <param name="dsName"></param>
        /// <param name="linuxDirDestination"></param>
        public void Fetch(string dsName, string linuxDirDestination, Action<string> statsUpdate, Func<string[], string[]> fileFilter = null)
        {
            // Do some basic checks on the Dir destination.
            Assert.IsFalse(linuxDirDestination.Contains(":"));

            utils.BuildSampleDirectory(_dirHere.FullName, fileFilter, _dsNames);
            LinuxDest = linuxDirDestination;

            NumberOfTimesWeFetched++;
        }

        public string LinuxDest { get; private set; }

        /// <summary>
        /// Get the list of files we are going to be creating. This is the full list, without any pruning.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public string[] GetListOfFiles(string dsname, Action<string> status = null)
        {
            var d = new DirectoryInfo("fork-it");
            if (d.Exists)
                d.Delete(true);
            utils.BuildSampleDirectoryBeforeBuild(d.FullName, dsname.SantizeDSName());
            return d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")).Select(f => "user.norm:" + f.Name).ToArray();
        }

        /// <summary>
        /// Simulate a copy from the local guy to the "remote" guy.
        /// </summary>
        /// <param name="linuxLocation"></param>
        /// <param name="directoryInfo"></param>
        public void CopyFromRemote(string linuxLocation, DirectoryInfo directoryInfo, Action<string> status = null)
        {
            // Copy the files and overwrite destination files if they already exist.
            foreach (var s in _dirHere.EnumerateFiles("*.root*", SearchOption.AllDirectories))
            {
                var f = new FileInfo(Path.Combine(directoryInfo.FullName, s.Name));
                if (!f.Exists)
                {
                    s.CopyTo(f.FullName);
                }
            }
        }
    }
}
