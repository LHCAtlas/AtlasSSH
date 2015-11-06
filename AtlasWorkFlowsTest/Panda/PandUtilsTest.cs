using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Panda;

namespace AtlasWorkFlowsTest.Panda
{
    [TestClass]
    public class PandUtilsTest
    {
        [TestMethod]
        public void OnlineGetGoodDatasetName()
        {
            // Returns the job with an appropriate task name.
            var task = "user.emmat.mc15_13TeV.361022.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ2W.merge.AOD.e3668_s2576_s2132_r6765_r6282__EXOT15_v3/".FindPandaJobWithTaskName();
            Assert.IsNotNull(task);
        }

        [TestMethod]
        public void OnlineGetBadDatasetName()
        {
            // Returns the job with an appropriate task name.
            var task = "user.emmat.mc15_13TeV.361022.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ2W.merge.__EXOT15_v3/".FindPandaJobWithTaskName();
            Assert.IsNull(task);
        }
    }
}
