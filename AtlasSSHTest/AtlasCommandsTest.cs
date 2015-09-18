using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;

namespace AtlasSSHTest
{
    [TestClass]
    public class AtlasCommandsTest
    {
        [TestMethod]
        public void setupATLAS()
        {
            // THis contains an internal check, so no need to do anything special here.
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s.setupATLAS();
        }

        [TestMethod]
        public void setupRucioWithATLASSetup()
        {
            // This contains an internal check, so we only need to watch for it to "work".
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void setupRucioWithoutATLASSetup()
        {
            // This contains an internal check, so we only need to watch for it to "work".
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupRucio(info.Item2);
        }

        [TestMethod]
        public void vomsProxyInit()
        {
            // Init a voms proxy correctly. There is an internal check, so this should
            // be fine.
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", info.Item2);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void vomsProxyWithBadUsername()
        {
            // Make sure that we fail when the username is bad
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", "freak-out");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void vomsProxyWithBadPassword()
        {
            // Make sure that we fail when the username is bad
            var info = util.GetUsernameAndPassword();
            util.SetPassword("GRID", "VOMSTestUser", "bogus-password");
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", "VOMSTestUser");
        }

        [TestMethod]
        public void downloadBadDS()
        {
            // Download an invalid dataset name to a remote directory (on Linux).
            Assert.Inconclusive();
        }

        [TestMethod]
        public void downloadDS()
        {
            // Download a good dataset to a remote directory.
            Assert.Inconclusive();
        }

        [TestMethod]
        public void downloadLocally()
        {
            // download to a local directory the dataset.
            Assert.Inconclusive();
        }
    }
}
