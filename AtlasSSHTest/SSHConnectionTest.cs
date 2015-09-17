using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    [TestClass]
    public class SSHConnectionTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UnknownUsername()
        {
            var s = new SSHConnection("junk.washington.edu", "bogus-man");
        }

        /// <summary>
        /// This and just about everything else will throw an exception if you
        /// have not defined the text file with username and password and also
        /// entered them into the credential store.
        /// </summary>
        [TestMethod]
        public void KnownUserAndCTor()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);
        }

        [TestMethod]
        public async Task ListDirectory()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);

            bool foundFile = false;
            await s.ExecuteCommand("ls -a | cat", l => { if (l.Contains(".bash_profile")) { foundFile = true; } Console.WriteLine(l); });
            Assert.IsTrue(foundFile);
        }
    }
}
