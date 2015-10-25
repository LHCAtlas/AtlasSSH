using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AtlasSSHTest
{
    /// <summary>
    /// Test all the extension methods that are linked to getting commands done.
    /// </summary>
    [TestClass]
    public class AtlasCommandsTest
    {
#if false
        ///
        /// These are all off b.c. there are high speed, local command/response guys below.
        ///

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
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddsetupVomsProxyInit("bogus")
                );
            s
                .setupATLAS()
                .setupRucio("bogus")
                .VomsProxyInit("atlas", "bogus");
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
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddsetupVomsProxyInit("bogus")
                .AddRucioListFiles("user.gwatts:user.gwatts.301295.EVNT.1")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .VomsProxyInit("atlas", "bogus")
                .FilelistFromGRID("user.gwatts:user.gwatts.301295.EVNT.1");

            foreach (var fname in r)
            {
                Console.WriteLine(fname);
            }
            Assert.AreEqual(10, r.Length);
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

        [TestMethod]
        public void setupGoodRcRelease()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("/tmp/gwattsbogusrel", "Base,2.3.30")
                );

            s
                .setupATLAS()
                .SetupRcRelease("/tmp/gwattsbogusrel", "Base,2.3.30");
        }

        [TestMethod]
        public void setupRcInNullDirectory()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("", "Base,2.3.30")
                );

            util.CatchException(() => s.setupATLAS().SetupRcRelease("", "Base,2.3.30"), typeof(ArgumentException), "must be an absolute Linux path");
        }

        [TestMethod]
        public void setupRcInRelativeDirectory()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("", "Base,2.3.30")
                );

            util.CatchException(() => s.setupATLAS().SetupRcRelease("bogus/dude", "Base,2.3.30"), typeof(ArgumentException), "must be an absolute Linux path");
        }

        [TestMethod]
        public void setupBadRcRelease()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("/tmp/gwattsbogusrel", "Base,2.3.31")
                .AddEntry("rcSetup Base,2.3.31", @"Warning!! Base,stable release 2.3.55 NOT available
Available releases are:
asgType=stable (at /cvmfs/atlas.cern.ch/repo/sw/ASG):
 Base:  1.0.0 1.0.1 1.1.0 1.2.0 1.3.0 1.4.0 1.5.0 1.5.1 1.5.2 1.5.3 1.5.4 1.5.5
        1.5.6 1.5.7 1.5.8 1.5.9 1.5.10 1.5.11 1.5.13 1.5.14
        2.0.0 2.0.1 2.0.2 2.0.3 2.0.4 2.0.5 2.0.6 2.0.7 2.0.8 2.0.9 2.0.10
        2.0.11 2.0.12 2.0.13 2.0.14 2.0.15 2.0.16 2.0.17 2.0.18 2.0.19 2.0.20
        2.0.21 2.0.22 2.0.23 2.0.24 2.0.25 2.0.26 2.0.27
        2.1.0 2.1.11 2.1.19 2.1.21 2.1.22 2.1.23 2.1.24 2.1.25 2.1.26 2.1.27
        2.1.28 2.1.29 2.1.30 2.1.31 2.1.32 2.1.32-MAC 2.1.33 2.1.34 2.1.34-MAC
        2.1.35 2.1.36 2.2.0 2.2.1 2.2.2 2.2.3 2.2.4 2.2.5 2.3.0 2.3.1 2.3.2
        2.3.3 2.3.4 2.3.5 2.3.6 2.3.7 2.3.8 2.3.9 2.3.10 2.3.11 2.3.11-MAC
        2.3.12 2.3.13 2.3.13-MAC 2.3.14 2.3.15 2.3.15-MAC 2.3.16 2.3.16-MAC
        2.3.17 2.3.17-MAC 2.3.18 2.3.18-MAC 2.3.19 2.3.19-MAC 2.3.20 2.3.20-MAC
        2.3.21 2.3.21-MAC 2.3.22 2.3.22-MAC 2.3.23 2.3.23-MAC 2.3.24 2.3.24-MAC
        2.3.25 2.3.25-MAC 2.3.26 2.3.28 2.3.28-MAC 2.3.29 2.3.29-MAC 2.3.30
        2.3.30-MAC 2.3.31 2.3.31-MAC 2.3.32 2.3.32-MAC

Or run 'rcSetup -r' for a full available release
")
                );

            util.CatchException(() => s.setupATLAS().SetupRcRelease("/tmp/gwattsbogusrel","Base,2.3.31"), typeof(LinuxMissingConfigurationException), "2.3.31");
        }

        [TestMethod]
        public void setupBadDirRcRelease()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("/tmp/gwattsbogusrel", "Base,2.3.30")
                .AddEntry("mkdir /tmp/gwattsbogusrel", "mkdir: cannot create directory `/tmp/gwattsbogusrel': Permission denied")
                );

            util.CatchException(() => s.setupATLAS().SetupRcRelease("/tmp/gwattsbogusrel", "Base,2.3.30"), typeof(LinuxMissingConfigurationException), "Unable to create release directory");
        }

        [TestMethod]
        public void setupAlreadyCreatedDirRcRelease()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRcSetup("/tmp/gwattsbogusrel", "Base,2.3.30")
                .AddEntry("mkdir /tmp/gwattsbogusrel", "mkdir: cannot create directory `/tmp/gwattsbogusrel': File exists")
                );

            util.CatchException(() => s.setupATLAS().SetupRcRelease("/tmp/gwattsbogusrel", "Base,2.3.30"), typeof(LinuxMissingConfigurationException), "already exists");
        }
    }
}
