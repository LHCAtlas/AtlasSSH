using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Panda;
using System.IO;
using System;
using System.Linq;

namespace AtlasWorkFlowsTest.Panda
{
    [TestClass]
    public class PandUtilsTest
    {
        [TestInitialize]
        public void TestInit()
        {
            PandaUtils.ResetCache("PandaCacheForTesting");
            var d = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PandaCacheForTesting"));
            if (d.Exists)
                d.Delete(true);
        }

        static string testingDSName = "user.gwatts.305588.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH125_mS5_lt5m.DiVertAnalysis_v10_35712B0E_E07C7B15/";

        [TestMethod]
        public void CacheHitAndMiss()
        {
            // Make sure the cache is empty.
            var cacheDir = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PandaCacheForTesting"));
            Assert.IsTrue(!cacheDir.Exists || cacheDir.EnumerateFiles().Count() == 0);

            // Do a test.
            var task = testingDSName.FindPandaJobWithTaskName();
            cacheDir.Refresh();
            Assert.IsTrue(cacheDir.Exists);
            Assert.AreEqual(1, cacheDir.EnumerateFiles().Count());

            // Dump for testing purposes the contents of this guy
            var f = cacheDir.EnumerateFiles().First();
            Console.WriteLine(f.FullName);
            Console.WriteLine(f.ReadLine(10));

            var task1 = testingDSName.FindPandaJobWithTaskName();
        }

        [TestMethod]
        public void OnlineGetGoodDatasetName()
        {
            // Returns the job with an appropriate task name.
            var task = testingDSName.FindPandaJobWithTaskName();
            Assert.IsNotNull(task);
        }

        [TestMethod]
        public void OnlineGetGoodDatasetNameAndDSInfo()
        {
            // Returns the job with an appropriate task name.
            var task = testingDSName.FindPandaJobWithTaskName(true);
            Assert.IsNotNull(task.datasets);
            Assert.AreNotEqual(0, task.datasets.Count);
        }

        [TestMethod]
        public void OnlineGetBadDatasetName()
        {
            // Returns the job with an appropriate task name.
            var task = "user.emmat.mc15_13TeV.361022.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ2W.merge.__EXOT15_v3/".FindPandaJobWithTaskName();
            Assert.IsNull(task);
        }

        static int tastTestingID = 10192902;

        [TestMethod]
        public void OnlineGoodTaskID()
        {
            var task = tastTestingID.FindPandaJobWithTaskName();
            Assert.IsNotNull(task);
            Assert.AreEqual("done", task.status);
        }

        [TestMethod]
        public void OnlineGoodTaskIDWithDS()
        {
            var task = tastTestingID.FindPandaJobWithTaskName(true);
            Assert.IsNotNull(task.datasets);
            Assert.AreNotEqual(0, task.datasets.Count);
        }

        [TestMethod]
        public void OnlineBadTaskID()
        {
            var task = 692325724.FindPandaJobWithTaskName();
            Assert.IsNull(task);
        }
    }
}
