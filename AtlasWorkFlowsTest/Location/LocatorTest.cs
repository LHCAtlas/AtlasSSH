using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Locations;
using System.Linq;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LocatorTest
    {
        [TestMethod]
        public void CERNAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var locator = new Locator();
            var lst = locator.FindBestLocations();
            Assert.IsNotNull(lst);
            var cern = lst.Where(l => l.Name == "CERN").FirstOrDefault();
            Assert.IsNotNull(cern);
        }

        [TestMethod]
        public void CERNNotAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.nytimes.com");
            var locator = new Locator();
            var lst = locator.FindBestLocations();
            Assert.IsNotNull(lst);
            var cern = lst.Where(l => l.Name == "CERN").FirstOrDefault();
            Assert.IsNull(cern);
        }

        [TestMethod]
        public void GetCERNLocation()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("bogus.nytimes.com");
            var locator = new Locator();
            var lst = locator.FindLocation("CERN");
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
    }
}
