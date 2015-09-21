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
            Assert.IsNotNull(r.IsLocal);
            Assert.IsFalse(r.IsLocal(null));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalNotExistingScopedDataset()
        {
            var c = GenerateLocalConfig();
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("user.gwatts:bogus");
            Assert.AreEqual("user.gwatts:bogus", r.Name);
            Assert.IsNotNull(r.IsLocal);
            Assert.IsFalse(r.IsLocal(null));
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
            Assert.IsTrue(r.IsLocal(null)); // They are all local.
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesInExistingScopedDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("user.gwatts:ds1.1.1");
            Assert.AreEqual("user.gwatts:ds1.1.1", r.Name);
            Assert.IsTrue(r.IsLocal(null)); // They are all local.
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
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.IsFalse(r.IsLocal(null));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }

        [TestMethod]
        public void LocalFilesExistInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LocalFilesInExistingDatasetIsPartial", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            var badFile = new FileInfo(Path.Combine(d.FullName, r.Name, "sub2", "file.root.5"));
            Assert.IsTrue(badFile.Exists);
            badFile.Delete();
            Assert.AreEqual("ds1.1.1", r.Name);
            Assert.IsTrue(r.IsLocal(fs => fs.Where(fname => fname.Contains(".root.1")).ToArray()));
            Assert.IsFalse(r.CanBeGeneratedAutomatically);
        }
        
        [TestMethod]
        public void GetForCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(5, l.GetDS(r, null, null).Length);
        }

        [TestMethod]
        public void LookForLocalFileInCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(r.IsLocal(fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void GetForLocalFileInCompleteDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForLocalFileInCompleteDataset", "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(1, l.GetDS(r, null, fslist => fslist.Take(1).ToArray()).Length);
        }

        [TestMethod]
        public void LookForLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("LookForLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.IsTrue(r.IsLocal(fslist => fslist.Take(1).ToArray()));
        }

        [TestMethod]
        public void GetForLocalFileInPartialDataset()
        {
            var d = utils.BuildSampleDirectoryBeforeBuild("GetForLocalFileInPartialDataset", "ds1.1.1");
            utils.MakePartial(d, "ds1.1.1");
            var c = GenerateLocalConfig(d);
            var l = LocalMachine.GetLocation(c);
            var r = l.GetDSInfo("ds1.1.1");
            Assert.AreEqual(1, l.GetDS(r, null, fslist => fslist.Take(1).ToArray()).Length);
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
            Assert.IsFalse(r.IsLocal(fslist => fslist.Where(f => f.Contains(".5")).Take(1).ToArray()));
        }

        [TestMethod]
        public void GetForMissingLocalFileInPartialDataset()
        {
            Assert.Inconclusive();
            // A copy of the Look version of this will trigger a copy, so write this
            // test when we implement the local downloads.
            
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
