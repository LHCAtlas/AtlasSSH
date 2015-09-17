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
    }
}
