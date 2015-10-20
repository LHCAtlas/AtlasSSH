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

        [TestMethod]
        public void JobOutput()
        {
            var j = new Job()
            {
                Commands = new Command[] { new Command() { CommandLine = "ls" } },
                Name = "MyJob",
                Version = 1234,
                Release = new Release() { Name = "notmyrelease" },
                SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "submit" } },
                Packages = new Package[] { new Package() { Name = "hithere", SCTag = "tag" } }
            };
            Assert.AreEqual("job(MyJob,1234){release(notmyrelease)package(hithere,tag)submit(submit)}", j.Print());
        }

        [TestMethod]
        public void PrintEmptyJob()
        {
            var j = new Job() { Name = "hihere", Version = 10 };
            var s = j.Print();
            Assert.AreEqual("job(hihere,10){}", s);
        }
    }
}
