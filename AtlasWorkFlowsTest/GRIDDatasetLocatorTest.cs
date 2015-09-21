using AtlasWorkFlows;
using AtlasWorkFlows.Locations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasWorkFlowsTest
{
    [TestClass]
    public class GRIDDatasetLocatorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AskForWSDataset()
        {
            GRIDDatasetLocator.FetchDatasetUris(" ");
        }

        [TestCleanup]
        public void CleanupConfig()
        {
            // Reset where we get the locations from!
            Locator._getLocations = null;
        }

        [TestMethod]
        public void DatasetAlreadyAtCERN()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectoryBeforeBuild("DatasetAlreadyAtCERN", dsname);
            Locator._getLocations = () => utils.GetLocal(d);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void LocationsAllMaskedOut()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectoryBeforeBuild("LocationsAllMaskedOut", dsname);
            Locator._getLocations = () => utils.GetLocal(d);

            // No locations allowed, which should cause this to bomb.
            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, locationFilter: locName => false);
        }

        [TestMethod]
        public void DatasetAlreadyAtCERNWithLocal()
        {
            // Make sure having an empty local dataset doesn't confuse where we pick stuff up!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectoryBeforeBuild("DatasetAlreadyAtCERNWithLocal", dsname);
            var localD = new DirectoryInfo("./DatasetAlreadyAtCERNWithLocalLocal");
            if (!localD.Exists)
                localD.Create();

            Locator._getLocations = () => utils.GetLocal(d, localD);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
        }

        [TestMethod]
        public void DatasetAlreadyAtCERNLimited()
        {
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d = utils.BuildSampleDirectoryBeforeBuild("DatasetAlreadyAtCERN", dsname);
            Locator._getLocations = () => utils.GetLocal(d);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, fileFilter: fs => fs.OrderBy(f => f).Take(1).ToArray());

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
        }

        [TestMethod]
        public void ChooseLocalIfPossible()
        {
            // Local dataset is complete, and should be chosen over remote!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("ChooseLocalIfPossibleRemote", dsname);
            var d2 = utils.BuildSampleDirectoryBeforeBuild("ChooseLocalIfPossibleLocal", dsname);
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, fileFilter: fs => fs.OrderBy(f => f).Take(1).ToArray());

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
            Console.WriteLine(r[0].LocalPath);
            Assert.IsTrue(r[0].LocalPath.Contains("ChooseLocalIfPossibleLocal"));
        }

        [TestMethod]
        public void ChooseLocalIfNothingRemote()
        {
            // Local dataset is complete, and should be chosen over remote!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = new DirectoryInfo("ChooseLocalIfNothingRemoteRemote");
            if (d1.Exists)
                d1.Delete(true);
            d1.Create();
            var d2 = utils.BuildSampleDirectoryBeforeBuild("ChooseLocalIfNothingRemoteLocal", dsname);
            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, fileFilter: fs => fs.OrderBy(f => f).Take(1).ToArray());

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
            Console.WriteLine(r[0].LocalPath);
            Assert.IsTrue(r[0].LocalPath.Contains("ChooseLocalIfNothingRemoteLocal"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void BombWhenLocalEmptyAndNoRemote()
        {
            // Local dataset is complete, and should be chosen over remote!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d2 = utils.BuildSampleDirectoryBeforeBuild("BombWhenLocalEmptyAndNoRemote", dsname);
            Locator._getLocations = () => utils.GetLocal(null, d2);

            var r = GRIDDatasetLocator.FetchDatasetUris("bogusnewdataset", fileFilter: fs => fs.OrderBy(f => f).Take(1).ToArray());
        }

        [TestMethod]
        public void ChooseRemoteWhenLocalNotComplete()
        {
            // Local dataset is complete, and should be chosen over remote!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("ChooseRemoteWhenLocalNotCompleteRemote", dsname);
            var d2 = utils.BuildSampleDirectoryBeforeBuild("ChooseRemoteWhenLocalNotCompleteLocal", dsname);
            utils.MakePartial(d2, "ds1.1.1");
            var f = new FileInfo(Path.Combine(d2.FullName, "ds1.1.1", "sub2", "file.root.5"));
            Assert.IsTrue(f.Exists);
            f.Delete();

            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname);

            Assert.IsNotNull(r);
            Assert.AreEqual(5, r.Length);
            Console.WriteLine(r[0].LocalPath);
            Assert.IsTrue(r[0].LocalPath.Contains("ChooseRemoteWhenLocalNotCompleteRemote"));
        }

        [TestMethod]
        public void ChooseLocalEvenIfPartial()
        {
            // Local dataset is complete, and should be chosen over remote!
            AtlasWorkFlows.Utils.IPLocationTests.SetIpName("pc.cern.ch");
            var dsname = "ds1.1.1";
            var d1 = utils.BuildSampleDirectoryBeforeBuild("ChooseLocalEvenIfPartialRemote", dsname);
            var d2 = utils.BuildSampleDirectoryBeforeBuild("ChooseLocalEvenIfPartialLocal", dsname);
            utils.MakePartial(d2, "ds1.1.1");
            var f = new FileInfo(Path.Combine(d2.FullName, "ds1.1.1", "sub2", "file.root.5"));
            Assert.IsTrue(f.Exists);
            f.Delete();

            Locator._getLocations = () => utils.GetLocal(d1, d2);

            var r = GRIDDatasetLocator.FetchDatasetUris(dsname, fileFilter: fs => fs.Where(fname => fname.Contains(".root.1")).ToArray());

            Assert.IsNotNull(r);
            Assert.AreEqual(1, r.Length);
            Console.WriteLine(r[0].LocalPath);
            Assert.IsTrue(r[0].LocalPath.Contains("ChooseLocalEvenIfPartialLocal"));
        }
    }
}
