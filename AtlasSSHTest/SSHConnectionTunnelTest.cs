using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    [TestClass]
    public class SSHConnectionTunnelTest
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SSHTunnelFailWithNoInitalization()
        {
            using (var t = new SSHConnectionTunnel())
            {
                var a = t.Connection;
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SSHTunnelIterateWithNothing()
        {
            using (var t = new SSHConnectionTunnel())
            {
                Assert.AreEqual(0, t.Count());
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public void SSHTunnelSingleLink()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                var pid = GetPID(t.Connection);
                Assert.IsTrue(pid != "");
            }
        }

        /// <summary>
        /// Get the PID for a process back.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private string GetPID (SSHConnection c)
        {
            var pid = "";
            c.ExecuteLinuxCommand("echo $$", s => pid = s);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(pid));
            return pid;
        }

        [TestMethod]
        [DeploymentItem("testMachineOne.txt")]
        public void SSHTunnelAddLater()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                var pid1 = GetPID(t.Connection);
                t.Add(File.ReadLines("testmachineone.txt").First());
                var pid2 = GetPID(t.Connection);
                Assert.AreNotEqual(pid1, pid2);
            }
        }

        [TestMethod]
        [DeploymentItem("testMachineTwo.txt")]
        public void SSHTunnelAddTwo()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testMachineTwo.txt").First()))
            {
                var pid2 = GetPID(t.Connection);
                Assert.IsTrue(pid2 != "");
                Console.WriteLine(pid2);
            }
        }
    }
}
