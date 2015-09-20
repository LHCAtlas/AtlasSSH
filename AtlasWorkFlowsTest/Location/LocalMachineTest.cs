using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AtlasWorkFlows.Locations;
using System.IO;

namespace AtlasWorkFlowsTest.Location
{
    [TestClass]
    public class LocalMachineTest
    {
        [TestMethod]
        public void TestName()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            Assert.AreEqual("Local", l.Name);
        }

        [TestMethod]
        public void LocalNotExistingDataset()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("bogus");
            Assert.AreEqual("bogus", r.Name);
            Assert.AreEqual(0, r.NumberOfFiles);
            Assert.IsFalse(r.IsLocal);
            Assert.IsFalse(r.IsPartial);
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesInExistingDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.AreEqual(5, r.NumberOfFiles);
            Assert.IsTrue(r.IsLocal);
            Assert.IsFalse(r.IsPartial);
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesInExistingDatasetIsPartial()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDatasetIsPartial", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.AreEqual(5, r.NumberOfFiles);
            Assert.IsTrue(r.IsLocal);
            Assert.IsTrue(r.IsPartial);
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        /// <summary>
        /// Generate the local configuration
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GenerateLocalConfig(DirectoryInfo local = null)
        {
            var paths = ".";
            if (local != null)
            {
                paths = local.FullName;
            }

            return new Dictionary<string, string>()
            {
                {"Name", "Local"},
                {"Paths", paths}
            };
        }
    }
}
