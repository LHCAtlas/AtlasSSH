﻿using AtlasWorkFlows.Jobs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Sprache;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public void GoodPackage()
        {
            var s = "package(pk,1234)";
            var c = JobParser.ParsePackage.Parse(s);
            Assert.AreEqual("pk", c.Name);
            Assert.AreEqual("1234", c.SCTag);
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
        public void EmptyJobList()
        {
            Assert.Inconclusive();
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
    }
}