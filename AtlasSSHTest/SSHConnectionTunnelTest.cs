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
        public async Task SSHTunnelFailWithNoInitalization()
        {
            try
            {
                using (var t = new SSHConnectionTunnel())
                {
                    var pid = await GetPID(t);
                }
            } catch (Exception e)
            {
                throw e.UnrollAggregateExceptions();
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public async Task SSHTunnelSingleLink()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                var pid = await GetPID(t);
                Assert.IsTrue(pid != "");
                Assert.AreEqual(0, t.TunnelCount);
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public async Task SSHTunnelSingleLinkStressTest1()
        {
            await BuildAndRunTunnels(1);
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public async Task SSHTunnelSingleLinkStressTest10()
        {
            await BuildAndRunTunnels(10);
        }

        [TestMethod]
        [DeploymentItem("testmachineTwo.txt")]
        public async Task SSHTunnelDoubleLinkStressTest10()
        {
            await BuildAndRunTunnels(10, configFile: "testMachineTwo.txt");
        }

        /// <summary>
        /// Run a number of simultanious tunnels
        /// </summary>
        /// <param name="tunnels"></param>
        /// <returns></returns>
        private async Task BuildAndRunTunnels(int tunnels, string configFile = "testMachineOne.txt")
        {
            var ts = Enumerable.Range(0, tunnels)
                .Select(_ => BuildAndRunTunnel(configFile));
            await Task.WhenAll(ts);
        }

        private async Task BuildAndRunTunnel(string configFile)
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines(configFile).First()))
            {
                var pid = await GetPID(t);
                Assert.IsTrue(pid != "");
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineOne.txt")]
        public void GloballyVisibleTunnel()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                Assert.IsTrue(t.GloballyVisible);
                Assert.AreEqual(0, t.TunnelCount);
            }
        }

        [TestMethod]
        [DeploymentItem("testmachineTwo.txt")]
        public void GloballyNotVisibleTunnel()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineTwo.txt").First()))
            {
                Assert.IsFalse(t.GloballyVisible);
                Assert.AreEqual(1, t.TunnelCount);
            }
        }

        /// <summary>
        /// Get the PID for a process back.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private async Task<string> GetPID (ISSHConnection c)
        {
            var pid = "";
            await c.ExecuteLinuxCommandAsync("echo $$", s => pid = s);
            Assert.IsTrue(!string.IsNullOrWhiteSpace(pid));
            return pid;
        }

        [TestMethod]
        [DeploymentItem("testMachineOne.txt")]
        public async Task SSHTunnelAddLater()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testmachineOne.txt").First()))
            {
                var pid1 = await GetPID(t);
                t.Add(File.ReadLines("testmachineone.txt").First());
                var pid2 = await GetPID(t);
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
                Assert.AreEqual(text.Substring(0, text.IndexOf('@')), t.UserName);
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
                Assert.AreEqual(machineTwo.Substring(0, machineTwo.IndexOf('@')), t.UserName);
                Assert.AreEqual(machineTwo.Substring(machineTwo.IndexOf('@') + 1), t.MachineName);
            }
        }

        [TestMethod]
        [DeploymentItem("testMachineTwo.txt")]
        public async Task SSHTunnelAddTwo()
        {
            using (var t = new SSHConnectionTunnel(File.ReadLines("testMachineTwo.txt").First()))
            {
                var pid2 = await GetPID(t);
                Assert.IsTrue(pid2 != "");
                Assert.AreEqual(1, t.TunnelCount);
                Console.WriteLine(pid2);

                await t.ExecuteLinuxCommandAsync("export", l => Console.WriteLine(l));
            }
        }
    }
}
