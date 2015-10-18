using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Sprache;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class JobParserTest
    {
        [TestMethod]
        public void EmptyFile()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        public void GoodReleaseText()
        {
            var s = "release(Base, 2.3.30)";
            var r = JobParser.ParseRelease.Parse(s);
            Assert.AreEqual("Base, 2.3.30", r.Name);
        }

        [TestMethod]
        public void GoodReleaseTextWithWS()
        {
            var s = "release ( Base, 2.3.30 ) ";
            var r = JobParser.ParseRelease.Parse(s);
            Assert.AreEqual("Base, 2.3.30", r.Name);
        }
    }
}
