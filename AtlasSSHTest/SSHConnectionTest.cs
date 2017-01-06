using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet.Common;

namespace AtlasSSHTest
{
    [TestClass]
    public class SSHConnectionTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void UnknownUsername()
        {
            using (var s = new SSHConnection("junk.washington.edu", "bogus-man"))
            { }
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
            using (var s = new SSHConnection(info.Item1, info.Item2)) { }
        }

        [TestMethod]
        public void ListDirectory()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                bool foundFile = false;
                s.ExecuteCommand("ls -a | cat", l => { if (l.Contains(".bash_profile")) { foundFile = true; } Console.WriteLine(l); Assert.IsFalse(string.IsNullOrWhiteSpace(l)); });
                Console.WriteLine("End of listing");
                Assert.IsTrue(foundFile);
            }
        }

        [TestMethod]
        public void PWDOutputAsExpected()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                int count = 0;
                s.ExecuteCommand("pwd", l =>
                {
                    Assert.IsFalse(l.Contains("\r"));
                    Assert.IsFalse(l.Contains("\n"));
                    Console.WriteLine("--> " + l);
                    count++;
                });
                Assert.AreEqual(1, count);
            }
        }

        [TestMethod]
        public void ListDirectoryWithNoOutput()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s.ExecuteCommand("ls -a | cat");
            }
        }

        [TestMethod]
        public void ReadBackWithPrompt()
        {
            var info = util.GetUsernameAndPassword();

            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                Console.WriteLine("Before we do anything here is the environment:");
                s.ExecuteCommand("set", l => Console.WriteLine(" set: " + l));
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();

                bool valueSet = false;
                bool sawPrompt = false;
                s.ExecuteCommand("prompt=\"what is up: \"");
                s.ExecuteCommand("read -p \"$prompt\" bogusvalue", l =>
                    {
                        sawPrompt = sawPrompt || l.Contains("what is up");
                        Console.WriteLine("==> " + l);
                    }, seeAndRespond: new Dictionary<string, string>() { { "up:", "this freak me out is a test" } })
                    .ExecuteCommand("set", l => Console.WriteLine(" set: " + l))
                    .ExecuteCommand("echo bogusvalue $bogusvalue", l =>
                    {
                        valueSet = valueSet || l.Contains("this");
                        Console.WriteLine("--> " + l);
                    });

                // THis guy isn't working yet because we don't seem to read in any input.
                Assert.IsTrue(sawPrompt);
                Assert.IsTrue(valueSet);
            }
        }

        [TestMethod]
        public void copyRemoteDirectoryLocal()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                // Create a "complex" directory structure on the remote machine
                s
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data")
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data/d1")
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data/d2")
                    .ExecuteCommand("echo hi1 > /tmp/usergwatts/data/f1")
                    .ExecuteCommand("echo hi2 > /tmp/usergwatts/data/d1/f2")
                    .ExecuteCommand("echo hi3 > /tmp/usergwatts/data/d2/f3")
                    .ExecuteCommand("echo hi4 > /tmp/usergwatts/data/d2/f4");

                // Remove everything local
                var d = new DirectoryInfo("./data");
                if (d.Exists)
                {
                    d.Delete(true);
                    d.Refresh();
                }
                d.Create();

                // Do the copy
                s.CopyRemoteDirectoryLocally("/tmp/usergwatts/data", d);

                Assert.IsTrue(File.Exists(Path.Combine(d.FullName, "f1")));
                Assert.IsTrue(File.Exists(Path.Combine(d.FullName, "d1", "f2")));
                Assert.IsTrue(File.Exists(Path.Combine(d.FullName, "d2", "f3")));
                Assert.IsTrue(File.Exists(Path.Combine(d.FullName, "d2", "f4")));
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ScpException))]
        public void copyBadRemoteDirectoryLocal()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                // Do the copy
                var d = new DirectoryInfo("./databogus");
                if (d.Exists)
                {
                    d.Delete(true);
                    d.Refresh();
                }
                d.Create();
                s.CopyRemoteDirectoryLocally("/tmp/usergwatts/databogusbogusbogusbogus", d);
            }
        }

        [TestMethod]
        public void CopyTwice()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                // Create a "complex" directory structure on the remote machine
                s
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data")
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data/d1")
                    .ExecuteCommand("mkdir -p /tmp/usergwatts/data/d2")
                    .ExecuteCommand("echo hi1 > /tmp/usergwatts/data/f1")
                    .ExecuteCommand("echo hi2 > /tmp/usergwatts/data/d1/f2")
                    .ExecuteCommand("echo hi3 > /tmp/usergwatts/data/d2/f3")
                    .ExecuteCommand("echo hi4 > /tmp/usergwatts/data/d2/f4");

                // Remove everything local
                var d = new DirectoryInfo("./data");
                if (d.Exists)
                {
                    d.Delete(true);
                    d.Refresh();
                }
                d.Create();

                // Do the copy
                s.CopyRemoteDirectoryLocally("/tmp/usergwatts/data", d);
                s.CopyRemoteDirectoryLocally("/tmp/usergwatts/data", d);
            }
        }
    }
}
