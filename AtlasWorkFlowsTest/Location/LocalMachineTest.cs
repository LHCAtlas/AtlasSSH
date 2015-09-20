using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using AtlasWorkFlows.Locations;
using System.Linq;
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

        [TestMethod]
        public void LookForCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(l.HasAllFiles(r, null));
        }

        [TestMethod]
        public void LookForLocalFileInCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(l.HasAllFiles(r, fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void LookForLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(l.HasAllFiles(r, fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void LookForMissingLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForMissingLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.IsFalse(l.HasAllFiles(r, fslist => fslist.Where(f => f.Contains(".5")).Take(1).ToArray()));
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
