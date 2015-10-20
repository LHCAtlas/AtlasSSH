using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    [TestClass]
    public class LineBufferTest
    {
        const string CrLf = "\r\n";

        [TestMethod]
        public void TestEmptyLineWidth()
        {
            var l = new LineBuffer(null);
            Assert.IsFalse(l.Match("bogus"));
        }

        [TestMethod]
        public void SimpleMatch()
        {
            var l = new LineBuffer();
            l.Add("bash");
            Assert.IsTrue(l.Match("bash"));
        }

        [TestMethod]
        public void SimpleMatchWithExtra()
        {
            var l = new LineBuffer();
            l.Add("bogusbashdude");
            Assert.IsTrue(l.Match("bash"));
        }

        [TestMethod]
        public void MissMatchOnNewLine()
        {
            var l = new LineBuffer();
            l.Add("bash" + CrLf + "dude");
            Assert.IsFalse(l.Match("bash"));
        }

        [TestMethod]
        public void MatchOnCharByChar()
        {
            var l = new LineBuffer();
            l.Add("b");
            l.Add("a");
            Assert.IsFalse(l.Match("bash"));
            l.Add("s");
            l.Add("h");
            Assert.IsTrue(l.Match("bash"));
        }
    }
}
