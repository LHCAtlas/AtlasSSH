using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.Linq;
using AtlasWorkFlows.Utils;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LinuxWithWindowsReflectorTest
    {
        [TestMethod]
        public void CERNLocationName()
        {
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            Assert.AreEqual("CERN", c.Name);
        }

        [TestMethod]
        public void CERNLocationAtUW()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.washington.phys.washington.edu");
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            Assert.IsFalse(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            Assert.IsTrue(c.LocationIsGood());
        }

        [TestMethod]
        public void CERNLocationAtWhereWeAre()
        {
            AtlasWorkFlows.Utils.IPLocationTests.ResetIpName();
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            Console.WriteLine("Are we at CERN? : {0}", c.LocationIsGood());
        }

        [TestMethod]
        public void CERNFetchDatasetInfo()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            var dsinfo = c.GetDSInfo("bogus.dataset.version.1");
            Assert.IsNotNull(dsinfo);
            Assert.AreEqual(true, dsinfo.CanBeGeneratedAutomatically);
            Assert.AreEqual(false, dsinfo.IsLocal);
            Assert.AreEqual("bogus.dataset.version.1", dsinfo.Name);
            Assert.AreEqual(0, dsinfo.NumberOfFiles);
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
            Assert.IsTrue(dsinfo.IsLocal);
            Assert.IsFalse(dsinfo.IsPartial);
            Assert.AreEqual(5, dsinfo.NumberOfFiles);
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
            Assert.IsNotNull(dsinfo);
            Assert.IsTrue(dsinfo.IsLocal);
            Assert.IsTrue(dsinfo.IsPartial);
            Assert.AreEqual(5, dsinfo.NumberOfFiles);
        }

        [TestMethod]
        public void CERNFetchDatasetGood()
        {
            // We can't really test the full cern fetch here, we'll do that elsewhere.
            // But do make sure we get something valid back here.
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var c = LinuxWithWindowsReflector.GetLocation(Config.GetLocationConfigs()["CERN"]);
            Assert.IsNotNull(c.GetDS);
        }
    }
}
