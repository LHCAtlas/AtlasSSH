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
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("mc15_13TeV.361020.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W.merge.AOD.e3569_s2576_s2132_r6765_r6282", "user.bogus");
            Assert.AreEqual("user.bogus.361020.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W.s2576_s2132_r6765_r6282.DiVertAnalysis_v22_1044055743", ds);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void xAODMCDatasetTooLong()
        {
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("mc15_13TeV.361020.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W_Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W_Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ0W.merge.AOD.e3569_s2576_s2132_r6765_r6282", "user.bogus");
        }

        [TestMethod]
        public void DxAODDatasetFromEmma()
        {
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("user.emmat.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.merge.AOD.e3668_s2576_s2132_r6765_r6282__EXOT15_EXT0", "user.bogus");
            Assert.AreEqual("user.bogus.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.r6765_r6282__EXOT15_EXT0.DiVertAnalysis_v22_1044055743", ds);
        }

        [TestMethod]
        public void LatestreamDS()
        {
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV:data15_13TeV.00277025.physics_Late.merge.DAOD_EXOT15.f622_m1486_p2425_tid06570038_00", "user.bogus");
            Assert.AreEqual("user.bogus.00277025.physics_Late.DAOD_EXOT15.f622_m1486_p2425.DiVertAnalysis_v22_1044055743", ds);
        }

        [TestMethod]
        public void LatestreamDSNoScope()
        {
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV.00277025.physics_Late.merge.DAOD_EXOT15.f622_m1486_p2425_tid06570038_00", "user.bogus");
            Assert.AreEqual("user.bogus.00277025.physics_Late.DAOD_EXOT15.f622_m1486_p2425.DiVertAnalysis_v22_1044055743", ds);
        }

        [TestMethod]
        public void DataDerivationxAOD()
        {
            var j = new Job() { Name = "DiVertAnalysis", Version = 22 };
            var ds = j.ResultingDatasetName("data15_13TeV:data15_13TeV.00280500.physics_Main.merge.DAOD_EXOT15.f631_m1504_p2425_tid06603342_00", "user.bogus");
            Assert.AreEqual("user.bogus.00280500.physics_Main.DAOD_EXOT15.f631_m1504_p2425.DiVertAnalysis_v22_1044055743", ds);
        }
    }
}
