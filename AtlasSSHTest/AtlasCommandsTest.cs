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
        public void setupATLASInjectedGOOD()
        {
            var setupATLASGoodResponse = @"lsetup               lsetup <tool1> [ <tool2> ...] (see lsetup -h):
 lsetup agis          (or localSetupAGIS) to use AGIS
 lsetup asetup        (or asetup) to use an Athena release
 lsetup atlantis      (or localSetupAtlantis) to use Atlantis
 lsetup dq2           (or localSetupDQ2Client) to use DQ2Client
 lsetup eiclient      (or localSetupEIClient) to use EIClient
 lsetup emi           (or localSetupEmi) to use  emi
 lsetup fax           (or localSetupFAX) to use FAX
 lsetup ganga         (or localSetupGanga) to use Ganga
 lsetup lcgenv        to use lcgenv
 lsetup panda         (or localSetupPandaClient) to use Panda Client
 lsetup pod           (or localSetupPoD) to use Proof-on-Demand
 lsetup pyami         (or localSetupPyAMI) to use pyAMI
 lsetup rcsetup       (or rcSetup) to setup an ASG release
 lsetup root          (or localSetupROOT) to use ROOT
 lsetup rucio         (or localSetupRucioClients) to use rucio-clients
 lsetup sft           (or localSetupSFT) to use SFT packages
 lsetup xrootd        (or localSetupXRootD) to use XRootD
advancedTools        for advanced tools
diagnostics          for diagnostic tools
helpMe               more help
printMenu            show this menu
showVersions         show versions of installed software

19 Jun 2015
  You are encouraged to use rucio instead of DQ2 clients, type
       lsetup rucio
     For more info: https://twiki.cern.ch/twiki/bin/view/AtlasComputing/RucioClientsHowTo
";
            var aliasResponse = @"alias asetup='source $AtlasSetup/scripts/asetup.sh'
alias atlasLocalPythonSetup='localSetupPython'
alias atlasLocalRootBaseSetup='source ${ATLAS_LOCAL_ROOT_BASE}/user/atlasLocalSetup-v2.sh'
alias changeASetup='source $ATLAS_LOCAL_ROOT_BASE/swConfig/asetup/changeAsetup.sh'
alias changeRCSSetup='source $ATLAS_LOCAL_ROOT_BASE/swConfig/rcsetup/changeRcsetup.sh'
alias diagnostics='source ${ATLAS_LOCAL_ROOT_BASE}/swConfig/Post/diagnostics/setup-Linux.sh'
alias helpMe='${ATLAS_LOCAL_ROOT_BASE}/utilities/generateHelpMe.sh'
alias l.='ls -d .* --color=auto'
alias ll='ls -l --color=auto'
alias localSetupAGIS='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh agis'
alias localSetupAtlantis='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh atlantis'
alias localSetupBoost='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh boost'
alias localSetupDQ2Client='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh dq2'
alias localSetupDavix='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh davix'
alias localSetupEIClient='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh eiclient'
alias localSetupEmi='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh emi'
alias localSetupFAX='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh fax'
alias localSetupGanga='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh ganga'
alias localSetupGcc='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh gcc'
alias localSetupPacman='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh pacman'
alias localSetupPandaClient='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh panda'
alias localSetupPoD='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh pod'
alias localSetupPyAMI='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh pyami'
alias localSetupPython='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh python'
alias localSetupROOT='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh root'
alias localSetupRucioClients='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh rucio'
alias localSetupSFT='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh sft'
alias localSetupXRootD='source ${ATLAS_LOCAL_ROOT_BASE}/utilities/oldAliasSetup.sh xrootd'
alias ls='ls --color=auto'
alias printMenu='$ATLAS_LOCAL_ROOT_BASE/swConfig/printMenu.sh ""all""'
alias rcSetup='source $ATLAS_LOCAL_RCSETUP_PATH/rcSetup.sh'
alias rcsetup='rcSetup'
alias setupATLAS='source $ATLAS_LOCAL_ROOT_BASE/user/atlasLocalSetup.sh'
alias showVersions='${ATLAS_LOCAL_ROOT_BASE}/utilities/showVersions.sh'
alias vi='vim'
alias which='alias | /usr/bin/which --tty-only --read-alias --show-dot --show-tilde'
";

            var s = new dummySSHConnection(new Dictionary<string, string>() {
            { "setupATLAS", setupATLASGoodResponse },
            { "alias", aliasResponse }
            });
            s.setupATLAS();
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void setupATLASInjectedNosetupATLASThere()
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
            s.setupATLAS();
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
