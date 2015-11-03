using CredentialManagement;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    static class util
    {
        /// <summary>
        /// Returns a username and password we can use for testing on this machine.
        /// </summary>
        /// <returns></returns>
        public static Tuple<string, string> GetUsernameAndPassword()
        {
            var cs = new CredentialSet("AtlasSSHTest");
            var thePasswordInfo = cs.Load().FirstOrDefault();
            if (thePasswordInfo == null)
            {
                throw new InvalidOperationException("Please create a generic windows password with the 'internet or network address' as 'AtlasSSHTest', and as a username the target node name, and a password that contains the username");
            }
            return Tuple.Create(thePasswordInfo.Username, thePasswordInfo.Password);
        }

        /// <summary>
        /// Create a temp generic windows credential. We do leave these behind on the machine.
        /// But they are just for testing, and hopefully the username used will make it clear
        /// that we don't care.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        internal static void SetPassword(string target, string username, string password)
        {
            var cs = new CredentialSet("GRID");
            var thePasswordInfo = cs.Load().Where(c => c.Username == username).FirstOrDefault();
            if (thePasswordInfo != null)
            {
                if (thePasswordInfo.Password == password)
                    return;
                thePasswordInfo.Delete();
            }

            var newC = new Credential(username, password, target);
            newC.Description = "Test credential for the AtlasSSH project. Delete at will!";
            newC.PersistanceType = PersistanceType.LocalComputer;
            newC.Save();
        }

        internal static Dictionary<string, string> AddEntry (this Dictionary<string, string> dict, string key, string val)
        {
            dict[key] = val;
            return dict;
        }

        /// <summary>
        /// Do setup for the rucio setup command
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        internal static Dictionary<string, string> AddsetupRucioResponses(this Dictionary<string, string> dict, string accountName)
        {
            dict["export RUCIO_ACCOUNT=" + accountName] = "";
            return dict
                .AddEntry("localSetupRucioClients", string.Format(@"************************************************************************
Requested:  rucio ...
 Setting up emi 3.14.0-1_v4c.sl6 ...
 Setting up rucio 1.0.1 ...
Info: Setting compatibility to slc6
Info: Set RUCIO_AUTH_TYPE to x509_proxy
Info: Set RUCIO_ACCOUNT to {0}
>>>>>>>>>>>>>>>>>>>>>>>>> Information for user <<<<<<<<<<<<<<<<<<<<<<<<<
 emi:
   No valid proxy present.  Type ""voms - proxy - init - voms atlas""
* ***********************************************************************
", accountName))
                .AddEntry("hash rucio", "");
        }

        internal static Dictionary<string, string> AddsetupATLASResponses(this Dictionary<string,string> dict)
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

            dict["setupATLAS"] = setupATLASGoodResponse;
            dict["alias"] = aliasResponse;

            return dict;
        }

        /// <summary>
        /// Setup everything for a voms proxy init commands and response.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public static Dictionary<string,string> AddsetupVomsProxyInit(this Dictionary<string,string> dict, string username)
        {
            // Setup a password!
            var sclist = new CredentialSet(string.Format("{0}@GRID", username));
            var passwordInfo = sclist.Load().Where(c => c.Username == username).FirstOrDefault();
            if (passwordInfo == null)
            {
                passwordInfo = new Credential(username, "badpass", string.Format("{0}@GRID", username));
                sclist.Add(passwordInfo);
                passwordInfo.Save();
            }

            return dict
                .AddEntry(string.Format("echo {0} | voms-proxy-init -voms atlas", passwordInfo.Password), @"Enter GRID pass phrase for this identity:
Contacting voms2.cern.ch:15001 [/DC=ch/DC=cern/OU=computers/CN=voms2.cern.ch] ""atlas""...
Remote VOMS server contacted succesfully.


Created proxy in / tmp / x509up_u1742.

Your proxy is valid until Fri Oct 23 08:56:05 PDT 2015
");
        }

        public static Dictionary<string, string> AddRucioListFiles(this Dictionary<string, string> dict, string dsname)
        {
            if (dsname == "user.gwatts:user.gwatts.301295.EVNT.1")
            {
                return dict
                    .AddEntry("rucio list-files user.gwatts:user.gwatts.301295.EVNT.1", @"+-------------------------------------------------------------------------------------------------+--------------------------------------+-------------+------------+----------+
| SCOPE:NAME                                                                                      | GUID                                 | ADLER32     |   FILESIZE |   EVENTS |
|-------------------------------------------------------------------------------------------------+--------------------------------------+-------------+------------+----------|
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000001.pool.root.1 | F5EB7282-ED9E-FF46-89A6-C116B0C32102 | ad:b67bc904 |  269294767 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000002.pool.root.1 | 01A94939-2DED-4049-BF2C-AC3F4712DCAC | ad:e64dfd09 |  269166581 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000003.pool.root.1 | 7AC93A6B-8466-F94E-B3CD-EEAFD9E13DDD | ad:845511a8 |  267773464 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000004.pool.root.1 | 39FF2CDD-1BB7-4E45-BB39-9B6ABC64B883 | ad:fb6019bc |  266137900 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000005.pool.root.1 | 416E0F2A-DC85-0B4E-8511-4F937048F90F | ad:2cc4200f |  272572969 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000006.pool.root.1 | 96476F7E-08D9-2847-BD08-CB35746FA79F | ad:4ba75760 |  270815952 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000007.pool.root.1 | 9E2C97D9-304C-6B4F-ABBD-C111F293C4EE | ad:bec7278c |  269030763 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000008.pool.root.1 | 77E6EBE6-9420-7A42-BB60-E46DD2DB4528 | ad:5410f73c |  268835854 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000009.pool.root.1 | 7E0B114B-D2EB-FF4D-BD01-BD477C26EA91 | ad:a023d16c |  270858128 |          |
| user.gwatts:MC15.301295.Pythia8EvtGen_AU2MSTW2008LO_HV_ggH_mH125_mVPI25.EVNT.000010.pool.root.1 | 64159E34-AB6D-DB4A-B41A-2A132FFA9803 | ad:92843e7f |  268988756 |          |
+-------------------------------------------------------------------------------------------------+--------------------------------------+-------------+------------+----------+
Total files : 10
Total size : 2693475134
");
            }
            else
            {
                Assert.IsTrue(false, "Unknown dataset: " + dsname);
                return null;
            }
        }

        public static Dictionary<string,string> AddRcSetup(this Dictionary<string,string> dict, string dirLocation, string releaseName)
        {
            return dict
                .AddEntry("mkdir " + dirLocation, "")
                .AddEntry("cd " + dirLocation, "")
                .AddEntry("rcSetup " + releaseName, string.Format(@"-bash-4.1$ 
Found ASG release with config=x86_64-slc6-gcc48-opt at
        /cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/{0}
Going to build a RootCore bin area= {1}/RootCoreBin
Good!! The already set ROOTSYS=/cvmfs/atlas.cern.ch/repo/ATLASLocalRootBase/x86_64/root/6.02.12-x86_64-slc6-gcc48-opt will be used", releaseName, dirLocation));
        }

        public static Dictionary<string, string> AddsetupKinit (this Dictionary<string, string> dict, string username, string password)
        {
            return dict
                .AddEntry(string.Format("echo {1} | kinit {0}", username, password), @"Password for gwatts@CERN.CH:");
        }

        public static Dictionary<string, string> AddCheckoutFromRelease(this Dictionary<string,string> dict, string packageName)
        {
            return dict
                .AddEntry(string.Format("rc checkout_pkg {0}", packageName), @"checking out xAODTrigger
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/test
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/test/ut_xaodtrigger_trigcomposite_test.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TriggerMenuContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigCompositeContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/MuonRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/JetRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/selection.xml
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/JetRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/BunchConf.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/EmTauRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigConfKeys.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/BunchConfContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TriggerMenuAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigCompositeAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/MuonRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigDecisionAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/JetRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/BunchConfKey.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/JetEtRoIAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigNavigationAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/EnergySumRoIAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TriggerMenu_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoIContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TriggerMenuContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoI_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigComposite_v1.icc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoIContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/ByteStreamAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigConfKeys_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoIAuxContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/BunchConfContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigCompositeAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/MuonRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigDecisionAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/BunchConfKey_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigNavigationAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetEtRoIAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigDecision_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigNavigation_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetEtRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EmTauRoI_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/BunchConfAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EnergySumRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/MuonRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigComposite_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/MuonRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TrigCompositeContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/BunchConf_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/ByteStreamAuxContainer_v1.icc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/TriggerMenuAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/JetRoIAuxContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/versions/EnergySumRoIAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigDecision.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/xAODTriggerDict.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/JetEtRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/EmTauRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigNavigation.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/BunchConfAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/MuonRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/EnergySumRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/EmTauRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TrigComposite.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/xAODTrigger/TriggerMenu.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TriggerMenuAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigCompositeAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/MuonRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/dict
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/dict/ContainerProxies.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigDecisionAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/BunchConfKey_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetRoIAuxContainer_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/compileVersionless.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigNavigationAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetEtRoIAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EnergySumRoIAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigDecision_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigNavigation_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EmTauRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetEtRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EmTauRoI_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/BunchConfAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EnergySumRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/MuonRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigComposite_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TriggerMenu_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/xAODTriggerCLIDs.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/JetRoI_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/ByteStreamAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/BunchConf_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EmTauRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/EmTauRoIAuxContainer_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/Root/TrigConfKeys_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/cmt
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/cmt/requirements
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/cmt/Makefile.RootCore
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/doc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/doc/mainpage.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger/ChangeLog
 U   /home/gwatts/atlas/trigger/newEDMObjectsRootCore/xAODTrigger
Checked out revision 704382.");
        }

        public static Dictionary<string,string> AddCheckoutFromRevision(this Dictionary<string, string> dict, string packagePath, string revision)
        {
            return dict
                .AddEntry(string.Format("rc checkout_pkg {0}/trunk@{1}", packagePath, revision), @"checking out trunk@704382
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/test
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/test/ut_xaodtrigger_trigcomposite_test.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/test/ut_xaodtrigger_trigpassbits_test.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TriggerMenuContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/selection.xml
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/JetRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigConfKeys.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigCompositeAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/MuonRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigDecisionAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigPassBits.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigNavigationAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/JetEtRoIAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/EnergySumRoIAuxInfo.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TriggerMenu_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoIContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TriggerMenuContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoI_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigComposite_v1.icc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoIContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/ByteStreamAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigConfKeys_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoIAuxContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/BunchConfContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigCompositeAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/MuonRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigDecisionAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/BunchConfKey_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigNavigationAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetEtRoIAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigDecision_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigPassBits_v1.icc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigNavigation_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetEtRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EmTauRoI_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/BunchConfAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EnergySumRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/MuonRoI_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigComposite_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/MuonRoIContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigCompositeContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigPassBitsAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/BunchConf_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/ByteStreamAuxContainer_v1.icc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TriggerMenuAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoIAuxContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/JetRoIAuxContainer_v2.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigPassBits_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/TrigPassBitsContainer_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/versions/EnergySumRoIAuxInfo_v1.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigDecision.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigNavigation.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/EmTauRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/JetEtRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/EmTauRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TriggerMenu.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/MuonRoIContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigCompositeContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/JetRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigPassBitsAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/BunchConf.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/EmTauRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/BunchConfContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TriggerMenuAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/JetRoIAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/BunchConfKey.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigPassBitsContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/xAODTriggerDict.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/BunchConfAuxContainer.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/TrigComposite.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/EnergySumRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/xAODTrigger/MuonRoI.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigCompositeAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/MuonRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/dict
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/dict/ContainerProxies.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigDecisionAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/BunchConfKey_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/compileVersionless.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigNavigationAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetEtRoIAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigDecision_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigNavigation_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EmTauRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetEtRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/BunchConfAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EmTauRoI_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EnergySumRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/MuonRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/xAODTriggerCLIDs.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigComposite_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigPassBitsAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/BunchConf_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TriggerMenuAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetRoIAuxContainer_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigPassBits_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EnergySumRoIAuxInfo_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TriggerMenu_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetRoI_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/JetRoI_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/ByteStreamAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EmTauRoIAuxContainer_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/EmTauRoIAuxContainer_v2.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/Root/TrigConfKeys_v1.cxx
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/cmt
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/cmt/requirements
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/cmt/Makefile.RootCore
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/doc
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/doc/mainpage.h
A    /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382/ChangeLog
 U   /home/gwatts/atlas/trigger/newEDMObjectsRootCore/trunk@704382
Checked out revision 704382.")
                .AddEntry(string.Format("mv trunk@{0} {1}", revision, packagePath.Split('/').Last()), "")
                ;
        }

        /// <summary>
        /// When a linux command runs and returns something good - the status is good. Add that
        /// command and response to test harness.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AddGoodLinuxCommand(this Dictionary<string, string> dict)
        {
            return dict
                .AddEntry("echo $?", "0");
        }

        public static Dictionary<string, string> AddBuildCommandResponses(this Dictionary<string, string> dict)
        {
            return dict
                .AddEntry("rc find_packages", @"using release set with set_release                                                                                              
looking for packages in /phys/users/gwatts/bogus                                                                                

packages found:
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ApplyJetCalibration
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AsgExampleTools    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AsgTools           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Boost          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Doxygen        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Eigen          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_FastJet        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_RooUnfold      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Test           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_root           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AssociationUtils   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthContainers      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthContainersInterfaces
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthLinks               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CPAnalysisExamples     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CalibrationDataInterface
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CaloGeoHelpers          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CxxUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/DiTauMassTools          
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronEfficiencyCorrection
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonFourMomentumCorrection
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonSelectorTools         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonShowerShapeFudgeTool  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoop                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoopAlgs                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoopGrid                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventPrimitives                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventShapeInterface                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/FourMomUtils                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/FsrUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/GeoPrimitives                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/GoodRunsLists                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/InDetTrackSelectionTool             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/IsolationCorrections                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/IsolationSelection                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetCPInterfaces                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetCalibTools                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetEDM                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetMomentTools                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetRec                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetResolution                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSelectorTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSubStructureMomentTools          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSubStructureUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetUncertainties                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MCTruthClassifier                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/METInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/METUtilities                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MultiDraw                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonEfficiencyCorrections           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonIdHelpers                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonMomentumCorrections             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonSelectorTools                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PATCore                             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PATInterfaces                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PathResolver                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PhotonEfficiencyCorrection          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PileupReweighting                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/QuickAna                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ReweightUtils                       
/phys/users/gwatts/bogus/RootCore                                                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/RootCoreUtils                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/SampleHandler                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/SemileptonicCorr                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TauAnalysisTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TauCorrUncert                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TestTools                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrackVertexAssociationTool          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigAnalysisInterfaces              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigBunchCrossingTool               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfBase                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfHLTData                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfInterfaces                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfL1Data                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfxAOD                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigDecisionInterface               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigDecisionTool                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigEgammaMatchingTool              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigMuonEfficiency                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigMuonMatching                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigNavStructure                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigSteeringEvent                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigTauMatching                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TruthUtils                          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ZMassConstraint                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/egammaLayerRecalibTool              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/egammaMVACalib                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODAssociations                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBPhys                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBTagging                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBTaggingEfficiency              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBase                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCaloEvent                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCore                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCutFlow                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEgamma                          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventFormat                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventInfo                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventShape                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODHIEvent                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODJet                             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODLuminosity                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMetaData                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMetaDataCnv                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMissingET                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMuon                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODPFlow                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODParticleEvent                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODPrimitives                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODRootAccess                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODRootAccessInterfaces            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTau                             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTracking                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigBphys                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigCalo                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigEgamma                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigL1Calo                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMinBias                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMissingET                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMuon                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigRinger                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigger                         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTriggerCnv                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTruth                           

sorted packages:
/phys/users/gwatts/bogus/RootCore
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Doxygen
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Test   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_root   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ApplyJetCalibration
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODRootAccessInterfaces
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Boost               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TestTools               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CxxUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthContainersInterfaces 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthLinks                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AthContainers           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCore                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventFormat         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODRootAccess          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AsgTools                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AsgExampleTools         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_Eigen               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_FastJet             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/Asg_RooUnfold           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBase                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CaloGeoHelpers          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventPrimitives         
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/GeoPrimitives           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCaloEvent           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTracking            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigger             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBTagging            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODPFlow               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODJet                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMissingET           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/FourMomUtils            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventInfo           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TruthUtils              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTruth               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODPrimitives          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEgamma              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonIdHelpers           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMuon                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTau                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/AssociationUtils        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/RootCoreUtils           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/SampleHandler           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoop               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PATCore                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PATInterfaces           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PathResolver            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonEfficiencyCorrections
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonSelectorTools        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MuonMomentumCorrections  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonSelectorTools
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/egammaLayerRecalibTool     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/egammaMVACalib             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonFourMomentumCorrection
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TauAnalysisTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronEfficiencyCorrection        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CalibrationDataInterface            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODEventShape                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetCalibTools                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSelectorTools                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetResolution                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetCPInterfaces                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetUncertainties                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/METInterface                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/InDetTrackSelectionTool             
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/METUtilities                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBTaggingEfficiency              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/CPAnalysisExamples                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/DiTauMassTools                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigNavStructure                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfBase                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfL1Data                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfHLTData                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfInterfaces                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigConfxAOD                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigDecisionInterface               
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigSteeringEvent                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigDecisionTool                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigCalo                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigEgamma                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/GoodRunsLists                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMuon                        
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigMuonMatching                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigEgammaMatchingTool              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoopGrid                       
/phys/users/gwatts/bogus/trunk@246773/DiVertAnalysis                                     
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ElectronPhotonShowerShapeFudgeTool  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MultiDraw                           
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventLoopAlgs                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/EventShapeInterface                 
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/IsolationSelection                  
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/FsrUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetEDM                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetRec                              
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/IsolationCorrections                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetUtils                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetMomentTools                      
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSubStructureUtils                
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/JetSubStructureMomentTools          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/MCTruthClassifier                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PhotonEfficiencyCorrection          
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODParticleEvent                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ReweightUtils                       
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/PileupReweighting                   
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/QuickAna                            
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/SemileptonicCorr                    
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TauCorrUncert
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrackVertexAssociationTool
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigAnalysisInterfaces
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigBunchCrossingTool
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigMuonEfficiency
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/TrigTauMatching
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/ZMassConstraint
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODAssociations
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODBPhys
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODCutFlow
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODHIEvent
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODLuminosity
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMetaData
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODMetaDataCnv
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigBphys
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigL1Calo
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMinBias
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigMissingET
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTrigRinger
/cvmfs/atlas.cern.ch/repo/sw/ASG/AnalysisBase/2.3.30/xAODTriggerCnv

writing changes
done")
            .AddEntry("echo $?", "0")
            .AddEntry("rc compile", @"bash-4.1$ rc compile
compiling RootCore  
finished compiling RootCore
compiling DiVertAnalysis   
Making directory /phys/users/gwatts/bogus/RootCoreBin/obj/x86_64-slc6-gcc48-opt/DiVertAnalysis/obj
Making dependency for LinkDef.h                                                                   
Making dependency for Common.cxx                                                                  
Making dependency for CalRatioEmul.cxx                                                            
Making dependency for DiVertUtils.cxx                                                             
Making dependency for CalRatioNoTrig.cxx                                                          
Making dependency for DiLeptonFinder.cxx                                                          
Making dependency for LinkDef.h                                                                   
Making dependency for CalRatioTrig.cxx                                                            
Making dependency for DiVertAnalysisLocalFileRunner.cxx                                           
Making dependency for DiVertAnalysis.cxx                                                          
Making dependency for TrigUtils.cxx                                                               
Making dependency for MuRoIClusTrig.cxx                                                           
Making dependency for TruthUtils.cxx                                                              
Making dependency for PrepObjects.cxx                                                             
Making dependency for DiVertAnalysisRunner.cxx                                                    
Making dependency for MSVertexFinder.cxx                                                          
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
Compiling Common.o                                                                                
Compiling MuRoIClusTrig.o                                                                         
Compiling DiVertAnalysisCINT.o                                                                    
Making directory /phys/users/gwatts/bogus/RootCoreBin/obj/x86_64-slc6-gcc48-opt/DiVertAnalysis/bin
Compiling DiVertAnalysisLocalFileRunner.o                                                         
Compiling DiVertAnalysisRunner.o                                                                  
In file included from input_line_11:1:                                                            
In file included from /phys/users/gwatts/bogus/DiVertAnalysis/Root/LinkDef.h:1:                   
In file included from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/DiVertAnalysis.h:7:  
In file included from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/DiVertUtils.h:3:     
In file included from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/Common.h:42:         
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
/phys/users/gwatts/bogus/DiVertAnalysis/Root/PrepObjects.cxx:555:8: warning: unused parameter 'isLLPMC' [-Wunused-parameter]    
   void addEventBranches(TTree* tree, struct eventBranches* branches, bool isLate, bool isLLPMC) {                              
        ^                                                                                                                       
/phys/users/gwatts/bogus/DiVertAnalysis/Root/DiVertUtils.cxx:5:8: warning: unused parameter 'isMC' [-Wunused-parameter]         
   void fillEventInfo(struct eventBranches* branches, int eventCount, double eventWeight, const xAOD::EventInfo* eventInfo, std::map<std::string, bool> trigPassed, bool isMC, bool isLate) {                                                                   
        ^                                                                                                                       
/phys/users/gwatts/bogus/DiVertAnalysis/Root/DiVertUtils.cxx:284:8: warning: unused parameter 'CRNT' [-Wunused-parameter]       
   void setCRJetMatch(struct jetBranches* branches, CalRatioTrig* CR, CalRatioNoTrig* CRNT){                                    
        ^                                                                                                                       
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:26:0,                    
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroup.h:27,                            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroupFunctions.h:19,                   
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolCore.h:19,                  
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolStandalone.h:40,            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionTool.h:18,                      
                 from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/Common.h:42,                                       
                 from /phys/users/gwatts/bogus/DiVertAnalysis/Root/Common.cxx:1:                                                
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::Jet_v1>]':                                                                                                    
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::Jet_v1>; STORED = DataVector<xAOD::Jet_v1>; CONTAINER = DataVector<xAOD::Jet_v1>; EventPtr_t = asg::SgTEvent*]'                                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::Jet_v1>; std::string = std::basic_string<char>]'                                                                         
/phys/users/gwatts/bogus/DiVertAnalysis/Root/Common.cxx:188:79:   required from here                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
     std::shared_ptr<const STORED> filter_if(boost::mpl::bool_<true> is_same, std::shared_ptr<const STORED>& original,const TrigPassBits* bits){                                                                                                                
                                   ^                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::TrackParticle_v1>]':                                                                                          
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::TrackParticle_v1>; STORED = DataVector<xAOD::TrackParticle_v1>; CONTAINER = DataVector<xAOD::TrackParticle_v1>; EventPtr_t = asg::SgTEvent*]'                                                                              
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::TrackParticle_v1>; std::string = std::basic_string<char>]'                                                               
/phys/users/gwatts/bogus/DiVertAnalysis/Root/Common.cxx:189:129:   required from here                                           
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
/phys/users/gwatts/bogus/DiVertAnalysis/Root/DiVertAnalysis.cxx: In member function 'virtual EL::StatusCode DiVertAnalysis::execute()':                                                                                                                         
/phys/users/gwatts/bogus/DiVertAnalysis/Root/DiVertAnalysis.cxx:318:14: warning: unused variable 'jet_et' [-Wunused-variable]   
       double jet_et = (**aJet).p4().Et()*0.001;                                                                                
              ^                                                                                                                 
In file included from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:26:0,                    
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroup.h:27,                            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/ChainGroupFunctions.h:19,                   
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolCore.h:19,                  
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionToolStandalone.h:40,            
                 from /phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/TrigDecisionTool.h:18,                      
                 from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/Common.h:42,                                       
                 from /phys/users/gwatts/bogus/DiVertAnalysis/DiVertAnalysis/TrigUtils.h:4,                                     
                 from /phys/users/gwatts/bogus/DiVertAnalysis/Root/TrigUtils.cxx:1:                                             
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::Jet_v1>]':                                                                                                    
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::Jet_v1>; STORED = DataVector<xAOD::Jet_v1>; CONTAINER = DataVector<xAOD::Jet_v1>; EventPtr_t = asg::SgTEvent*]'                                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::Jet_v1>; std::string = std::basic_string<char>]'                                                                         
/phys/users/gwatts/bogus/DiVertAnalysis/Root/TrigUtils.cxx:42:83:   required from here                                          
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
     std::shared_ptr<const STORED> filter_if(boost::mpl::bool_<true> is_same, std::shared_ptr<const STORED>& original,const TrigPassBits* bits){                                                                                                                
                                   ^                                                                                            
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h: In instantiation of 'std::shared_ptr<const _Tp> Trig::FeatureAccessImpl::filter_if(mpl_::bool_<true>, std::shared_ptr<const _Tp>&, const TrigPassBits*) [with STORED = DataVector<xAOD::TrackParticle_v1>]':                                                                                          
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:140:69:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureAccessImpl::typedGet(const std::vector<Trig::TypelessFeature>&, HLT::TrigNavStructure*, EventPtr_t) [with REQUESTED = DataVector<xAOD::TrackParticle_v1>; STORED = DataVector<xAOD::TrackParticle_v1>; CONTAINER = DataVector<xAOD::TrackParticle_v1>; EventPtr_t = asg::SgTEvent*]'                                                                              
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureContainer.h:74:109:   required from 'std::vector<Trig::Feature<T> > Trig::FeatureContainer::containerFeature(const string&, unsigned int, const string&) const [with CONTAINER = DataVector<xAOD::TrackParticle_v1>; std::string = std::basic_string<char>]'                                                               
/phys/users/gwatts/bogus/DiVertAnalysis/Root/TrigUtils.cxx:43:126:   required from here                                         
/phys/users/gwatts/bogus/RootCoreBin/include/TrigDecisionTool/FeatureCollectStandalone.h:86:35: warning: unused parameter 'is_same' [-Wunused-parameter]                                                                                                        
cc1plus: warning: unrecognized command line option "" - Wno - tautological - undefined - compare"" [enabled by default]                   
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
At global scope:
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
In file included from DiVertAnalysisCINT.cxx.h:1:                                                                               
In file included from / phys / users / gwatts / bogus / DiVertAnalysis / DiVertAnalysis / DiVertAnalysis.h:7:                                
In file included from / phys / users / gwatts / bogus / DiVertAnalysis / DiVertAnalysis / DiVertUtils.h:3:                                   
In file included from / phys / users / gwatts / bogus / DiVertAnalysis / DiVertAnalysis / Common.h:42:                                       
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
cc1plus: warning: unrecognized command line option ""-Wno-tautological-undefined-compare""[enabled by default]
Linking libDiVertAnalysis.so
Linking DiVertAnalysisRunner
Linking DiVertAnalysisLocalFileRunner
finished compiling DiVertAnalysis
root[0]
Processing / cvmfs / atlas.cern.ch / repo / sw / ASG / AnalysisBase / 2.3.32 / RootCore / scripts / load_packages.C...
xAOD::Init                INFO    Environment initialised for data access
");
        }

        [Serializable]
        public class TestAssertException : Exception
        {
            public TestAssertException() { }
            public TestAssertException(string message) : base(message) { }
            public TestAssertException(string message, Exception inner) : base(message, inner) { }
            protected TestAssertException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context)
            { }
        }

        /// <summary>
        /// Make sure an error occurs
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expr"></param>
        /// <param name="expressionType"></param>
        /// <param name="expectedMessage"></param>
        public static void CatchException<T>(this Func<T> expr, Type expressionType, string expectedMessage)
        {
            Exception whatIGot = null;
            try
            {
                expr();
            } catch (Exception e)
            {
                whatIGot = e;

                // If it is the wrong type of exception - then we want to let it go by as if
                // this wasn't out problem.
                if (expressionType != whatIGot.GetType())
                    throw new TestAssertException(string.Format("Was expecting exception of type '{0}', got one of type '{1}'.",expressionType.Name, whatIGot.GetType().Name) , e);

                // This is trikier. Lets assume this is the proper type, so we want to throw a real error if they aren't equal.
                if (!string.IsNullOrEmpty(expectedMessage))
                {
                    if (!whatIGot.Message.Contains(expectedMessage))
                        throw new TestAssertException(string.Format("Exception message ({0}) did not contain expected text ({1}).", whatIGot.Message, expectedMessage), e);
                }
            }

            Assert.IsNotNull(whatIGot, "Did not catch any exception!");
        }
    }
}
