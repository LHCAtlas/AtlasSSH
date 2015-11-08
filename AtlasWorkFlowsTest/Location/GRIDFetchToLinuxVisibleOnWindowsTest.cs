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
        public void GetFileListForLocalFilesWithNoWork()
        {
            var dsinfo = MakeDSInfo("ds1.1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("GetFileListForLocalFilesWithNoWork", dsinfo.Name);

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            Assert.AreEqual(5, gf.ListOfFiles("ds1.1.1.1").Length);
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
        public void FindSomeFilesWithScopedDataset()
        {
            var dsinfo = MakeDSInfo("user.norm:ds1.1.1");
            var d = utils.BuildSampleDirectoryBeforeBuild("FindLocationWithScopedDataset", "ds1.1.1");

            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, null, null);
            var r = gf.GetDS(dsinfo, fileFilter: fs => fs.Where(fname => fname.Contains("root.1")).ToArray());
            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
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
        public void AttemptToDownloadBadDS()
        {
            // Seen in wild: try to download a bad dataset, and it creates a directory
            // anyway. Ops!

            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("AttemptToDownloadBadDS");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, "forkitover");
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");

            try {
                var r = gf.GetDS(dsinfo);
            } catch (ArgumentException)
            {
                // Expecting it to throw here - no dataset should exist by that name!
            }

            // Did a local directory get created?
            d.Refresh();
            Assert.IsFalse(d.Exists);
        }

        [TestMethod]
        public void DownloadToLinuxDirectoryThatIsAWindowsDirectoryAndAreadyCreated()
        {
            // Seen in the wild. A crash (or other interruption) means the dataset directory has
            // been created, but is empty (for whatever reason). In that particular case, we should
            // treat it as not there.

            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("DownloadToLinuxDirectoryThatIsAWindowsDirectory");
            if (d.Exists)
            {
                d.Delete(true);
            }
            d.Create();
            var dsdir = d.SubDir(dsinfo.Name);
            dsdir.Create();

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);
            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
            Assert.AreEqual("/bogus/files/store/ds1.1.1", ld.LinuxDest);
        }

        [TestMethod]
        public void MakeSureContentsFileIsCreated()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("MakeSureContentsFileIsCreated");
            if (d.Exists)
            {
                d.Delete(true);
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);

            var f = new FileInfo(Path.Combine(d.FullName, "contents.txt"));
            Assert.IsTrue(f.Exists);
            Assert.IsTrue(f.ReadLine().StartsWith(dsinfo.Name));
        }

        [TestMethod]
        public void MakeSureContentsFileIsUpdated()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("MakeSureContentsFileIsUpdated");
            if (d.Exists)
            {
                d.Delete(true);
            }
            d.Create();
            var f = new FileInfo(Path.Combine(d.FullName, "contents.txt"));
            using (var wr = f.CreateText())
            {
                wr.WriteLine("otherdataset.data.set gwatts");
            }


            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);

            Assert.IsTrue(f.Exists);
            Assert.AreEqual("otherdataset.data.set gwatts", f.ReadLine(1));
            Assert.IsTrue(f.ReadLine(2).StartsWith(dsinfo.Name));
        }

        [TestMethod]
        public void MakeSureContentsFileIsNotUpdated()
        {
            var dsinfo = MakeDSInfo("ds1.1.1");
            var d = new DirectoryInfo("MakeSureContentsFileIsUpdated");
            if (d.Exists)
            {
                d.Delete(true);
            }
            d.Create();
            var f = new FileInfo(Path.Combine(d.FullName, "contents.txt"));
            using (var wr = f.CreateText())
            {
                wr.WriteLine("ds1.1.1 gwatts bogus fight");
            }

            var ld = new LinuxMirrorDownloaderPretend(d, dsinfo.Name);
            var gf = new GRIDFetchToLinuxVisibleOnWindows(d, ld, "/bogus/files/store");
            var r = gf.GetDS(dsinfo);

            Assert.IsTrue(f.Exists);
            Assert.AreEqual("ds1.1.1 gwatts bogus fight", f.ReadLine(1));
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
        /// Create a Dataset Info for debugging.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        private DSInfo MakeDSInfo(string dsname)
        {
            return new DSInfo()
            {
                CanBeGeneratedAutomatically = true,
                Name = dsname,
                IsLocal = null
            };
        }

    }
}
