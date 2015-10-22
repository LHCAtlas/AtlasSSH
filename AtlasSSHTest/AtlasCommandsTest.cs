using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AtlasSSHTest
{
    [TestClass]
    public class AtlasCommandsTest
    {
#if false
        [TestMethod]
        public void setupATLAS()
        {
            // THis contains an internal check, so no need to do anything special here.
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s.setupATLAS();
            }
        }

        [TestMethod]
        public void setupRucioWithATLASSetup()
        {
            // This contains an internal check, so we only need to watch for it to "work".
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void setupRucioWithoutATLASSetup()
        {
            // This contains an internal check, so we only need to watch for it to "work".
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupRucio(info.Item2);
            }
        }
#endif

        [TestMethod]       
        public void setupATLAS()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>().AddsetupATLASResponses());
            s.setupATLAS();
        }

        [TestMethod]
        public void setupATLASNosetupATLASThere()
        {
            var setupATLASBad = @"-bash: setupATLAS: command not found";
            var aliasResponse = @"alias asetup='source $AtlasSetup/scripts/asetup.sh'
alias diagnostics='source ${ATLAS_LOCAL_ROOT_BASE}/swConfig/Post/diagnostics/setup-Linux.sh'
alias helpMe='${ATLAS_LOCAL_ROOT_BASE}/utilities/generateHelpMe.sh'
alias l.='ls -d .* --color=auto'
alias ll='ls -l --color=auto'
alias ls='ls --color=auto'
alias printMenu='$ATLAS_LOCAL_ROOT_BASE/swConfig/printMenu.sh ""all""'
alias setupATLAS='source $ATLAS_LOCAL_ROOT_BASE/user/atlasLocalSetup.sh'
alias showVersions='${ATLAS_LOCAL_ROOT_BASE}/utilities/showVersions.sh'
alias vi='vim'
alias which='alias | /usr/bin/which --tty-only --read-alias --show-dot --show-tilde'
";
            var s = new dummySSHConnection(new Dictionary<string, string>() { 
            { "setupATLAS", setupATLASBad },
            { "alias", aliasResponse}
            });
            util.CatchException(() => s.setupATLAS(), typeof(LinuxConfigException), "setupATLAS command did not have the expected effect");
        }

        [TestMethod]
        public void setupRucioWithoutATLASSetup()
        {
            // Fails when we don't get right setup
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupRucioResponses("bogus")
                .AddEntry("localSetupRucioClients", "-bash: localSetupRucioClients: command not found")
                .AddEntry("hash rucio", "-bash: hash: rucio: not found")
                );

            util.CatchException(() => s.setupRucio("bogus"), typeof(LinuxConfigException), "Unable to setup Rucio");
        }

        [TestMethod]
        public void setupRucio()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                );

            s.setupRucio("bogus");
        }

        [TestMethod]
        public void vomsProxyInit()
        {
            // Init a voms proxy correctly. There is an internal check, so this should
            // be fine.
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2);
            }
        }

        [TestMethod]
        public void vomsProxyInitWithInjectedText()
        {
            // Do it right, but as we would expect.
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void vomsProxyWithBadUsername()
        {
            // Make sure that we fail when the username is bad
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", "freak-out");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void vomsProxyWithBadPassword()
        {
            // Make sure that we fail when the username is bad
            var info = util.GetUsernameAndPassword();
            util.SetPassword("GRID", "VOMSTestUser", "bogus-password");
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", "VOMSTestUser");
            }
        }

        [TestMethod]
        public void vomsProxyInitWithInjectedGoodResponse()
        {
            Assert.Inconclusive();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void downloadBadDS()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2)
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.11111", "/tmp/usergwattstempdata");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void downloadToBadDir()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2)
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1", "/fruitcake/usergwattstempdata");
            }
        }

        [TestMethod]
        public void getDSFileList()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                var r = s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2)
                    .FilelistFromGRID("user.gwatts:user.gwatts.301295.EVNT.1");
                foreach (var fname in r)
                {
                    Console.WriteLine(fname);
                }
                Assert.AreEqual(10, r.Length);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void getBadDSFileList()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                var r = s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2)
                    .FilelistFromGRID("user.gwatts:user.gwatts.301295.EVNT.1111");
            }
        }

        [TestMethod]
        public void downloadDS()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
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
        }

        [TestMethod]
        public void downloadDSSelection()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas", info.Item2)
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1", "/tmp/usergwattstempdataShort", fileNameFilter: files =>
                    {
                        Assert.AreEqual(10, files.Length);
                        return files.OrderBy(f => f).Take(2).ToArray();
                    });

                // Now, check!
                var foundfiles = new List<string>();
                s.ExecuteCommand("find /tmp/usergwattstempdataShort -type f", l => foundfiles.Add(l));

                foreach (var fname in foundfiles)
                {
                    Console.WriteLine("-> " + fname);
                }

                Assert.AreEqual(2, foundfiles.Count);
            }
        }

    }
}
