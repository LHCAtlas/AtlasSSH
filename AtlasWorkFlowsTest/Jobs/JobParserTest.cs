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

        [TestMethod]
        public void GoodCommand()
        {
            var s = "command(ls dudemyfood)";
            var c = JobParser.ParseCommand.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.CommandLine);
        }

        [TestMethod]
        public void GoodCommandWithWS()
        {
            var s = "command ( ls dudemyfood ) ";
            var c = JobParser.ParseCommand.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.CommandLine);
        }

        [TestMethod]
        public void GoodSubmit()
        {
            var s = "submit(ls dudemyfood)";
            var c = JobParser.ParseSubmit.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodSubmitWithWS()
        {
            var s = "submit ( ls dudemyfood ) ";
            var c = JobParser.ParseSubmit.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodPackage()
        {
            var s = "package(pk,1234)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("1234", c.SCTag);
        }

        [TestMethod]
        public void GoodPackageWithWS()
        {
            var s = "package ( pk , 1234 ) ";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("1234", c.SCTag);
        }
    }
}
