using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;
using System.Collections.Generic;

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
        [ExpectedException(typeof(ArgumentException))]
        public void downloadBadDS()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", info.Item2)
                .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.11111", "/tmp/usergwattstempdata");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void downloadToBadDir()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", info.Item2)
                .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1", "/furitcake/usergwattstempdata");
        }

        [TestMethod]
        public void downloadDS()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupATLAS()
                .setupRucio(info.Item2)
                .VomsProxyInit("atlas", info.Item2)
                .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1", "/tmp/usergwattstempdata");

            // Now, check!
            var files = new List<string>();
            s.ExecuteCommand("ls /tmp/usergwattstempdata/user.gwatts.301295.EVNT.1 | cat", l => files.Add(l));

            foreach (var fname in files)
            {
                Console.WriteLine("-> " + fname);
            }

            Assert.AreEqual(10, files.Count);
        }

        [TestMethod]
        public void copyRemoteDirectoryLocal()
        {
            // download to a local directory the dataset.
            Assert.Inconclusive();
        }
    }
}
