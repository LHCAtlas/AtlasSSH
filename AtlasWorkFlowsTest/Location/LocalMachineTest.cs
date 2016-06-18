using AtlasSSH;
using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LocalMachineTest
    {
        [TestCleanup]
        public void Cleanup()
        {
            Locator.SetLocationFilter(null);
            Locator.DisableAllLocators(false);
        }

        [TestMethod]
        public void TestName()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            Assert.AreEqual("Local", l.Name);
        }

        [TestMethod]
        public void LocalNotExistingDataset()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("bogus");
            Assert.AreEqual("bogus", r.Name);
            Assert.IsNotNull(r.IsLocal);
            Assert.IsFalse(r.IsLocal(null));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
            Assert.AreEqual(0, r.ListOfFiles().Length);
        }

        [TestMethod]
        public void LocalNotExistingScopedDataset()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("user.gwatts:bogus");
            Assert.AreEqual("user.gwatts:bogus", r.Name);
            Assert.IsNotNull(r.IsLocal);
            Assert.IsFalse(r.IsLocal(null));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesInExistingDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.IsTrue(r.IsLocal(null)); // They are all local.
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
            Assert.AreEqual(5, r.ListOfFiles().Length);
        }

        [TestMethod]
        public void LocalFilesInExistingScopedDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("user.gwatts:ds1.1.1");
            Assert.AreEqual("user.gwatts:ds1.1.1", r.Name);
            Assert.IsTrue(r.IsLocal(null)); // They are all local.
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesInExistingDatasetIsPartial()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDatasetIsPartial", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.IsFalse(r.IsLocal(null));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesExistInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDatasetIsPartial", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.IsTrue(r.IsLocal(fs => fs.Where(fname => fname.Contains(".root.1")).ToArray()));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }
        
        [TestMethod]
        public void GetForCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(5, l.GetDS(r, null, null, null, 0).Length);
        }

        [TestMethod]
        public void LookForLocalFileInCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(r.IsLocal(fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void GetForLocalFileInCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForLocalFileInCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(1, l.GetDS(r, null, fslist => fslist.Take(1).ToArray(), null, 0).Length);
        }

        [TestMethod]
        public void LookForLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(r.IsLocal(fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void GetForLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(1, l.GetDS(r, null, fslist => fslist.Take(1).ToArray(), null, 0).Length);
        }

        [TestMethod]
        public void LookForMissingLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForMissingLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.IsFalse(r.IsLocal(fslist => fslist.Where(f => f.Contains(".5")).Take(1).ToArray()));
        }

        [TestMethod]
        public void LoadNewFilesToLocalWhenMissing()
        {
            // Setup the infrastructure. For Local to fetch files, it will callback into the global
            // infrastructure - so that needs to be setup for the test.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("LoadNewFilesToLocalWhenMissingRemote", dsname);
            var d2 = new DirectoryInfo("LoadNewFilesToLocalWhenMissingLocal");
            if (d2.Exists)
            {
                d2.Delete(true);
            }
            d2.Create();
            d2.Refresh();
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, null, null, 0);
            Assert.AreEqual(5, files.Length);
            Assert.IsTrue(files[0].LocalPath.Contains("LoadNewFilesToLocalWhenMissingLocal"));
            Assert.AreEqual("ds1.1.1", d2.EnumerateDirectories().First().Name);
        }

        [TestMethod]
        public void LoadNewFilesToLocalWhenEmptyDirectory()
        {
            // Seen in the wild. Local directory is actually there for the dataset, but
            // empty due to an earlier crash. Make sure the copy still occurs.

            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("LoadNewFilesToLocalWhenMissingRemote", dsname);
            var d2 = new DirectoryInfo("LoadNewFilesToLocalWhenMissingLocal");
            if (d2.Exists)
            {
                d2.Delete(true);
            }
            d2.Create();
            d2.Refresh();
            d2.SubDir("ds1.1.1").Create();
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, null, null, 0);
            Assert.AreEqual(5, files.Length);
            Assert.IsTrue(files[0].LocalPath.Contains("LoadNewFilesToLocalWhenMissingLocal"));
            Assert.AreEqual("ds1.1.1", d2.EnumerateDirectories().First().Name);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void FailBecauseLocalRepoNotCreated()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("LoadNewFilesToLocalWhenMissingRemote", dsname);
            var d2 = new DirectoryInfo("FailBecauseLocalRepoNotCreated");
            if (d2.Exists)
            {
                d2.Delete(true);
            }
            d2.Refresh();
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, null, null, 0);
        }

        [TestMethod]
        public void LoadTwoFilesToLocalWhenMissing()
        {
            // Setup the infrastructure. For Local to fetch files, it will callback into the global
            // infrastructure - so that needs to be setup for the test.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("LoadNewFilesToLocalWhenMissingRemote", dsname);
            var d2 = new DirectoryInfo("LoadNewFilesToLocalWhenMissingLocal");
            if (d2.Exists)
            {
                d2.Delete(true);
            }
            d2.Create();
            d2.Refresh();
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, fslist => fslist.Take(2).ToArray(), null, 0);
            Assert.AreEqual(2, files.Length);
            Assert.IsTrue(files[0].LocalPath.Contains("LoadNewFilesToLocalWhenMissingLocal"));
            Assert.AreEqual(2, d2.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Count());
        }

        [TestMethod]
        public void FetchOneMissingFileFromRemote()
        {
            // Local is complete in all but one file. Force a fetch of just that one file.
            // Setup the infrastructure. For Local to fetch files, it will callback into the global
            // infrastructure - so that needs to be setup for the test.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("FetchOneMissingFileFromRemoteRemote", dsname);
            var d2 = utils.BuildSampleDirectoryBeforeBuild("FetchOneMissingFileFromRemoteLocal", dsname);
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var fbad = new FileInfo(Path.Combine(d2.FullName, "ds1.1.1", "sub2", "file.root.5"));
            Assert.IsTrue(fbad.Exists);
            fbad.Delete();
            utils.MakePartial(d2, "ds1.1.1");

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, null, null, 0);
            Assert.AreEqual(5, files.Length);
            Assert.IsTrue(files[0].LocalPath.Contains("FetchOneMissingFileFromRemoteLocal"));
            Assert.AreEqual(5, d2.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")).Count());
        }

        [TestMethod]
        public void FetchOneMissingFileFromBackupSource()
        {
            // Dataset isn't here and there is no remote... Backup to the rescue!
            var dsname = "ds1.1.1";
            var d2 = new DirectoryInfo("FetchOneMissingFileFromBackupSourceLocal");
            if (d2.Exists)
            {
                d2.Delete(true);
            }
            d2.Create();
            d2.Refresh();
            Locator._getLocations = () => utils.GetLocal(null, d2);

            // Configure a dummy fetcher that will get the datasets (they will get pulled when things are created).
            // This isn't a real Linux downloader, so we need to put the files in some other place
            var dtemp = new DirectoryInfo("FetchOneMissingFileFromBackupSourceRemoteLinuxDownload");
            if (dtemp.Exists)
            {
                dtemp.Delete(true);
            }
            dtemp.Create();
            FetchToRemoteLinuxDirInstance._test = new LinuxMirrorDownloaderPretend(dtemp, dsname);

            var locator = new Locator();
            var l = locator.FindLocation("MyTestLocalLocation");
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsFalse(r.IsLocal(null));
            var files = l.GetDS(r, null, null, null, 0);
            Assert.AreEqual(5, files.Length);
            Assert.IsTrue(files[0].LocalPath.Contains("FetchOneMissingFileFromBackupSourceLocal"));
            Assert.AreEqual(5, d2.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")).Count());
        }

        [TestMethod]
        [Ignore]
        public void FetchOneFileWithNoBackupOverNetwork()
        {
            // This is a real run (so it is slow), and it will fetch data over the network.
            var d = new DirectoryInfo("FetchOneFileWithNoBackupOverNetwork");
            if (d.Exists)
            {
                d.Delete(true);
            }
            d.Create();

            var c = GenerateLocalConfigWithWorkingRemote(d);
            Locator.DisableAllLocators(true);

            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("user.gwatts:user.gwatts.301295.EVNT.1");
            Assert.AreEqual(1, l.GetDS(r, rep => Console.WriteLine(rep), fslist => fslist.Take(1).ToArray(), null, 0).Length);

            var dsdir = d.SubDir("user.gwatts.301295.EVNT.1");
            Assert.IsTrue(dsdir.Exists);
            Assert.AreEqual(1, dsdir.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Count());

            // Next, make sure that there are no files left over up on the remote machine.
            var username = c["LinuxUserName"];
            var machine = c["LinuxHost"];

            using (var s = new SSHConnection(machine, username))
            {
                bool stillThere = false;
                s.ExecuteCommand(string.Format("ls {0}", c["LinuxTempLocation"]),
                    outLine =>
                    {
                        Console.WriteLine("output of ls: " + outLine);
                        stillThere |= !outLine.Contains("cannot access");
                    }
                    );
                Assert.IsFalse(stillThere);
            }

        }

        /// <summary>
        /// Generate the local configuration
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GenerateLocalConfig(DirectoryInfo local = null)
        {
            var paths = ".";
            if (local != null)
            {
                paths = local.FullName;
            }

            return new Dictionary<string, string>()
            {
                {"Name", "Local"},
                {"Paths", paths},
                {"LinuxFetcherType", "LinuxFetcher"},
                {"LinuxHost", "bogus.nytimes.com"},
                {"LinuxUserName", "whereAmI"},
            };
        }

        /// <summary>
        /// Generate the local configuration with real values
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GenerateLocalConfigWithWorkingRemote(DirectoryInfo local = null)
        {
            var paths = ".";
            if (local != null)
            {
                paths = local.FullName;
            }

            return new Dictionary<string, string>()
            {
                {"Name", "Local"},
                {"Paths", paths},
                {"LinuxFetcherType", "LinuxFetcher"},
                {"LinuxHost", "tev01.phys.washington.edu"},
                {"LinuxUserName", "gwatts"},
                {"LinuxTempLocation", "/tmp/gwatts/tmpdata" },
            };
        }
    }
}
