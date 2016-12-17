using AtlasSSH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasSSHTest
{
    /// <summary>
    /// Test all the extension methods that are linked to getting commands done.
    /// </summary>
    [TestClass]
    public class AtlasCommandsTest
    {
        [TestInitialize]
        public void InitCaches()
        {
            new DiskCache("GRIDFileInfoCache").Clear();
        }

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

#if false
        // Can't actually test this due to password injection.
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
                .VomsProxyInit("atlas");
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
                    .VomsProxyInit("atlas");
            }
        }
#endif

        [TestMethod]
        [Ignore] // Because this requires real remote and is slow.
        public void RunATLASSetup()
        {
            // Dirt simple test to make sure setup ATLAS actually works.
            // Seen some failures with new versions of SSH.NET in the field.
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS();
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore] // Because this requires real remote and is slow.
        public void downloadBadDS()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.11111", "/tmp/usergwattstempdata");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore] // Because this requires real remote and is slow.
        public void downloadToBadDir()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1", "/fruitcake/usergwattstempdata");
            }
        }

        [TestMethod]
        public void getDSFileList()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("user.gwatts:user.gwatts.301295.EVNT.1")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .FilelistFromGRID("user.gwatts:user.gwatts.301295.EVNT.1");

            foreach (var fname in r)
            {
                Console.WriteLine(fname);
            }
            Assert.AreEqual(10, r.Length);
        }

        [TestMethod]
        public void getDSFileListInfoNoEventsBytes()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("user.gwatts:user.gwatts.301295.EVNT.1")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .FileInfoFromGRID("user.gwatts:user.gwatts.301295.EVNT.1");

            foreach (var fname in r)
            {
                Console.WriteLine(fname);
            }
            Assert.AreEqual(10, r.Count);
            Assert.AreEqual(2568, (int) r.Sum(i => i.size));
            Assert.AreEqual(0, r.Sum(i => i.eventCount));
        }

        [TestMethod]
        public void getDSFileListInfoNoEventsGBytes()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .FileInfoFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282");

            foreach (var fname in r)
            {
                Console.WriteLine($"{fname.size} - {fname.name}");
            }
            Assert.AreEqual(40, r.Count);
            Assert.AreEqual(39*10000+5000, r.Sum(i => i.eventCount));
            Assert.AreEqual((int)((4.4*39.5) * 1024), (int)r.Sum(i => i.size));
        }

        [TestMethod]
        public void getZeroDSFileList()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("user.gwatts.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.DAOD_EXOT15.r6765_r6282_p2452.DiVertAnalysis_v4_539A3CCD_hist")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .FilelistFromGRID("user.gwatts.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.DAOD_EXOT15.r6765_r6282_p2452.DiVertAnalysis_v4_539A3CCD_hist");

            foreach (var fname in r)
            {
                Console.WriteLine(fname);
            }
            Assert.AreEqual(0, r.Length);
        }

        [TestMethod]
        public void getDSFileListT()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                );
            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .FilelistFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282");

            foreach (var fname in r)
            {
                Console.WriteLine(fname);
            }
            Assert.AreEqual(40, r.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        [Ignore] // Because this requires real remote and is slow.
        public void getBadDSFileList()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                var r = s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
                    .FilelistFromGRID("user.gwatts:user.gwatts.301295.EVNT.1111");
            }
        }

        [TestMethod]
        public void DownloadDS()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                .AddGoodDownloadResponses("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now")
                );

            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .DownloadFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now");
        }

        [TestMethod]
        [ExpectedException(typeof(FileFailedToDownloadException))]
        public void DownloadDSBadFileDownload()
        {
            // A file fails to download correctly.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                .AddBadDownloadResponses("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now")
                );

            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .DownloadFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now");
        }

        [TestMethod]
        [ExpectedException(typeof(ClockSkewException))]
        public void DownloadDSClockSkewFileDownload()
        {
            // A file fails to download correctly due to clock skew... But we don't catch up in the timeout times.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                .AddClockSkewDownloadResponses("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now")
                );

            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .DownloadFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now");
        }

        [TestMethod]
        public void DownloadDSClockSkewThatGetsBetterFileDownload()
        {
            // A file fails to download correctly due to clock skew... but a little while later it is ok.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupATLASResponses()
                .AddsetupRucioResponses("bogus")
                .AddRucioListFiles("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
                .AddClockSkewDownloadResponses("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now")
                );
            s.AddClockSkewDownloadOKResponses("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282");

            var r = s
                .setupATLAS()
                .setupRucio("bogus")
                .DownloadFromGRID("mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282", "/tmp/gwattsdownload/now");
        }

        [TestMethod]
        [Ignore] // Because this requires real remote and is slow.
        public void downloadDSFromGRID()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
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
        [Ignore] // Because this requires real remote and is slow.
        public void downloadDSSelection()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .ExecuteCommand("rm -rf /tmp/usergwattstempdataShort")
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
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
        [Ignore] // Because this requires real remote and is slow.
        public void downloadDSSelectionWithSlash()
        {
            var info = util.GetUsernameAndPassword();
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s
                    .ExecuteCommand("rm -rf /tmp/usergwattstempdataShort")
                    .setupATLAS()
                    .setupRucio(info.Item2)
                    .VomsProxyInit("atlas")
                    .DownloadFromGRID("user.gwatts:user.gwatts.301295.EVNT.1/", "/tmp/usergwattstempdataShort", fileNameFilter: files =>
                    {
                        Assert.AreEqual(10, files.Length);
                        return files.OrderBy(f => f).Take(1).ToArray();
                    });

                // Now, check!
                var foundfiles = new List<string>();
                s.ExecuteCommand("find /tmp/usergwattstempdataShort -type f", l => foundfiles.Add(l));

                foreach (var fname in foundfiles)
                {
                    Console.WriteLine("-> " + fname);
                }

                Assert.AreEqual(1, foundfiles.Count);
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

        [TestMethod]
        public void kinitGood()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupKinit("bogus@CERN.CH", "mypassword")
                );

            s
                .Kinit("bogus@CERN.CH", "mypassword");
        }

        [TestMethod]
        public void KinitBadDomain()
        {
            //-bash-4.1$ kinit gwatts@CERN.BOGUS
            //kinit: Cannot resolve servers for KDC in realm "CERN.BOGUS" while getting initial credentials

            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupKinit("bogus@CERN.CH", "mypassword")
                .AddEntry("echo mypassword | kinit bogus@CERN.CH", @"kinit: Cannot resolve servers for KDC in realm ""CERN.BOGUS"" while getting initial credentials")
                );

            util.CatchException(() => s.Kinit("bogus@CERN.CH", "mypassword"), typeof(LinuxCommandErrorException), "Cannot resolve servers");
        }

        [TestMethod]
        public void KinitBadPassword()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupKinit("bogus@CERN.CH", "mypassword")
                .AddEntry("echo mypassword | kinit bogus@CERN.CH", @"Password for gwatts@CERN.CH:
kinit: Preauthentication failed while getting initial credentials")
                );

            util.CatchException(() => s.Kinit("bogus@CERN.CH", "mypassword"), typeof(LinuxCommandErrorException), "failed while getting initial");
        }

        [TestMethod]
        public void KinitCommandNotKnown()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddsetupKinit("bogus@CERN.CH", "mypassword")
                .AddEntry("echo mypassword | kinit bogus@CERN.CH", @"-bash: kinit: command not found")
                );

            util.CatchException(() => s.Kinit("bogus@CERN.CH", "mypassword"), typeof(LinuxCommandErrorException), "command not found");
        }

        [TestMethod]
        public void CheckoutNoTagsDirectoryAllowed()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                );

            util.CatchException(() => s.CheckoutPackage("atlasoff/Event/xAOD/xAODTrigger/tags", ""), typeof(ArgumentException), "tags");
        }

        [TestMethod]
        public void CheckoutNoTrunkAllowed()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                );

            util.CatchException(() => s.CheckoutPackage("atlasoff/Event/xAOD/xAODTrigger/trunk", ""), typeof(ArgumentException), "trunk");
        }

        [TestMethod]
        public void CheckoutByRevision()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddCheckoutFromRevision("atlasoff/Event/xAOD/xAODTrigger", "704382")
                );

            s.CheckoutPackage("atlasoff/Event/xAOD/xAODTrigger", "704382");
        }

        [TestMethod]
        public void CheckoutNoTrailingSlash()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddCheckoutFromRevision("atlasoff/Event/xAOD/xAODTrigger", "704382")
                );

            util.CatchException(() => s.CheckoutPackage("atlasoff/Event/xAOD/xAODTrigger/", "704382"), typeof(ArgumentException), "ends with a slash");
        }

        [TestMethod]
        public void CheckoutSubDirOfTrunk()
        {
            // Check out a package that is in a sub-dir of trunk.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddCheckoutFromRevisionTrunk("atlasoff/Event/xAOD/xAODTrigger/trunk/subpkg", "704382")
                );

            s.CheckoutPackage("atlasoff/Event/xAOD/xAODTrigger/trunk/subpkg", "704382");

        }

        [TestMethod]
        public void CheckoutByRevisionWithReleasePackage()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                );

            util.CatchException(() => s.CheckoutPackage("xAODTrigger", "704382"), typeof(ArgumentException), "revision is specified then the package path must be fully specified");
        }

        [TestMethod]
        public void CheckoutByReleasePackage()
        {
            // Just checkout a package as it is listed in the release.
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddCheckoutFromRelease("xAODTrigger")
                );

            s.CheckoutPackage("xAODTrigger", "");
        }

        [TestMethod]
        public void CheckoutNonexistantPackage()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddEntry("rc checkout_pkg xAODForward", @"RootCore: Error unknown package xAODForward")
                );

            util.CatchException(() => s.CheckoutPackage("xAODForward", ""), typeof(LinuxCommandErrorException), "Unable to check out svn package xAODForward");
        }

        [TestMethod]
        public void ExecuteBadLinuxCommand()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddEntry("ls bogusduduefreak", @"ls: cannot access bogusduduefreak: No such file or directory")
                .AddEntry("echo $?", "2")
                );

            util.CatchException(() => s.ExecuteLinuxCommand("ls bogusduduefreak"), typeof(LinuxCommandErrorException), "The remote command");
        }

        [TestMethod]
        public void ExecuteGoodLinuxCommand()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddGoodLinuxCommand()
                .AddEntry("ls", @"1                        cmt                        rachel
105279.root              cmthome                    runBuildTest.sh
191517_DESD.list         dbrunner.txt               runBuildTest.sh~
191517_DESD.list~        doe2010.p12                runSingleBuildTest.sh
AnalysisExamples         doit.sh                    runSingleBuildTest.sh~
AnalysisExamples.tar.gz  doitall.sh                 runwill.sh
D2011-ttbar-sf.txt       doitall.sh~                setupLocalRelease.sh
Desktop                  dojunk.sh                  setupLocalRelease.sh~
Documents                done.txt                   setupScripts
Downloads                dq2-datasets.py            supportInfoNoASetup.txt
Evgen.log                dq2-datasets.py~           tally-cvmfs-v1.txt
HV                       dump_20140527_1610.txt.gz  tally-old.txt
Music                    dump_20140527_1611.txt.gz  tally.txt
Pictures                 dump_20140606_0120.txt.gz  tally.txt-2-19-10.bak
PoolFileCatalog.xml      egammaTimeCorrConfig.py    tally.txt-2-26-10.txt
PoolFileCatalog.xml.BAK  fndbrelease.txt            tally2.txt
Public                   junk                       temp
Templates                krb5.conf                  test.c
Videos                   krb5.conf~                 testPROOF
atest.root               libcurl.so.3               testSize
athena_gen.out           log.txt                    testarea
athena_sim.log           longsvn.txt                usercert.pem
bin                      notebooks                  userkey.pem
bogus                    oradiag_gwatts             wget-log
bogus_dude               proof                      workarea
build_nv_ntuple.txt      python                     xAOForward.tar.gz")
                );

            s.ExecuteLinuxCommand("ls");
        }

        [TestMethod]
        public void CompileWithNoErrors()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddBuildCommandResponses()
            );

            s.BuildWorkArea();
        }

        [TestMethod]
        public void CompileBadFindPackages()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddEntry("rc find_packages", @"bash-4.1$ rc find_packages                                                                                                      
using release set with set_release                                                                                              
looking for packages in /phys/users/gwatts/bogus                                                                                

packages found:
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ApplyJetCalibration
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AsgExampleTools    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AsgTools           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_Boost          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_Doxygen        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_Eigen          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_FastJet        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_RooUnfold      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_Test           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/Asg_root           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AssociationUtils   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AthContainers      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AthContainersInterfaces
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/AthLinks               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/CPAnalysisExamples     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/CalibrationDataInterface
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/CaloGeoHelpers          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/CxxUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/DiTauMassTools          
/phys/users/gwatts/bogus/DiVertAnalysis                                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ElectronEfficiencyCorrection
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ElectronPhotonFourMomentumCorrection
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ElectronPhotonSelectorTools         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ElectronPhotonShowerShapeFudgeTool  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/EventLoop                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/EventLoopAlgs                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/EventLoopGrid                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/EventPrimitives                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/EventShapeInterface                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/FourMomUtils                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/FsrUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/GeoPrimitives                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/GoodRunsLists                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/InDetTrackSelectionTool             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/IsolationCorrections                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/IsolationSelection                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetCPInterfaces                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetCalibTools                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetEDM                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetMomentTools                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetRec                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetResolution                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetSelectorTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetSubStructureMomentTools          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetSubStructureUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetUncertainties                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/JetUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MCTruthClassifier                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/METInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/METUtilities                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MultiDraw                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MuonEfficiencyCorrections           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MuonIdHelpers                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MuonMomentumCorrections             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/MuonSelectorTools                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PATCore                             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PATInterfaces                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PathResolver                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PhotonEfficiencyCorrection          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PhotonVertexSelection               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/PileupReweighting                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/QuickAna                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ReweightUtils                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/RootCore                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/RootCoreUtils                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/SampleHandler                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/SemileptonicCorr                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TauAnalysisTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TauCorrUncert                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TestTools                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrackVertexAssociationTool          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigAnalysisInterfaces              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigBunchCrossingTool               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigConfBase                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigConfHLTData                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigConfInterfaces                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigConfL1Data                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigConfxAOD                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigDecisionInterface               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigDecisionTool                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigEgammaMatchingTool              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigMuonEfficiency                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigMuonMatching                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigNavStructure                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigSteeringEvent                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TrigTauMatching                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/TruthUtils                          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/ZMassConstraint                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/egammaLayerRecalibTool              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/egammaMVACalib                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODAssociations                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODBPhys                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODBTagging                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODBTaggingEfficiency              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODBase                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODCaloEvent                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODCore                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODCutFlow                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODEgamma                          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODEventFormat                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODEventInfo                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODEventShape                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODHIEvent                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODJet                             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODLuminosity                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODMetaData                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODMetaDataCnv                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODMissingET
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODMuon
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODPFlow
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODParticleEvent
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODPrimitives
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODRootAccess
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODRootAccessInterfaces
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTau
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTracking
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigBphys
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigCalo
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigEgamma
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigL1Calo
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigMinBias
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigMissingET
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigMuon
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigRinger
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTrigger
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTriggerCnv
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.32/xAODTruth
RootCore: Error package boguspkg not known, required by DiVertAnalysis")
                .AddEntry("echo $?", "1")
            );

            util.CatchException(() => s.BuildWorkArea(), typeof(LinuxCommandErrorException), "boguspkg not known");
        }

        [TestMethod]
        public void CompileSourceCodeError()
        {
            var s = new dummySSHConnection(new Dictionary<string, string>()
                .AddBuildCommandResponses()
                .AddEntry("rc compile", @"compiling RootCore  
finished compiling RootCore
compiling DiVertAnalysis   
Making directory /phys/users/gwatts/bogus/RootCoreBin/obj/x86_64-slc6-gcc48-opt/DiVertAnalysis/obj
Making dependency for Common.cxx                                                                  
Making dependency for CalRatioNoTrig.cxx                                                          
Making dependency for CalRatioEmul.cxx                                                            
Making dependency for LinkDef.h                                                                   
Making dependency for DiLeptonFinder.cxx                                                          
Making dependency for DiVertAnalysisLocalFileRunner.cxx                                           
Making dependency for CalRatioTrig.cxx                                                            
Making dependency for DiVertUtils.cxx                                                             
Making dependency for LinkDef.h                                                                   
Making dependency for TrigUtils.cxx                                                               
Making dependency for DiVertAnalysis.cxx                                                          
Making dependency for MuRoIClusTrig.cxx                                                           
Making dependency for PrepObjects.cxx                                                             
Making dependency for TruthUtils.cxx                                                              
Making dependency for MSVertexFinder.cxx                                                          
Making dependency for DiVertAnalysisRunner.cxx                                                    
Compiling DiVertUtils.o                                                                           
Compiling PrepObjects.o                                                                           
Compiling CalRatioEmul.o                                                                          
Compiling CalRatioNoTrig.o                                                                        
Compiling DiVertAnalysis.o                                                                        
Compiling CalRatioTrig.o                                                                          
Compiling MSVertexFinder.o                                                                        
Compiling DiLeptonFinder.o                                                                        
Compiling TrigUtils.o                                                                             
Compiling TruthUtils.o                                                                            
Compiling MuRoIClusTrig.o                                                                         
Compiling Common.o                                                                                
Making directory /phys/users/gwatts/bogus/RootCoreBin/obj/x86_64-slc6-gcc48-opt/DiVertAnalysis/bin
Compiling DiVertAnalysisLocalFileRunner.o                                                         
Compiling DiVertAnalysisCINT.o                                                                    
Compiling DiVertAnalysisRunner.o                                                                  
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:26:0,
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroup.h:27,        
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroupFunctions.h:19,
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolCore.h:19,
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolStandalone.h:40,
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionTool.h:18,          
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/Common.h:29,              
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/TrigUtils.h:4,            
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/TrigUtils.cxx:1:                    
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::Jet_v1>]':                                                                                                    
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::Jet_v1>; STORED = DataVector<xAOD::Jet_v1>; CONTAINER = DataVector<xAOD::Jet_v1>; EventPtr_t = asg::SgTEvent*]'                                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::Jet_v1>; std::string = std::basic_string<char>]'                                                                         
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/TrigUtils.cxx:42:83:   required from here                             
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
     std::shared_ptr<const STORED> filter_if(boost::mpl::bool_<true> is_same, std::shared_ptr<const STORED>& original,const TrigPassBits* bits){                                                                                                                
                                   ^                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::TrackParticle_v1>]':                                                                                          
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::TrackParticle_v1>; STORED = DataVector<xAOD::TrackParticle_v1>; CONTAINER = DataVector<xAOD::TrackParticle_v1>; EventPtr_t = asg::SgTEvent*]'                                                                              
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::TrackParticle_v1>; std::string = std::basic_string<char>]'                                                               
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/TrigUtils.cxx:43:126:   required from here                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
In file included from input_line_11:1:                                                                                          
In file included from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/LinkDef.h:1:                                    
In file included from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/DiVertAnalysis.h:7:                   
In file included from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/DiVertUtils.h:3:                      
In file included from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/Common.h:29:                          
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionTool.h:18:                      
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolStandalone.h:40:            
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolCore.h:19:                  
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroupFunctions.h:19:                   
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroup.h:25:                            
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/CacheGlobalMemory.h:22:                     
In file included from /cvmfs/atlas.cern.ch/repo/ATLASLocalRootBase/x86_64/Gcc/gcc484_x86_64_slc6/slc6/x86_64-slc6-gcc48-opt/bin/../lib/gcc/x86_64-unknown-linux-gnu/4.8.4/../../../../include/c++/4.8.4/ext/hash_map:60:                                        
/cvmfs/atlas.cern.ch/repo/ATLASLocalRootBase/x86_64/Gcc/gcc484_x86_64_slc6/slc6/x86_64-slc6-gcc48-opt/bin/../lib/gcc/x86_64-unknown-linux-gnu/4.8.4/../../../../include/c++/4.8.4/backward/backward_warning.h:32:2: warning:                                    
      This file includes at least one deprecated or antiquated header which may be removed without further notice at a future   
      date. Please use a non-deprecated interface with equivalent functionality instead. For a listing of replacement headers   
      and interfaces, consult the file backward_warning.h. To disable this warning use -Wno-deprecated. [-W#warnings]           
#warning \                                                                                                                      
 ^                                                                                                                              
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/DiVertAnalysis.cxx: In member function 'virtual EL::StatusCode DiVertAnalysis::execute()':                                                                                                            
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/DiVertAnalysis.cxx:239:23: error: 'getTauTrigTracksVector' is not a member of 'PrepObjects'                                                                                                           
     auto trigTracks = PrepObjects::getTauTrigTracksVector(event);                                                              
                       ^                                                                                                        
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/DiVertAnalysis.cxx:253:62: error: 'trigTauTracks_IDTrig' was not declared in this scope                                                                                                               
     TrigUtils::getCalRatioTriggerEmulator_a4tcemjesPS(m_tdt, trigTauTracks_IDTrig, trigTauJets, Emul_objects, tauRoIs,truthparts, thePV,muons, trigPassed, jetsPassingCalREmul, dPhi_DJ);                                                                      
                                                              ^                                                                 
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:26:0,                    
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroup.h:27,                            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroupFunctions.h:19,                   
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolCore.h:19,                  
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolStandalone.h:40,            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionTool.h:18,                      
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/Common.h:29,                          
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/Common.cxx:1:                                   
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::Jet_v1>]':                                                                                                    
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::Jet_v1>; STORED = DataVector<xAOD::Jet_v1>; CONTAINER = DataVector<xAOD::Jet_v1>; EventPtr_t = asg::SgTEvent*]'                                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::Jet_v1>; std::string = std::basic_string<char>]'                                                                         
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/Common.cxx:188:79:   required from here                               
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
     std::shared_ptr<const STORED> filter_if(boost::mpl::bool_<true> is_same, std::shared_ptr<const STORED>& original,const TrigPassBits* bits){                                                                                                                
                                   ^                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::TrackParticle_v1>]':                                                                                          
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::TrackParticle_v1>; STORED = DataVector<xAOD::TrackParticle_v1>; CONTAINER = DataVector<xAOD::TrackParticle_v1>; EventPtr_t = asg::SgTEvent*]'                                                                              
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::TrackParticle_v1>; std::string = std::basic_string<char>]'                                                               
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/Common.cxx:189:129:   required from here                              
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/boost/system/system_error.hpp:14:0,                          
                 from /phys/users/gwatts/bogus/RootCoreBin/include/boost/thread/exceptions.hpp:22,                              
                 from /phys/users/gwatts/bogus/RootCoreBin/include/boost/thread/pthread/recursive_mutex.hpp:11,                 
                 from /phys/users/gwatts/bogus/RootCoreBin/include/boost/thread/recursive_mutex.hpp:16,                         
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigConfHLTData/HLTPrescaleSetCollection.h:12,               
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigConfHLTData/HLTFrame.h:14,                               
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigEgammaMatchingTool/ITrigEgammaMatchingTool.h:5,          
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigEgammaMatchingTool/TrigEgammaMatchingTool.h:4,           
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/DiLeptonFinder.h:14,                  
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/PrepObjects.h:10,                     
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/DiVertAnalysis/DiVertAnalysis.h:8,                   
                 from /phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis/Root/DiVertAnalysis.cxx:8:                           
