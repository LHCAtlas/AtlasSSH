using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Sprache;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AtlasWorkFlowsTest.Jobs
{
    [TestClass]
    public class JobParserTest
    {
        [TestMethod]
        public void GoodReleaseText()
        {
            var s = "release(Base, 2.3.30)";
            var r = JobParser.ParseRelease.Parse(s);
            Assert.AreEqual("Base, 2.3.30", r.Name);
        }

        [TestMethod]
        public void GoodReleaseTextWithWS()
        {
            var s = "release ( Base, 2.3.30 ) ";
            var r = JobParser.ParseRelease.Parse(s);
            Assert.AreEqual("Base, 2.3.30", r.Name);
        }

        [TestMethod]
        public void GoodCommand()
        {
            var s = "command(ls dudemyfood)";
            var c = JobParser.ParseCommand.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.CommandLine);
        }

        [TestMethod]
        public void GoodCommandWithWS()
        {
            var s = "command ( ls dudemyfood ) ";
            var c = JobParser.ParseCommand.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.CommandLine);
        }

        [TestMethod]
        public void GoodSubmit()
        {
            var s = "submit(ls dudemyfood)";
            var c = JobParser.ParseSubmit.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodSubmitWithWS()
        {
            var s = "submit ( ls dudemyfood ) ";
            var c = JobParser.ParseSubmit.Parse(s);
            Assert.AreEqual("ls dudemyfood", c.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodPatternSubmit()
        {
            var s = "submit_pattern(xAOD, ls dummymyfood)";
            var c = JobParser.ParseSubmitPattern.Parse(s);
            Assert.AreEqual("xAOD", c.Regex);
            Assert.AreEqual("ls dummymyfood", c.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodPatternSubmitQuoted()
        {
            var s = "submit_pattern(xAOD, \"ls dummymyfood\")";
            var c = JobParser.ParseSubmitPattern.Parse(s);
            Assert.AreEqual("xAOD", c.Regex);
            Assert.AreEqual("ls dummymyfood", c.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodPackage()
        {
            var s = "package(pk,1234)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("1234", c.SCTag);
        }

        [TestMethod]
        public void GoodPackageByRelease()
        {
            var s = "package(pk)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("", c.SCTag);
        }

        [TestMethod]
        public void GoodGitPackageLongForm()
        {
            var s = "package(https://:@gitlab.cern.ch:8443/atlas-phys-exotics-llp-mscrid/DiVertAnalysisCode.git)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("https://:@gitlab.cern.ch:8443/atlas-phys-exotics-llp-mscrid/DiVertAnalysisCode.git", c.Name);
            Assert.AreEqual("", c.SCTag);
        }

        [TestMethod]
        public void GoodGitPackageShortForm()
        {
            var s = "package(atlas-phys-exotics-llp-mscrid/DiVertAnalysisCode.git)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("atlas-phys-exotics-llp-mscrid/DiVertAnalysisCode.git", c.Name);
            Assert.AreEqual("", c.SCTag);
        }

        [TestMethod]
        public void SubmissionMachine()
        {
            var s = "submission_machine(tev01.phys.washington.edu, gwatts)";
            var c = JobParser.ParseSubmissionMachine.Parse(s);
            Assert.AreEqual("tev01.phys.washington.edu", c.MachineName);
            Assert.AreEqual("gwatts", c.UserName);
        }

        [TestMethod]
        public void GoodPackageWithExtraCharaters()
        {
            var s = "package(atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis, 247827)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("247827", c.SCTag);
        }

        [TestMethod]
        public void GoodPackageWithWS()
        {
            var s = "package ( pk , 1234 ) ";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("1234", c.SCTag);
        }

        [TestMethod]
        public void EmptyJob()
        {
            var j = JobParser.ParseJob.Parse("job(DiVert,22){}");
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(0, j.Packages.Length);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.AreEqual("", j.SubmitCommand.SubmitCommand.CommandLine);

        }

        [TestMethod]
        public void GoodJob()
        {
            var s = "job(DiVert,22){release(Base,1234)package(DiVertAnalysis,1234)submit(ls)}";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.AreEqual("ls", j.SubmitCommand.SubmitCommand.CommandLine);
            Assert.IsNotNull(j.SubmitPatternCommands);
            Assert.AreEqual(0, j.SubmitPatternCommands.Length);
        }

        [TestMethod]
        public void GoodJobWithPattern()
        {
            var s = "job(DiVert,22){release(Base,1234)package(DiVertAnalysis,1234)submit_pattern(joke, ls)}";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.AreEqual(0, j.SubmitCommand.SubmitCommand.CommandLine.Length);
            Assert.IsNotNull(j.SubmitPatternCommands);
            Assert.AreEqual(1, j.SubmitPatternCommands.Length);
            Assert.AreEqual("ls", j.SubmitPatternCommands[0].SubmitCommand.SubmitCommand.CommandLine);
            Assert.AreEqual("joke", j.SubmitPatternCommands[0].Regex);
        }

        [TestMethod]
        public void GoodJobWith2Patterns()
        {
            var s = "job(DiVert,22){release(Base,1234)package(DiVertAnalysis,1234)submit_pattern(joke, ls)submit_pattern(myfoot, dude)}";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.IsNotNull(j.SubmitPatternCommands);
            Assert.AreEqual(2, j.SubmitPatternCommands.Length);
            Assert.AreEqual("ls", j.SubmitPatternCommands[0].SubmitCommand.SubmitCommand.CommandLine);
            Assert.AreEqual("joke", j.SubmitPatternCommands[0].Regex);
            Assert.AreEqual("dude", j.SubmitPatternCommands[1].SubmitCommand.SubmitCommand.CommandLine);
            Assert.AreEqual("myfoot", j.SubmitPatternCommands[1].Regex);
        }

        [TestMethod]
        public void GoodJobWithSpaces()
        {
            var s = "job(DiVert,22) { release(Base,1234) package(DiVertAnalysis,1234) submit(ls) }";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.AreEqual("ls", j.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        [ExpectedException(typeof(Sprache.ParseException))]
        public void BadParseWithError()
        {
            var s = "job(DiVert,22) { job release(Base,1234) package(DiVertAnalysis,1234) submit(ls) }";
            var j = JobParser.ParseJobFile.Parse(s);
        }

        [TestMethod]
        [ExpectedException(typeof(Sprache.ParseException))]
        public void BadJobWithSubmitAndPatternSubmits()
        {
            var s = "job(DiVert,22){release(Base,1234)package(DiVertAnalysis,1234)submit(ls)submit_pattern(myfoot, dude)}";
            var j = JobParser.ParseJob.Parse(s);
        }

        [TestMethod]
        [ExpectedException(typeof(Sprache.ParseException))]
        public void BadParseWithError2()
        {
            var s = "job(DiVert,22) { release(Base,1234) package(DiVertAnalysis,1234) submit(ls) } job ";
            var j = JobParser.ParseJobFile.Parse(s);
        }

        [TestMethod]
        [ExpectedException(typeof(JobParseException))]
        public void BadParseFromFile()
        {
            var f = new FileInfo("BadParseFromFile.txt");
            if (f.Exists)
            {
                f.Delete();
            }
            using (var writer = f.CreateText())
            {
                writer.WriteLine("job(DiVert,22) { job release(Base,1234) package(DiVertAnalysis,1234) submit(ls) }");
            }

            f.Refresh();
            var r = f.ParseJobsInFile();
        }

        [TestMethod]
        public void GoodJobOnLines()
        {
            var s = @"job(DiVert,22) 
    { 
        release(Base,1234)
        package(DiVertAnalysis,1234)
        submit(ls)
    }";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.AreEqual("ls", j.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void GoodJobWithCommentsOnLines()
        {
            var s = @"job(DiVert,22) 
    { 
// This is a test
# so is this.
        release(Base,1234)
        package(DiVertAnalysis,1234)
        submit(ls)
# Dude!
    }";
            var j = JobParser.ParseJob.Parse(s);
            Assert.IsNotNull(j.Commands);
            Assert.AreEqual(0, j.Commands.Length);
            Assert.AreEqual("DiVert", j.Name);
            Assert.AreEqual(22, j.Version);
            Assert.IsNotNull(j.Packages);
            Assert.AreEqual(1, j.Packages.Length);
            Assert.AreEqual("DiVertAnalysis", j.Packages[0].Name);
            Assert.IsNotNull(j.Release);
            Assert.AreEqual("Base,1234", j.Release.Name);
            Assert.IsNotNull(j.SubmitCommand);
            Assert.AreEqual("ls", j.SubmitCommand.SubmitCommand.CommandLine);
        }

        [TestMethod]
        public void FileWithMoreThanOne()
        {
            var s = @"job(DiVert,22) 
    { 
        release(Base,1234)
        package(DiVertAnalysis,1234)
        submit(ls)
    }
    submission_machine(tev01.phys.washington.edu,gwatts)
    job(Dork,1) {
      release(Base,123211)
    }";
            var j = JobParser.ParseJobFile.Parse(s);

            Assert.AreEqual(2, j.Jobs.Length);
            Assert.AreEqual(1, j.machines.Length);
        }

        [TestMethod]
        public void FileWithComments()
        {
            var s = @"job(DiVert,22) 
    { 
        release(Base,1234)
        package(DiVertAnalysis,1234)
        submit(ls)
    }
# this is the basic submission stuff now
    submission_machine(tev01.phys.washington.edu,gwatts)
    job(Dork,1) {
      release(Base,123211)
    }";
            var j = JobParser.ParseJobFile.Parse(s);

            Assert.AreEqual(2, j.Jobs.Length);
            Assert.AreEqual(1, j.machines.Length);
        }
    }
}
