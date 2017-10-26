using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;

namespace AtlasSSHTest
{
    [TestClass]
    public class CircularStringBufferTest
    {
        [TestMethod]
        public void CBufferLonger()
        {
            var b = new CircularStringBuffer(10);
            b.Add("hi");
            Assert.AreEqual("hi", b.ToString());
        }

        [TestMethod]
        public void CBufferSame()
        {
            var b = new CircularStringBuffer(10);
            b.Add("0123456789");
            Assert.AreEqual("0123456789", b.ToString());
        }

        [TestMethod]
        public void CBufferShorter()
        {
            var b = new CircularStringBuffer(10);
            b.Add("0123456789");
            b.Add("0");
            Assert.AreEqual("1234567890", b.ToString());
        }

        [TestMethod]
        public void CBufferInsertLong()
        {
            var b = new CircularStringBuffer(10);
            b.Add("01234567890");
            Assert.AreEqual("1234567890", b.ToString());
        }

        [TestMethod]
        public void CBufferInsertVeryLong()
        {
            var b = new CircularStringBuffer(10);
            b.Add("012345678901234567890");
            Assert.AreEqual("1234567890", b.ToString());
        }
    }
}
