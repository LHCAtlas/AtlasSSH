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
    public class DatasetTest
    {
        [TestMethod]
        public void xAODMCDataset()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("mc15_13TeV.361020.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W.merge.AOD.e3569_s2576_s2132_r6765_r6282", "user.bogus");
            Assert.IsTrue(ds.StartsWith("user.bogus.361020.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W.s2132_r6765_r6282.DiVertAnalysis_v22_3B233454"), ds);
        }

        [TestMethod]
        public void DxAODDatasetFromEmma()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("user.emmat.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.merge.AOD.e3668_s2576_s2132_r6765_r6282__EXOT15_EXT0", "user.bogus");
            Assert.IsTrue(ds.StartsWith("user.bogus.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.r6282__EXOT15_EXT0.DiVertAnalysis_v22_3B233454"), ds);
        }

        [TestMethod]
        public void DxNoDash()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("user.emmat.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.merge.AOD.e3668_s2576_s2132_r6765_r6282__EXOT15_EXT0", "user.bogus");
            Assert.IsFalse(ds.Contains("-"), ds);
        }

        [TestMethod]
        public void DxAODDatasetSlightlyDifferent()
        {
            // Seen in the wild. Need to take into account the fact that there might be subtle differences in the dataset names.
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds1 = j.ResultingDatasetName("user.emmat.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.merge.AOD.e3668_s2576_s2132_r6765_r6282__EXOT15_EXT0", "user.bogus");
            var ds2 = j.ResultingDatasetName("user.emmat.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.merge.AOD.e3669_s2576_s2132_r6765_r6282__EXOT15_EXT0", "user.bogus");
            Assert.AreNotEqual(ds1, ds2);
        }

        [TestMethod]
        public void LatestreamDS()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV:data15_13TeV.00277025.physics_Late.merge.DAOD_EXOT15.f622_m1486_p2425_tid06570038_00", "user.bogus");
            Assert.IsTrue(ds.StartsWith("user.bogus.00277025.physics_Late.DAOD_EXOT15.f622_m1486_p2425.DiVertAnalysis_v22_3B233454"));
        }

        [TestMethod]
        public void LatestreamDSNoScope()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV.00277025.physics_Late.merge.DAOD_EXOT15.f622_m1486_p2425_tid06570038_00", "user.bogus");
            Assert.IsTrue(ds.StartsWith("user.bogus.00277025.physics_Late.DAOD_EXOT15.f622_m1486_p2425.DiVertAnalysis_v22_3B233454"));
        }

        [TestMethod]
        public void DataDerivationxAOD()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV:data15_13TeV.00280500.physics_Main.merge.DAOD_EXOT15.f631_m1504_p2425_tid06603342_00", "user.bogus");
            Assert.IsTrue(ds.StartsWith("user.bogus.00280500.physics_Main.DAOD_EXOT15.f631_m1504_p2425.DiVertAnalysis_v22_3B233454"));
        }

        [TestMethod]
        public void StableHash()
        {
            var j = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var h = j.Hash();
            Assert.AreEqual("3B233454", h);
        }

        [TestMethod]
        public void HashTheSame()
        {
            var j1 = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            var j2 = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            Assert.AreEqual(j1.Hash(), j2.Hash());
        }

        [TestMethod]
        public void HashNotSame()
        {
            var j1 = new AtlasJob() { Name = "DiVertAnalysiss", Version = 22 };
            var j2 = new AtlasJob() { Name = "DiVertAnalysis", Version = 22 };
            Assert.AreNotEqual(j1.Hash(), j2.Hash());
        }

        [TestMethod]
        public void HashSameForReorderedPackages()
        {
            var j1 = new AtlasJob() { Name = "DiVertAnalysis", Version = 22,
                Packages = new Package[]
                {
                    new Package() { Name = "pkg1", SCTag = "v00-00-00" },
                    new Package() { Name = "pkg2", SCTag = "v00-01-00" }
                },
            };
            var j2 = new AtlasJob()
            {
                Name = "DiVertAnalysis",
                Version = 22,
                Packages = new Package[]
                {
                    new Package() { Name = "pkg2", SCTag = "v00-01-00" },
                    new Package() { Name = "pkg1", SCTag = "v00-00-00" },
                },
            };
            Assert.AreEqual(j1.Hash(), j2.Hash());
        }
    }
}
