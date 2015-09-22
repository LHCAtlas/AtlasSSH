using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasWorkFlows.Utils;
using System.IO;

namespace AtlasWorkFlowsTest.Utils
{
    [TestClass]
    public class ConfigTest
    {
        [TestMethod]
        public void SingleComment()
        {
            var f = WriteConfigFile("SingleComment.txt", "#hi there");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(0, r.Count);
        }

        [TestMethod]
        public void SingleLine()
        {
            var f = WriteConfigFile("SingleLine.txt", "UW.Name=george");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george", r["UW"]["Name"]);
        }

        [TestMethod]
        public void NameWithUnderscore()
        {
            var f = WriteConfigFile("SingleLine.txt", "UW.name_is_good=george");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("name_is_good"));
            Assert.AreEqual("george", r["UW"]["name_is_good"]);
        }

        [TestMethod]
        public void SingleLineWithSpaceAfterEquals()
        {
            var f = WriteConfigFile("SingleLine.txt", "UW.Name= george");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george", r["UW"]["Name"]);
        }

        [TestMethod]
        public void SingleLineWithSpaceAroundDots()
        {
            var f = WriteConfigFile("SingleLine.txt", "UW.Name= george");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george", r["UW"]["Name"]);
        }

        [TestMethod]
        public void SingleLineWithSpaceBeforeEquals()
        {
            var f = WriteConfigFile("SingleLine.txt", "UW.Name =george");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george", r["UW"]["Name"]);
        }

        [TestMethod]
        public void SingleLineWithSpace1()
        {
            var f = WriteConfigFile("SingleLineWithSpace1.txt", "UW.Name=george is here");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george is here", r["UW"]["Name"]);
        }

        [TestMethod]
        public void SingleLineWithSpace2()
        {
            var f = WriteConfigFile("SingleLineWithSpace2.txt", "UW.Name= george is here");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george is here", r["UW"]["Name"]);
        }

        [TestMethod]
        public void SingleLineWithSpace3()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name= george is here ");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george is here", r["UW"]["Name"]);
        }

        [TestMethod]
        public void TwoLines()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name=george", "UW.NoWay=YES");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.IsTrue(r["UW"].ContainsKey("Name"));
            Assert.AreEqual("george", r["UW"]["Name"]);
            Assert.IsTrue(r["UW"].ContainsKey("NoWay"));
            Assert.AreEqual("YES", r["UW"]["NoWay"]);
        }

        [TestMethod]
        public void ThreeWithComment()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name=george", "# Wat is going on today?", "UW.NoWay = YES");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.AreEqual(2, r["UW"].Count);
        }

        [TestMethod]
        public void CommentsAndBlankLines()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "#", "# This is a sample file", "# ", "", "", "UW.Name=bogus #this is the first line", "", "UW.Focus =Yes");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.AreEqual(2, r["UW"].Count);
        }

        [TestMethod]
        public void StartWithComment()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "#dude what are you doing?", "UW.Name=george", "# Wat is going on today?", "UW.NoWay = YES");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.AreEqual(2, r["UW"].Count);
        }

        [TestMethod]
        public void EndWithComment()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name=george # and no way this worked", "UW.NoWay = YES");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.AreEqual(2, r["UW"].Count);
            Assert.AreEqual("george", r["UW"]["Name"]);
        }

        [TestMethod]
        public void ThreeLines()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name=george", "CERN.Day=50", "ND.Never=10");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(3, r.Count);
        }

        [TestMethod]
        public void BlankLines()
        {
            var f = WriteConfigFile("SingleLineWithSpace3.txt", "UW.Name=george", "", "UW.NoWay=YES", "");
            var r = Config.ParseConfigFile(f);
            Assert.AreEqual(1, r.Count);
            Assert.IsTrue(r.ContainsKey("UW"));
            Assert.AreEqual(2, r["UW"].Count);
        }

        /// <summary>
        /// Generate a config file
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private FileInfo WriteConfigFile(string fname, params string[] p)
        {
            var f = new FileInfo(fname);
            using (var wr = f.CreateText())
            {
                foreach (var l in p)
                {
                    wr.WriteLine(l);
                }
            }
            return f;
        }
    }
}
