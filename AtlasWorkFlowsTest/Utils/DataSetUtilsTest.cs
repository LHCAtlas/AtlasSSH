using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Utils;

namespace AtlasWorkFlowsTest.Utils
{
    [TestClass]
    public class DataSetUtilsTest
    {
        [TestMethod]
        public void TestUnadornedDSName()
        {
            Assert.AreEqual("ds1.1.1", "ds1.1.1".SantizeDSName());
        }

        [TestMethod]
        public void TestScopedDSName()
        {
            Assert.AreEqual("ds1.1.1", "user.heynorm:ds1.1.1".SantizeDSName());
        }
    }
}
