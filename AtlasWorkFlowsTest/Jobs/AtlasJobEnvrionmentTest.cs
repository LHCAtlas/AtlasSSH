using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static AtlasWorkFlows.Jobs.AtlasJobEnvrionmentUtils;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class AtlasJobEnvrionmentTest
    {
        [TestMethod]
        public void GetCleanEnvironment()
        {
            var e = CleanAtlasJobEnvrionment;
            Assert.IsNotNull(e);
            Assert.IsNull(e.Job);
        }

        [TestMethod]
        public void BuildJobFunctionality()
        {
            var e = CleanAtlasJobEnvrionment
                .NameVersionRelease("DiVertAnalysis", 3, "2.3.32")
                .Package("JetSelectorTools")
                .Package("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", "248132")
                .Command("grep -v \"emf < 0.05\" JetSelectorTools/Root/JetCleaningTool.cxx > JetSelectorTools/Root/JetCleaningTool-New.cxx")
                .Command("mv JetSelectorTools/Root/JetCleaningTool-New.cxx JetSelectorTools/Root/JetCleaningTool.cxx")
                .SubmitCommand("DiVertAnalysisRunner -EventLoopDriver GRID *INPUTDS* -ELGRIDOutputSampleName *OUTPUTDS* -WaitTillDone FALSE -isLLPMC true");

            var job = e.Job;

            Assert.AreEqual("DiVertAnalysis", job.Name);
            Assert.AreEqual(3, job.Version);
            Assert.AreEqual("2.3.32", job.Release.Name);

            Assert.AreEqual(2, job.Packages.Length);
            Assert.AreEqual("JetSelectorTools", job.Packages[0].Name);
            Assert.AreEqual("", job.Packages[0].SCTag);
            Assert.AreEqual("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", job.Packages[1].Name);
            Assert.AreEqual("248132", job.Packages[1].SCTag);

            Assert.AreEqual(2, job.Commands.Length);
            Assert.AreEqual("grep -v \"emf < 0.05\" JetSelectorTools/Root/JetCleaningTool.cxx > JetSelectorTools/Root/JetCleaningTool-New.cxx", job.Commands[0].CommandLine);
            Assert.AreEqual("mv JetSelectorTools/Root/JetCleaningTool-New.cxx JetSelectorTools/Root/JetCleaningTool.cxx", job.Commands[1].CommandLine);

            Assert.AreEqual("DiVertAnalysisRunner -EventLoopDriver GRID *INPUTDS* -ELGRIDOutputSampleName *OUTPUTDS* -WaitTillDone FALSE -isLLPMC true", job.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void CopyNotModify()
        {
            var eBase = CleanAtlasJobEnvrionment
                .NameVersionRelease("DiVertAnalysis", 3, "2.3.32")
                .Package("JetSelectorTools");

            var e1 = eBase
                .Package("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", "248132");

            Assert.AreEqual(1, eBase.Job.Packages.Length);
            Assert.AreEqual(2, e1.Job.Packages.Length);
        }

        [TestMethod]
        public void CheckCloneWithSubmit()
        {
            var e = CleanAtlasJobEnvrionment
                .NameVersionRelease("DiVertAnalysis", 3, "2.3.32")
                .Package("JetSelectorTools")
                .Package("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", "248132")
                .SubmitCommand("DiVertAnalysisRunner -EventLoopDriver GRID *INPUTDS* -ELGRIDOutputSampleName *OUTPUTDS* -WaitTillDone FALSE -isLLPMC true")
                .Command("grep -v \"emf < 0.05\" JetSelectorTools/Root/JetCleaningTool.cxx > JetSelectorTools/Root/JetCleaningTool-New.cxx")
                .Command("mv JetSelectorTools/Root/JetCleaningTool-New.cxx JetSelectorTools/Root/JetCleaningTool.cxx");
        }

        [TestMethod]
        public void CheckCloneWithSubmitPattern()
        {
            var e = CleanAtlasJobEnvrionment
                .NameVersionRelease("DiVertAnalysis", 3, "2.3.32")
                .Package("JetSelectorTools")
                .Package("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", "248132")
                .SubmitPatternCommand("DiVertAnalysisRunner -EventLoopDriver GRID *INPUTDS* -ELGRIDOutputSampleName *OUTPUTDS* -WaitTillDone FALSE -isLLPMC true", "pattern")
                .Command("grep -v \"emf < 0.05\" JetSelectorTools/Root/JetCleaningTool.cxx > JetSelectorTools/Root/JetCleaningTool-New.cxx")
                .Command("mv JetSelectorTools/Root/JetCleaningTool-New.cxx JetSelectorTools/Root/JetCleaningTool.cxx");
        }
    }


}
