using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.Linq;
using AtlasWorkFlows.Utils;
using System.IO;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LinuxWithWindowsReflectorTest
    {
        [TestInitialize]
        public void SetupConfig()
        {
            Locator._getLocations = () => utils.GetLocal(new DirectoryInfo(@"C:\"));
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            Locator._getLocations = null;
        }

        [TestMethod]
        public void CERNLocationName()
        {
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Assert.AreEqual("MyTestLocation", c.Name);
        }

        [TestMethod]
        public void CERNLocationAtUW()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.washington.phys.washington.edu");
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Assert.IsFalse(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Assert.IsTrue(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNSecondLocation()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.ch.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Assert.IsTrue(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtWhereWeAre()
        {
            AtlasWorkFlows.Utils.IPLocationTests.ResetIpName();
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Console.WriteLine("Are we at CERN? : {0}", c.LocationIsGood());
        }

        [TestMethod]
        public void CERNFetchDatasetInfo()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            var dsinfo = c.GetDSInfo("bogus.dataset.version.1");
            Assert.IsNotNull(dsinfo);
            Assert.AreEqual(true, dsinfo.CanBeGeneratedAutomatically);
            Assert.AreEqual(false, dsinfo.IsLocal(null));
            Assert.AreEqual("bogus.dataset.version.1", dsinfo.Name);
        }

        [TestMethod]
        public void CERNFetchDSInfoForFullDS()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("CERNFetchDSInfoForFullDS", "bogus.dataset.version.1");
            var configInfo = utils.GetLocal(dataStore);
            var c = LinuxWithWindowsReflector.GetLocation(configInfo["MyTestLocation"]);
            var dsinfo = c.GetDSInfo("bogus.dataset.version.1");
            Assert.IsNotNull(dsinfo);
            Assert.IsTrue(dsinfo.IsLocal(null));
        }

        [TestMethod]
        public void CERNFetchDSInfoForPartialDS()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dataStore = utils.BuildSampleDirectoryBeforeBuild("CERNFetchDSInfoForPartialDS", "bogus.dataset.version.1");
            utils.MakePartial(dataStore, "bogus.dataset.version.1");
            var configInfo = utils.GetLocal(dataStore);
            var c = LinuxWithWindowsReflector.GetLocation(configInfo["MyTestLocation"]);
            var dsinfo = c.GetDSInfo("bogus.dataset.version.1");
            var badFile = new FileInfo(Path.Combine(dataStore.FullName, dsinfo.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.IsNotNull(dsinfo);
            Assert.IsFalse(dsinfo.IsLocal(null));
        }

        [TestMethod]
        public void CERNFetchDatasetGood()
        {
            // We can't really test the full cern fetch here, we'll do that elsewhere.
            // But do make sure we get something valid back here.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Locator.GetMasterConfig()["MyTestLocation"]);
            Assert.IsNotNull(c.GetDS);
        }
    }
}
