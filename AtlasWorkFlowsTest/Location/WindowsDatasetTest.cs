using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.IO;

namespace AtlasWorkFlowsTest.Location
{
    /// <summary>
    /// Some extra tests for WindowsDataset
    /// This isn't tested as well because it is mostly refactored code from LinuxWithWindowsReflector.
    /// As a result a lot of its testing happens here.
    /// </summary>
    [TestClass]
    public class WindowsDatasetTest
    {
        [TestMethod]
        public void GetFileListFromDir()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("GetFileListFromDir", "ds1.1.1");
            var configInfo = utils.GetLocal(dataStore);

            var w = new WindowsDataset(dataStore);
            var list = w.ListOfDSFiles("ds1.1.1");
            Assert.AreEqual(5, list.Length);
        }

        [TestMethod]
        public void GetFileListFromDirWithoutFileList()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("GetFileListFromDirWithoutFileList", "ds1.1.1");
            var f = new FileInfo(Path.Combine(dataStore.FullName, "ds1.1.1", "aa_dataset_complete_file_list.txt"));
            Assert.IsTrue(f.Exists);
            f.Delete();

            var configInfo = utils.GetLocal(dataStore);

            var w = new WindowsDataset(dataStore);
            var list = w.ListOfDSFiles("ds1.1.1");
            Assert.AreEqual(5, list.Length);
        }

        [TestMethod]
        public void GetFileListFromDirWithoutFileListOneMissing()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("GetFileListFromDirWithoutFileList", "ds1.1.1");
            var f = new FileInfo(Path.Combine(dataStore.FullName, "ds1.1.1", "aa_dataset_complete_file_list.txt"));
            Assert.IsTrue(f.Exists);
            f.Delete();
            var fdata = new FileInfo(Path.Combine(dataStore.FullName, "ds1.1.1", "sub1", "file.root.1"));
            Assert.IsTrue(fdata.Exists);
            fdata.Delete();

            var configInfo = utils.GetLocal(dataStore);

            var w = new WindowsDataset(dataStore);
            var list = w.ListOfDSFiles("ds1.1.1");
            Assert.AreEqual(4, list.Length);
        }

        [TestMethod]
        public void GetFileListFromMissingList()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("GetFileListFromMissingList", "ds1.1.1");

            var configInfo = utils.GetLocal(dataStore);

            var w = new WindowsDataset(dataStore);
            var list = w.ListOfDSFiles("bogus");
            Assert.AreEqual(0, list.Length);
        }
    }
}
