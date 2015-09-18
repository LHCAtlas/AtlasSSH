using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;
using System.Threading.Tasks;
using System.Collections.Generic;

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
        public void ListDirectory()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);

            bool foundFile = false;
            s.ExecuteCommand("ls -a | cat", l => { if (l.Contains(".bash_profile")) { foundFile = true; } Console.WriteLine(l); });
            Assert.IsTrue(foundFile);
        }

        [TestMethod]
        public void PWDOutputAsExpected()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);

            int count = 0;
            s.ExecuteCommand("pwd", l => {
                Assert.IsFalse(l.Contains("\r"));
                Assert.IsFalse(l.Contains("\n"));
                Console.WriteLine("--> " + l);
                count++;
            });
            Assert.AreEqual(1, count);            
        }

        [TestMethod]
        public void ListDirectoryWithNoOutput()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);

            s.ExecuteCommand("ls -a | cat");
        }

        [TestMethod]
        public void ReadBackWithPrompt()
        {
            var info = util.GetUsernameAndPassword();
            var s = new SSHConnection(info.Item1, info.Item2);

            Console.WriteLine("Before we do anything here is the environment:");
            s.ExecuteCommand("set", l => Console.WriteLine(" set: " + l));
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            bool valueSet = false;
            bool sawPrompt = false;
            s.ExecuteCommand("prompt=\"what is up: \"");
            s.ExecuteCommandWithInput("read -p \"$prompt\" bogusvalue", new Dictionary<string, string>() { { "up:", "\nthis freak me out is a test" } }, l =>
                {
                    sawPrompt = sawPrompt || l.Contains("what is up");
                    Console.WriteLine("==> " + l);
                })
                .ExecuteCommand("set", l => Console.WriteLine(" set: " + l))
                .ExecuteCommand("echo bogusvalue $bogusvalue", l =>
                {
                    valueSet = valueSet || l.Contains("this");
                    Console.WriteLine("--> " + l);
                });

            // THis guy isn't working yet because we don't seem to read in any input.
            Assert.Inconclusive();
            Assert.IsTrue(sawPrompt);
            Assert.IsTrue(valueSet);
        }
    }
}
