using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprache;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class JobPrintTest
    {
        [TestMethod]
        public void ReleaseOutput()
        {
            var r = new Release() { Name = "Base,2222" };
            Assert.AreEqual("release(Base,2222)", r.Print());
            var rp = JobParser.ParseRelease.Parse(r.Print());
            Assert.AreEqual(r.Name, rp.Name);
        }
    }
}
