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
            try
            {
                using (var t = new SSHConnectionTunnel())
                {
                    var pid = GetPID(t);
                }
            } catch (Exception e)
            {
                throw e.UnrollAggregateExceptions();
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public void SSHTunnelSingleLink()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                var pid = GetPID(t);
                Assert.IsTrue(pid != "");
                Assert.AreEqual(0, t.TunnelCount);
            }
        }

        /// <summary>
        /// Get the PID for a process back.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private string GetPID (ISSHConnection c)
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
                var pid1 = GetPID(t);
                t.Add(File.ReadLines("testmachineone.txt").First());
                var pid2 = GetPID(t);
                Assert.AreNotEqual(pid1, pid2);
                Assert.AreEqual(1, t.TunnelCount);
            }
        }

        [TestMethod]
        [DeploymentItem("testMachineOne.txt")]
        public void SSHTunnelMachineUserName()
        {
            var text = File.ReadLines("testmachineOne.txt").First();
            using (var t = new SSHConnectionTunnel(text))
            {
                Assert.AreEqual(text.Substring(0, text.IndexOf('@')), t.Username);
                Assert.AreEqual(text.Substring(text.IndexOf('@')+1), t.MachineName);
            }
        }

        [TestMethod]
        [DeploymentItem("testMachineTwo.txt")]
        public void SSHTunnelTwoMachineUserName()
        {
            var text = File.ReadLines("testMachineTwo.txt").First();
            var machineTwo = text.Split(new [] { "->" }, StringSplitOptions.None)
                .Select(i => i.Trim())
                .Last();
            using (var t = new SSHConnectionTunnel(text))
            {
                Assert.AreEqual(machineTwo.Substring(0, machineTwo.IndexOf('@')), t.Username);
                Assert.AreEqual(machineTwo.Substring(machineTwo.IndexOf('@') + 1), t.MachineName);
            }
        }

        [TestMethod]
        [DeploymentItem("testMachineTwo.txt")]
        public void SSHTunnelAddTwo()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testMachineTwo.txt").First()))
            {
                var pid2 = GetPID(t);
                Assert.IsTrue(pid2 != "");
                Assert.AreEqual(1, t.TunnelCount);
                Console.WriteLine(pid2);
            }
        }
    }
}