/phys/users/gwatts/bogus/RootCoreBin/include/boost/system/error_code.hpp: At global scope:                                      
/phys/users/gwatts/bogus/RootCoreBin/include/boost/system/error_code.hpp:221:36: warning: 'boost::system::posix_category' defined but not used [-Wunused-variable]                                                                                              
     static const error_category &  posix_category = generic_category();                                                        
                                    ^                                                                                           
/phys/users/gwatts/bogus/RootCoreBin/include/boost/system/error_code.hpp:222:36: warning: 'boost::system::errno_ecat' defined but not used [-Wunused-variable]                                                                                                  
     static const error_category &  errno_ecat     = generic_category();                                                        
                                    ^                                                                                           
/phys/users/gwatts/bogus/RootCoreBin/include/boost/system/error_code.hpp:223:36: warning: 'boost::system::native_ecat' defined but not used [-Wunused-variable]                                                                                                 
     static const error_category &  native_ecat    = system_category();                                                         
                                    ^                                                                                           
cc1plus: warning: unrecognized command line option "" - Wno - tautological - undefined - compare"" [enabled by default]                   
make: *** [/ phys / users / gwatts / bogus / RootCoreBin / obj / x86_64 - slc6 - gcc48 - opt / DiVertAnalysis / obj / DiVertAnalysis.o] Error 1
make: ***Waiting for unfinished jobs....                                                                                       
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
In file included from DiVertAnalysisCINT.cxx.h:1:
In file included from / phys / users / gwatts / bogus / trunk@246773 / DiVertAnalysis / DiVertAnalysis / DiVertAnalysis.h:7:
In file included from / phys / users / gwatts / bogus / trunk@246773 / DiVertAnalysis / DiVertAnalysis / DiVertUtils.h:3:
In file included from / phys / users / gwatts / bogus / trunk@246773 / DiVertAnalysis / DiVertAnalysis / Common.h:29:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / TrigDecisionTool.h:18:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / TrigDecisionToolStandalone.h:40:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / TrigDecisionToolCore.h:19:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / ChainGroupFunctions.h:19:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / ChainGroup.h:25:
In file included from / phys / users / gwatts / bogus / RootCoreBin / include / TrigDecisionTool / CacheGlobalMemory.h:22:
In file included from / cvmfs / atlas.cern.ch / repo / ATLASLocalRootBase / x86_64 / Gcc / gcc484_x86_64_slc6 / slc6 / x86_64 - slc6 - gcc48 - opt / bin /../ lib / gcc / x86_64 - unknown - linux - gnu / 4.8.4 /../../../../ include / c++ / 4.8.4 / ext / hash_map:60:
/ cvmfs / atlas.cern.ch / repo / ATLASLocalRootBase / x86_64 / Gcc / gcc484_x86_64_slc6 / slc6 / x86_64 - slc6 - gcc48 - opt / bin /../ lib / gcc / x86_64 - unknown - linux - gnu / 4.8.4 /../../../../ include / c++ / 4.8.4 / backward / backward_warning.h:32:2: warning:
      This file includes at least one deprecated or antiquated header which may be removed without further notice at a future
      date.Please use a non - deprecated interface with equivalent functionality instead.For a listing of replacement headers
      and interfaces, consult the file backward_warning.h.To disable this warning use -Wno-deprecated. [-W#warnings]
#warning \
 ^
RootCore: Error failed to compile package DiVertAnalysis")
            )
            .AddQueuedChange("echo $?", "echo $?", "1");

            util.CatchException(() => s.BuildWorkArea(), typeof(LinuxCommandErrorException), "'trigTauTracks_IDTrig' was not declared in this scope");
        }

        [TestMethod]
        [Ignore] // Because this requires real remote and is slow.
        public void OnlineFullCheckoutAndBuild()
        {
            // Do it for real to make sure all the dummy test stuff we have above actually works.
            var info = util.GetUsernameAndPassword();
            var kinitInfo = util.GetPasswordForKint(info.Item1);
            using (var s = new SSHConnection(info.Item1, info.Item2))
            {
                s.ExecuteLinuxCommand("rm -rf /tmp/gwatts/buildtest"); // All releases must start from scratch!

                s.setupATLAS()
                    .Kinit(kinitInfo.Item1, kinitInfo.Item2)
                    .SetupRcRelease("/tmp/gwatts/buildtest", "Base,2.3.32")
                    .CheckoutPackage("atlasphys-exo/Physics/Exotic/UEH/DisplacedJets/Run2/AnalysisCode/trunk/DiVertAnalysis", "247550")
                    .BuildWorkArea();

                // Next get back a list of the executables, and make sure one we are looking for is actually there.

                var answers = new List<string>();
                s.ExecuteLinuxCommand("find ./RootCoreBin/bin -name DiVertAnalysisRunner -print", l => answers.Add(l));

                Assert.AreEqual(1, answers.Count);
                Assert.IsTrue(answers[0].Contains("DiVertAnalysisRunner"));
            }

        }
    }
}
