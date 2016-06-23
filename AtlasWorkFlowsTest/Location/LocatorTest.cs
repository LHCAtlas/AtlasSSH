using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LocatorTest
    {
        [TestInitialize]
        public void SetupConfig()
        {
            Locator._getLocations = () => utils.GetLocal(new DirectoryInfo(@"C:\"));
            Locator.ResetLocationCache();
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            Locator._getLocations = null;
            Locator.SetLocationFilter(null);
            AtlasWorkFlows.Utils.IPLocationTests.ResetIpName();
        }

        [TestMethod]
        public void CERNAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var locator = new Locator();
            var lst = locator.FindBestLocations();
            Assert.IsNotNull(lst);
            var cern = lst.Where(l => l.Name == "MyTestLocation").FirstOrDefault();
            Assert.IsNotNull(cern);
        }

        [TestMethod]
        public void NormalCall()
        {
            // Make sure it doesn't break.
            var location = new Locator();
            var ls = location.FindBestLocations();
            Assert.IsNotNull(ls);
        }

        [TestMethod]
        public void CERNNotAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.nytimes.com");
            var locator = new Locator();
            var lst = locator.FindBestLocations();
            Assert.IsNotNull(lst);
            var cern = lst.Where(l => l.Name == "MyTestLocation").FirstOrDefault();
            Assert.IsNull(cern);
        }

        [TestMethod]
        public void GetCERNLocation()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.nytimes.com");
            var locator = new Locator();
            var lst = locator.FindLocation("MyTestLocation");
            Assert.IsNotNull(lst);
        }

        [TestMethod]
        public void GetBadLocation()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.nytimes.com");
            var locator = new Locator();
            var lst = locator.FindLocation("FeakoutLocation");
            Assert.IsNull(lst);
        }

        [TestMethod]
        public void ScreenLocations()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            Locator.SetLocationFilter(loc => false);
            var locator = new Locator();
            var lst = locator.FindBestLocations();
            Assert.IsNotNull(lst);
            Assert.AreEqual(0, lst.Length);
        }
    }
}
