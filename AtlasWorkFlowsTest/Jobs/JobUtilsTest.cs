using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class JobUtilsTest
    {
        [TestMethod]
        public void HashIsDifferent()
        {
            var j = new Job() { Name = "Hi there", Version = 12 };
            var h1 = j.Hash();
            j.Name = "not there";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }

        [TestMethod]
        public void HashDifferentForComplexJob()
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
            var h1 = j.Hash();
            j.Packages[0].SCTag = "tag1";
            var h2 = j.Hash();
            Assert.AreNotEqual(h1, h2);
        }
    }
}
