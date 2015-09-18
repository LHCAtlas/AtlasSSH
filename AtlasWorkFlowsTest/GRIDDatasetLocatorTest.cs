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
    }
}
