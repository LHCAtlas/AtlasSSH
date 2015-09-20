using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows;
using AtlasWorkFlows.Utils;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class GRIDFetchToLinuxVisibleOnWindowsTest
    {
        [TestMethod]
        public void FindLocalFilesWithNoWork()
        {
            var dsinfo = MakeDSInfo("ds1.1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("FindLocalFilesWithNoWork", dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void CheckNonPartialDataset()
        {
            var dsinfo = MakeDSInfo("ds1.1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("FindLocalFilesWithNoWork", dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            Assert.IsFalse(gf.CheckIfPartial(dsinfo.Name));
        }

        [TestMethod]
        public void CheckPartialDataset()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("FindLocalFilesWithNoWork", dsinfo.Name);
            utils.MakePartial(d, dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            Assert.IsTrue(gf.CheckIfPartial(dsinfo.Name));
        }

        [TestMethod]
        public void CountFilesDownloaded()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("CountFilesDownloaded", dsinfo.Name);
            utils.MakePartial(d, dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            Assert.AreEqual(5, gf.CheckNumberOfFiles(dsinfo.Name));
        }

        [TestMethod]
        public void FindLocationWithScopedDataset()
        {
            var dsinfo = MakeDSInfo("user.norm:ds1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("FindLocationWithScopedDataset", "ds1.1.1");

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

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
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
            var d = new DirectoryInfo("DownloadLimitedNumberOfFiles");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.AreEqual(2, r.Length);
            Assert.AreEqual(2, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.FullName.EndsWith(".part")).Count());
        }

        [TestMethod]
        public void CheckPartialDownloadIsCorrectlyMarked()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("CheckPartialDownloadIsCorrectlyMarked");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.IsTrue(gf.CheckIfPartial(dsinfo.Name));
        }

        [TestMethod]
        public void CheckFullDownloadIsCorrectlyMarked()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("CheckPartialDownloadIsCorrectlyMarked");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);
            Assert.IsFalse(gf.CheckIfPartial(dsinfo.Name));
        }

        [TestMethod]
        public void CheckFullDownloadWithNullFilterIsCorrectlyMarked()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("CheckPartialDownloadIsCorrectlyMarked");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist);
            Assert.IsFalse(gf.CheckIfPartial(dsinfo.Name));
        }

        [TestMethod]
        public void DownloadLimitedNumberOfFilesTwice()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadLimitedNumberOfFilesTwice");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.AreEqual(2, r.Length);

            r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.AreEqual(2, r.Length);
            Assert.AreEqual(1, ld.NumberOfTimesWeFetched);

            Assert.AreEqual(2, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.FullName.EndsWith(".part")).Count());
        }

        [TestMethod]
        public void DownloadMoreFilesThanFirstTime()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadMoreFilesThanFirstTime");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(1).ToArray());
            Assert.AreEqual(1, r.Length);

            r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(2).ToArray());
            Assert.AreEqual(2, r.Length);

            Assert.AreEqual(2, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.FullName.EndsWith(".part")).Count());
        }

        [TestMethod]
        public void DownloadWeirdFileSecondTime()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadMoreFilesThanFirstTime");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(1).ToArray());
            Assert.AreEqual(1, r.Length);

            r = gf.GetDS(dsinfo, fileFilter: flist => flist.Where(f => f.Contains("5")).ToArray());
            Assert.AreEqual(1, r.Length);
            Assert.IsTrue(r[0].LocalPath.Contains("file.root.5"), r[0].LocalPath);

            Assert.AreEqual(2, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.FullName.EndsWith(".part")).Count());
        }

        [TestMethod]
        public void DownloadAllFilesSecondTime()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadAllFilesSecondTime");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo, fileFilter: flist => flist.OrderBy(f => f).Take(1).ToArray());
            Assert.AreEqual(1, r.Length);

            r = gf.GetDS(dsinfo);
            Assert.AreEqual(5, r.Length);

            Assert.AreEqual(5, d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.FullName.EndsWith(".part")).Count());
            Assert.IsFalse(gf.CheckIfPartial(dsinfo.Name));
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
            public string[] GetListOfFiles(string dsname)
            {
                var d = new DirectoryInfo("forkit");
                if (d.Exists)
                    d.Delete(true);
                utils.BuildSampleDirectoryBeforeBuild(d.FullName, dsname.SantizeDSName());
                return d.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")).Select(f => "user.norm:" + f.Name).ToArray();
            }
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
