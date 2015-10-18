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

        [TestMethod]
        public void CommandOutput()
        {
            var r = new Command() { CommandLine = "ls" };
            Assert.AreEqual("command(ls)", r.Print());
            var rp = JobParser.ParseCommand.Parse(r.Print());
            Assert.AreEqual(r.CommandLine, rp.CommandLine);
        }

        [TestMethod]
        public void SubmitOutput()
        {
            var r = new Submit() { SubmitCommand = new Command { CommandLine = "ls" } };
            Assert.AreEqual("submit(ls)", r.Print());
            var rp = JobParser.ParseSubmit.Parse(r.Print());
            Assert.AreEqual(r.SubmitCommand.CommandLine, rp.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void PackageOutput()
        {
            var r = new Package() { Name = "pkg", SCTag = "1231" };
            Assert.AreEqual("package(pkg,1231)", r.Print());
            var rp = JobParser.ParsePackage.Parse(r.Print());
            Assert.AreEqual(r.Name, rp.Name);
            Assert.AreEqual(r.SCTag, rp.SCTag);
        }
    }
}
