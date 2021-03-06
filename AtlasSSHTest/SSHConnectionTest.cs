﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AtlasSSH;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet.Common;
using static AtlasSSH.SSHConnection;

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
        public void KnownGenericNodeTest()
        {
            // Use a set of credentials that use something like "username" and "machine001.domain.edu" but have "machine.domain.edu" as the credential.
            // This makes sure that the fallback password finder works properly.
            var info = util.GetUsernameAndPassword(credentialKey: "AtlasSSHTestGenericMachine");
            using (var s = new SSHConnection(info.Item1, info.Item2)) {
                s.ExecuteCommand("ls -a", l => Console.WriteLine(l));
            }
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
            var l = new List<string>();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s.ExecuteCommand("ls -a | cat", output: ln => l.Add(ln));
            }
            // Make sure we got a few things back.
            Assert.AreNotEqual(0, l.Count);
        }

        [TestMethod]
        public void TimeoutBetweenEchos()
        {
            var info = util.GetUsernameAndPassword();
            var l = new List<DateTime>();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s.ExecuteCommand("echo hi; sleep 2; echo there", output: ln => l.Add(DateTime.Now));
            }
            Assert.AreEqual(2, l.Count);
            var diff = l[1] - l[0];
            Assert.IsTrue(diff > TimeSpan.FromSeconds(1), $"The time diff was {diff.ToString()} instead of at leaset 1 second");
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
        public void DoubleSSH()
        {
            var infoOutter = util.GetUsernameAndPassword();
            var infoInner = util.GetUsernameAndPassword("CERNSSHTest");
            string hostInner = null;
            string hostOutter = null;
            using (var s = new SSHConnection(infoOutter.Item1, infoOutter.Item2))
            {
                using (var subShell = s.SSHTo(infoInner.Item1, infoInner.Item2))
                {
                    s.ExecuteCommand("echo $HOSTNAME", output: l => hostInner = l);
                }
                s.ExecuteCommand("echo $HOSTNAME", output: l => hostOutter = l);
            }

            Console.WriteLine($"Inner host: {hostInner}");
            Console.WriteLine($"Outter host: {hostOutter}");

            Assert.AreNotEqual(hostInner, hostOutter, "Expect the host names of the two machines to be different");
            Assert.IsFalse(string.IsNullOrWhiteSpace(hostInner));
            Assert.IsFalse(string.IsNullOrWhiteSpace(hostOutter));
        }

        [TestMethod]
        [ExpectedException(typeof(UnableToCreateSSHTunnelException))]
        public void DoubleSSHToBadAddress()
        {
            try
            {
                var infoOutter = util.GetUsernameAndPassword("CERNSSHTest");
                var infoInner = Tuple.Create("bogus-pc.holidays.edu", "myusername");
                string hostInner = null;
                string hostOutter = null;
                using (var s = new SSHConnection(infoOutter.Item1, infoOutter.Item2))
                {
                    using (var subShell = s.SSHTo(infoInner.Item1, infoInner.Item2))
                    {
                        s.ExecuteCommand("echo $HOSTNAME", output: l => hostInner = l);
                    }
                    s.ExecuteCommand("echo $HOSTNAME", output: l => hostOutter = l);
                }
            } catch (Exception e)
            {
                throw e.UnrollAggregateExceptions();
            }
        }

        [TestMethod]
        public void CopyRemoteDirectoryLocal()
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
        public void CopyBadRemoteDirectoryLocal()
        {
            try
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
            } catch (Exception e)
            {
                throw e.UnrollAggregateExceptions();
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
