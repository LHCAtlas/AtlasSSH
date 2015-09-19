using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class GRIDFetchToLinuxVisibleOnWindowsTest
    {
        [TestMethod]
        public void FindLocalFilesWithNoWork()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = utils.BuildSampleDirectory("FindLocalFilesWithNoWork", dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void FindLocationWithScopedDataset()
        {
            var dsinfo = MakeDSInfo("user.norm:ds1.1.1");
            var d = utils.BuildSampleDirectory("FindLocationWithScopedDataset", "ds1.1.1");

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void DownloadToLinuxDirectoryThatIsAWindowsDirectory()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadToLinuxDirectoryThatIsAWindowsDirectory");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, "ds1.1.1");
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
            Assert.AreEqual("/bogus/files/store/ds1.1.1", ld.LinuxDest);
        }

        [TestMethod]
        public void DownloadLimitedNumberOfFiles()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadToLinuxDirectoryThatIsAWindowsDirectory");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, "ds1.1.1");
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.AreEqual(2, r.Length);
            Assert.AreEqual(2, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Count());
        }

        [TestMethod]
        public void DownloadToLinuxDirectoryThatIsAWindowsDirectoryWithScoppedDS()
        {
            var dsinfo = MakeDSInfo("user.norm:ds1.1.1");
            var d = new DirectoryInfo("DownloadToLinuxDirectoryThatIsAWindowsDirectory");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, "ds1.1.1");
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
            Assert.AreEqual("/bogus/files/store/ds1.1.1", ld.LinuxDest);
        }

        /// <summary>
        /// Build a sample directory 
        /// </summary>
        private class LinuxMirrorDownloaderPretend : IFetchToRemoteLinuxDir
        {
            private DirectoryInfo _dirHere;
            private string[] _dsNames;
            public LinuxMirrorDownloaderPretend(DirectoryInfo dirHere, params string[] dsnames)
            {
                _dirHere = dirHere;
                _dsNames = dsnames;
            }

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
            }

            public string LinuxDest { get; private set; }
        }

        /// <summary>
        /// Create a Dataset Info for debugging.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private DSInfo MakeDSInfo(string dsname)
        {
            return new DSInfo()
            {
                CanBeGenerated = true,
                Name = dsname,
                NumberOfFiles = 0,
                IsLocal = false
            };
        }

    }
}
