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
                .setupRucio(info.Item1);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void setupRucioWithoutATLASSetup()
        {
            // This contains an internal check, so we only need to watch for it to "work".
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
            s
                .setupRucio(info.Item1);
        }
    }
}
