using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows;

namespace AtlasWorkFlowsTest
{
    [TestClass]
    public class GRIDDatasetLocatorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AskForWSDataset()
        {
            GRIDDatasetLocator.FetchDatasetUris(" ");
        }

        [TestMethod]
        public void DatasetAlreadyAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectory("DatasetAlreadyAtCERN", dsname);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void DatasetAlreadyLocal()
        {
            Assert.Inconclusive();
        }

        // ALso, need tests for small numbers of files.
    }
}
