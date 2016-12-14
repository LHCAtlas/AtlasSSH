using AtlasSSHTest;
using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AtlasWorkFlows.Jobs.JobUtils;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class JobUtilsTest
    {
        [TestMethod]
        public void HashIsDifferent()
        {
            var j = new AtlasJob() { Name = "Hi there", Version = 12 };
            var h1 = j.Hash();
            j.Name = "not there";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }

        [TestMethod]
        public void HashDifferentForComplexJob()
        {
            AtlasJob j = MakeSimpleJob();
            var h1 = j.Hash();
            j.Packages[0].SCTag = "tag1";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }

        [TestMethod]
        public void HashDifferentForSubmitCommands()
        {
            var j = MakeSimpleJob();
            var h1 = j.Hash();
            j.SubmitCommand.SubmitCommand.CommandLine = "forkitover";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }

        [TestMethod]
        public void HashDifferentForDifferentPatterns()
        {
            var j = MakePatternJob();
            var h1 = j.Hash();
            j.SubmitPatternCommands[0].RegEx = "anotheronebitesthedust";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }

        [TestMethod]
        public void HashDifferentSubmitsInPatternNotAProblem()
        {
            var j = MakePatternJob();
            var h1 = j.Hash();
            j.SubmitCommand.SubmitCommand.CommandLine = "anotherone is crazy";
            var h2 = j.Hash();
            Assert.AreEqual(h1, h2);
        }

        private static AtlasJob MakeSimpleJob()
        {
            return new AtlasJob()
            {
                Commands = new Command[] { new Command() { CommandLine = "ls" } },
                Name = "MyJob",
                Version = 1234,
                Release = new Release() { Name = "notmyrelease" },
                SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "submit" } },
                Packages = new Package[] { new Package() { Name = "myexperiment/off/firstlevel/hithere", SCTag = "tag" } },
                SubmitPatternCommands = new SubmitPattern[0]
            };
        }

        private static AtlasJob MakePatternJob()
        {
            var j = MakeSimpleJob();
            j.SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "neveruseorerror" } };

            j.SubmitPatternCommands = new SubmitPattern[2];
            j.SubmitPatternCommands[0] = new SubmitPattern() { RegEx = "ds1", SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "ds1_submit" } } };
            j.SubmitPatternCommands[1] = new SubmitPattern() { RegEx = "ds2", SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "ds2_submit" } } };

            return j;
        }

        [TestMethod]
        public void SubmitCommandWithGlobal()
        {
            AtlasJob j = MakeSimpleJob();
            var s = new dummySSHConnection(new Dictionary<string, string>().AddSubmitInfo(j));
            s.SubmitJob(j, "ds1", "ds1-out", credSet: "bogus");
        }

        [TestMethod]
        public void SubmitCommandWithGlobalPattern()
        {
            AtlasJob j = MakePatternJob();
            var s = new dummySSHConnection(new Dictionary<string, string>().AddSubmitInfo(j, "ds1_submit"));
            s.SubmitJob(j, "ds1", "ds1-out", credSet: "bogus");
        }

        [TestMethod]
        [ExpectedException(typeof(GRIDSubmitException))]
        public void SubmitCommandWithAmbiguousPattern()
        {
            AtlasJob j = MakePatternJob();
            var s = new dummySSHConnection(new Dictionary<string, string>().AddSubmitInfo(j, "ds1_submit"));
            s.SubmitJob(j, "ds1ds2", "ds1-out", credSet: "bogus");
        }

        [TestMethod]
        [ExpectedException(typeof(GRIDSubmitException))]
        public void SubmitCommandWithMissingPattern()
        {
            AtlasJob j = MakePatternJob();
            var s = new dummySSHConnection(new Dictionary<string, string>().AddSubmitInfo(j, "ds1_submit"));
            s.SubmitJob(j, "ds3", "ds1-out", credSet: "bogus");
        }
    }

    static class JobUtilsTestHelpers
    {
        public static Dictionary<string, string> AddSubmitInfo(this Dictionary<string,string> source, AtlasJob j, string submitCommand = "submit")
        {
            var d = new Dictionary<string, string>()
            {
            { "rm -rf /tmp/ds1-out", "" },
                { "lsetup panda", "" },
                { "mkdir /tmp/ds1-out", "" },
                { "cd /tmp/ds1-out", "" },
                { "rcSetup notmyrelease", "Found ASG release with" },
                { "echo fubar | kinit bogus@mydomain.CH", "Password for bogus@mydomain.CH: " },
                { $"rc checkout_pkg {j.Packages[0].Name}/trunk@tag", "Checked out revision" },
                { "mv trunk@tag hithere", "" },
                { "rc find_packages", "" },
                { "rc compile", "" },
                { "ls", "" },
                { "echo $?", "0" },
                { submitCommand, "dude" }
                };

            return source.AddEntry(d);
        }

        public static Dictionary<string,string> AddEntry(this Dictionary<string, string> source, Dictionary<string, string> additional)
        {
            var d = new Dictionary<string, string>();
            foreach (var ent in source)
            {
                d.Add(ent.Key, ent.Value);
            }
            foreach (var ent in additional)
            {
                d.Add(ent.Key, ent.Value);
            }
            return d;
        }
    }
}
