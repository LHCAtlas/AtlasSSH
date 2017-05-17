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
        public static Tuple<string, string> GetUsernameAndPassword(string credentialKey = "AtlasSSHTest")
        {
            var cs = new CredentialSet(credentialKey);
            var thePasswordInfo = cs.Load().FirstOrDefault();
            if (thePasswordInfo == null)
            {
                throw new InvalidOperationException($"Please create a generic windows password with the 'internet or network address' as '{credentialKey}', and as a username the target node name, and a password that contains the username");
            }
            return Tuple.Create(thePasswordInfo.Username, thePasswordInfo.Password);
        }

        public static Tuple<string, string> GetPasswordForKint(string username)
        {
            var cs = new CredentialSet(username);
            var thePasswordInfo = cs.Load().FirstOrDefault();
            if (thePasswordInfo == null)
            {
                throw new InvalidOperationException(string.Format("Please create a generic windows password with the 'internet or network address' as '{0}', and as a username the target node name, and a password that contains the username", username));
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
                .AddEntry("lsetup rucio", string.Format(@"************************************************************************
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
            else if (dsname == "mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
            {
                return dict
                    .AddEntry("rucio list-files mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282",
                    @"+---------------------------------------------+--------------------------------------+-------------+------------+----------+
| SCOPE:NAME                                  | GUID                                 | ADLER32     | FILESIZE   |   EVENTS |
|---------------------------------------------+--------------------------------------+-------------+------------+----------|
| mc15_13TeV:AOD.07483884._000001.pool.root.1 | D9F3F7A4-352B-1A4A-8547-5C3544D11FE5 | ad:753349a4 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000002.pool.root.1 | 06537715-8719-6C45-BB57-28D129B8A769 | ad:2786b7f7 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000003.pool.root.1 | 306775A9-EE22-F549-95DE-9584CAF3FB9D | ad:2d9d8c63 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000004.pool.root.1 | A5F03A54-975F-EC4C-B271-1868D40FB662 | ad:827317d0 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000005.pool.root.1 | 287E5D10-DDE0-3643-9088-F1182B2144C4 | ad:8b7e6c4f | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000006.pool.root.1 | 55A558B1-D02B-A648-9B80-02AC9933A9AB | ad:75b1fb76 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000007.pool.root.1 | B7D7D8DF-D638-2646-964B-C8955F0B1352 | ad:2b1b278e | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000008.pool.root.1 | FFAD6EC3-9A51-1D47-9AC8-15AC32E91949 | ad:23b669e7 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000009.pool.root.1 | 2AD25562-5FB1-0540-B0C6-7E4F1F8C21AA | ad:fbf827eb | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000010.pool.root.1 | 10993A2F-4637-F748-A964-513B31729FF1 | ad:f84527ff | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000011.pool.root.1 | 68DD962E-E498-014E-B115-CCE9E9740F47 | ad:689d3658 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000012.pool.root.1 | 83F703DB-E5A0-3C4B-A6F6-3CA1A359D47F | ad:b2c9284f | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000013.pool.root.1 | 6B830295-9514-C643-993F-ABB94A5C5F16 | ad:b053a9dc | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000014.pool.root.1 | 38FBDD38-D439-A64E-9E52-89093D3E8928 | ad:5678d3ee | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000015.pool.root.1 | 74B8B748-3A33-2F40-A5F4-AEFCE0D3256C | ad:0f663851 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000016.pool.root.1 | 9F06C466-71BC-1B4E-8793-426F14F44B57 | ad:3c2ca059 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000017.pool.root.1 | F278E911-6851-3C42-BFB4-9E0794437EA3 | ad:befc131a | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000018.pool.root.1 | EA2D1803-A70D-5249-A4C9-8D98095C2F4A | ad:06431aac | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000019.pool.root.1 | FE25E88E-AC1E-2843-BA66-62F81EE9F9DF | ad:6f50757a | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000020.pool.root.1 | AD608786-A470-054B-89DB-85689D612253 | ad:a669a5fe | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000021.pool.root.1 | B209EE55-BAA3-074F-9307-C93334059A1B | ad:c86cdbb3 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000022.pool.root.1 | 2C98061E-74C2-0A47-9637-6D074DADB7C9 | ad:85cc7fbc | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000023.pool.root.1 | AC09A00D-BEA1-B841-81AA-0AD9A647605E | ad:333286ee | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000024.pool.root.1 | 0B837A30-9ABB-684D-8E30-AB5BA09FDCE7 | ad:77bf8f33 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000025.pool.root.1 | 974F8345-BC2E-E84E-9390-5C5DE4BE9AD2 | ad:504e04fe | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000026.pool.root.1 | FC513715-B597-2B41-A977-99EBA8E26A97 | ad:3fc9c3e1 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000027.pool.root.1 | 1A2A3874-A3FD-0B47-BA84-4EAC357F577C | ad:19b3c548 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000028.pool.root.1 | 7705072C-4272-B642-B37D-6701E0F92613 | ad:acbc3ced | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000029.pool.root.1 | 71A06BAB-08A2-1A40-AF29-B1509885333A | ad:1a37801a | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000030.pool.root.1 | 8523FB3F-31BA-904F-B1EE-7FC2F2D3AE39 | ad:550f08ec | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000031.pool.root.1 | 2DFEAB42-AD16-7D46-B124-71DF119B01A1 | ad:8bfd2c81 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000033.pool.root.1 | 01BD2B71-E010-384B-920C-EA30225026EC | ad:aa1770af | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000036.pool.root.1 | 88D70585-ADDC-C242-942A-13A03842ABB7 | ad:0400ad71 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000037.pool.root.1 | 7FB4DCD9-D90F-2C47-9E6B-82C58C6AE31D | ad:b7332d60 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000038.pool.root.1 | C59DE348-8494-284E-82B5-E3127E3E9D42 | ad:7026e741 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000039.pool.root.1 | C457114E-B9CB-C74C-AD32-05ED9625F5B7 | ad:f06dc531 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000040.pool.root.1 | 2ED9F860-9ABE-AD4A-81F6-733DB6C36D71 | ad:c688e9e4 | 2.2 GB     |     5000 |
| mc15_13TeV:AOD.07483884._000041.pool.root.1 | 1C4DE59E-28B3-1A4E-9441-3B67D806FB46 | ad:22030cb4 | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000042.pool.root.1 | 95587D17-0256-824C-8D2D-7D83804F6539 | ad:21c12fff | 4.4 GB     |    10000 |
| mc15_13TeV:AOD.07483884._000043.pool.root.1 | 868E3A37-C74C-8C47-ACCA-776ACDC55721 | ad:b78a21ae | 4.4 GB     |    10000 |
+---------------------------------------------+--------------------------------------+-------------+------------+----------+
Total files : 40
Total size : 173359640796
Total events : 395000
")
.AddEntry("rucio ls mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282",
@"-bash-4.1$ rucio ls mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282
+ ------------------------------------------------------------------------------------------------------------------------------------------+--------------+
| SCOPE:NAME | [DID TYPE] |
| ------------------------------------------------------------------------------------------------------------------------------------------+--------------|
| mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282_tid07787075_00 | DATASET |
| mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 | CONTAINER |
+------------------------------------------------------------------------------------------------------------------------------------------+--------------+
");
            }
            else if (dsname == "user.gwatts.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.DAOD_EXOT15.r6765_r6282_p2452.DiVertAnalysis_v4_539A3CCD_hist")
            {
                return dict
                    .AddEntry("rucio list-files user.gwatts.361023.Pythia8EvtGen_A14NNPDF23LO_jetjet_JZ3W.DAOD_EXOT15.r6765_r6282_p2452.DiVertAnalysis_v4_539A3CCD_hist",
                    @"+---------------------------------------------+--------------------------------------+-------------+------------+----------+
| SCOPE:NAME                                  | GUID                                 | ADLER32     | FILESIZE   |   EVENTS |
|---------------------------------------------+--------------------------------------+-------------+------------+----------|
+---------------------------------------------+--------------------------------------+-------------+------------+----------+
Total files : 0
Total size : 0
");
            }
            else if (dsname == "data15_13TeV:data15_13TeV.00266904.physics_Main.merge.DAOD_EXOT15.r7600_p2521_p2950")
            {
                return dict
                    .AddEntry("rucio list-files data15_13TeV:data15_13TeV.00266904.physics_Main.merge.DAOD_EXOT15.r7600_p2521_p2950",
                    @"+-------------------------------------------------------+--------------------------------------+-------------+------------+----------+
| SCOPE:NAME                                            | GUID                                 | ADLER32     | FILESIZE   |   EVENTS |
|-------------------------------------------------------+--------------------------------------+-------------+------------+----------|
| data15_13TeV:DAOD_EXOT15.10330628._000001.pool.root.1 | F291C821-1031-E640-BA70-D80B77878928 | ad:dd85903b | 151.454 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000002.pool.root.1 | 044CDC11-D8CB-A241-A477-35BCF63357C1 | ad:47de4407 | 151.691 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000003.pool.root.1 | 6607997B-44FB-5341-9759-F61EF637BBBC | ad:8ce12f6e | 151.693 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000004.pool.root.1 | 2BD06214-E455-A541-BCB9-12D1161044EE | ad:b7083389 | 151.698 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000006.pool.root.1 | 094AC801-C8EB-C041-AEAD-5F85F48287EB | ad:bce8248b | 151.702 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000007.pool.root.1 | B642C377-B481-8F4B-A1FA-06A5D59BD78C | ad:c374d065 | 151.626 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000008.pool.root.1 | 1A907807-0C28-0447-902D-E50EB51F46D7 | ad:73538688 | 151.651 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000009.pool.root.1 | 966AEBC7-7E55-9241-90B5-1044FD25EDFB | ad:ea2fcbba | 151.733 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000010.pool.root.1 | F8BBFB57-7545-7644-8C28-45D589EFBFFF | ad:f4b5af7a | 151.715 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000066.pool.root.1 | 40EEC809-30A8-034B-BD97-6CB0F6A3B264 | ad:dfbd1017 | 178.726 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000080.pool.root.1 | C10C445B-D81D-8044-9580-00D84282DBBF | ad:903b76ef | 222.350 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000132.pool.root.1 | E12A924A-060F-574E-BEE4-AC7829C6130A | ad:356ec5b6 | 147.918 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000154.pool.root.1 | 2B2DCA77-D5B4-4742-968E-7DD6B17D0052 | ad:2a449def | 147.699 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000302.pool.root.1 | D1DB07F0-258D-2746-956F-1E143721A7CD | ad:8bc25258 | 198.000 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000313.pool.root.1 | 68011685-0799-2844-A876-A00CC869D1B0 | ad:f05f2f69 | 224.023 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000741.pool.root.1 | 19DB66C6-504F-A449-9E92-4BF0A99C7D9C | ad:64bd77e8 | 715.939 kB |        5 |
| data15_13TeV:DAOD_EXOT15.10330628._000743.pool.root.1 | 3B408EED-92B2-5649-AFE2-E0FAAD678DA7 | ad:47ce3ab4 | 810.279 kB |        4 |
| data15_13TeV:DAOD_EXOT15.10330628._000744.pool.root.1 | D7297343-9690-BF40-8AB9-A3098674C7FC | ad:b9dbe3f0 | 147.906 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._000790.pool.root.1 | 58FFD9A1-2558-2D4B-A81A-24DC0787C499 | ad:f4cda9a9 | 769.410 kB |        5 |
| data15_13TeV:DAOD_EXOT15.10330628._000977.pool.root.1 | D1F7C838-0212-F34B-9471-7090F096BE9D | ad:e6d7b40d | 147.501 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001443.pool.root.1 | B3AD9455-963A-E147-B3DE-0F43F96628FA | ad:b06ecea7 | 147.948 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001801.pool.root.1 | 44E56977-4836-9F46-8BA3-A702D64CF529 | ad:651748db | 722.236 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001802.pool.root.1 | 5FE2EF99-0C6E-0648-8D66-54BC8D74E4A9 | ad:3de680dc | 506.113 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001803.pool.root.1 | 58AC4E17-CB08-BC49-8954-28B552584721 | ad:dc3997d3 | 508.393 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001804.pool.root.1 | 900C31CC-4D1B-D746-A1FD-BF9A9DAC9940 | ad:6b639391 | 556.147 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001805.pool.root.1 | BC0D91EE-1FA3-0B45-8EF3-4AF748E9DD74 | ad:a75bb6e3 | 611.337 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001806.pool.root.1 | F5244D2A-0560-194E-AA47-7CD770BB6A77 | ad:fd0ea80c | 505.786 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001807.pool.root.1 | 017C9A5B-24C5-6D49-BBB1-DCB503AE38AD | ad:8e1d7b93 | 519.092 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001808.pool.root.1 | B4047F39-74B1-724E-9549-5EB72C13DBC8 | ad:691cd94d | 169.010 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001809.pool.root.1 | D9C56193-30DC-BB49-B049-39F04A3293E5 | ad:9c9b5e32 | 502.759 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001810.pool.root.1 | 69DBFB79-EFA1-7145-B1A9-AA2FB974050E | ad:f59711f9 | 474.201 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001811.pool.root.1 | D5FCF4A9-3BEA-D84F-BAE3-FC944218D42E | ad:3996b136 | 537.393 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001812.pool.root.1 | 4838AAA9-AC14-CE4E-B825-B4C732A1F1D7 | ad:f399481d | 534.313 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001813.pool.root.1 | FCBA3CDE-B845-5F42-8FE9-F3897DB73CF1 | ad:502770bc | 588.601 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001814.pool.root.1 | BF75CB4A-BDA6-8E4E-8F3F-00D0CFB8E8C7 | ad:e47e78ac | 537.017 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001815.pool.root.1 | DAE9A70B-F749-CA4D-9500-5A2CC5F583DC | ad:a833e579 | 490.222 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001816.pool.root.1 | 432B3C4A-266D-044E-841A-2EB5A6AC2338 | ad:e347c2cb | 523.856 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001817.pool.root.1 | 2B806432-9B2B-AD41-9050-2790393FF3A2 | ad:9c9351cd | 147.881 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001818.pool.root.1 | 81EDE583-3CCC-CA42-8D4C-BCFB18F84BCF | ad:76cef84d | 147.936 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001819.pool.root.1 | F5AA9284-C4AA-FE4D-A438-4C45AB0F1A7D | ad:3eb490d9 | 569.479 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001820.pool.root.1 | 154EF964-B081-5E44-84FC-FD0C096FD46E | ad:8dd0f7c7 | 507.483 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001821.pool.root.1 | DF3C9FF6-982A-994A-8328-4DCB532608D0 | ad:119c926e | 565.879 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001822.pool.root.1 | 5443BBCA-2A46-0E46-97B9-69C46AF7ADDE | ad:83a69ab4 | 435.004 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001823.pool.root.1 | EAA1EA63-2B15-4840-8B1B-FDAE5441ABC4 | ad:8f1a061f | 480.876 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001824.pool.root.1 | 25297A88-F8A6-9E43-B42E-F1D40E637561 | ad:9442afae | 482.788 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001825.pool.root.1 | 6C1F6322-0AEC-E145-AD7E-A3DA99B4FBF4 | ad:be1f99c9 | 651.509 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001826.pool.root.1 | 0E83EC66-48BF-9C4F-9420-B34EC97AA97C | ad:279f4f57 | 575.301 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001827.pool.root.1 | 1A44C2C6-B10B-6546-A528-3F3E41E7E602 | ad:d9bbf7e9 | 529.613 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001828.pool.root.1 | BF9D8817-F087-0B41-B097-8C30697E5B03 | ad:faa0114b | 176.264 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001867.pool.root.1 | 5BAF4762-E70A-934D-8AA7-8342C15EC984 | ad:ff072a97 | 147.479 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001868.pool.root.1 | 49603A8A-F167-9046-B711-768738B8F573 | ad:f7dd1d21 | 495.998 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001869.pool.root.1 | 96E8DF47-79D5-CA4D-BB46-266D15F93AE7 | ad:20417a05 | 176.643 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001870.pool.root.1 | F050DA0C-AB6A-5D4D-88F7-9EC8E1A67BD0 | ad:c6ff24e7 | 176.236 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001871.pool.root.1 | A51A6C93-E1D4-9E41-AAC4-F08474055783 | ad:286a1ea6 | 506.867 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001872.pool.root.1 | 3E501919-C301-AB4C-81C6-0BB2DB2210AB | ad:7c2732d9 | 176.239 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001873.pool.root.1 | 56DEDA7D-4A15-CE4F-A44E-B5CE66E0E39B | ad:db49d70b | 147.534 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001874.pool.root.1 | CC91144A-0C5B-CA46-8DE0-F7C799DA808E | ad:fa67b98f | 614.392 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001875.pool.root.1 | 7FB0F20A-1133-EE42-80BF-487673E2EE56 | ad:ed08e950 | 147.835 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001876.pool.root.1 | 88A809A3-31EA-FE49-B8C8-579DC8A1B369 | ad:1e9a1db6 | 176.449 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001877.pool.root.1 | 63BF46DA-C994-944F-A429-0C182D387941 | ad:e90f6fae | 489.358 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001878.pool.root.1 | 396A6B96-739B-984B-B62B-95C15F4E3515 | ad:a542df06 | 147.990 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001879.pool.root.1 | 1A11B5C7-AEEC-5D4E-81E7-87AB9F9C9384 | ad:d1a44835 | 529.550 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001880.pool.root.1 | 8125F010-8CBE-DD40-9814-818A8F9D2D2D | ad:45cc5d27 | 176.813 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001881.pool.root.1 | 416F0341-9B75-3C4B-B94B-2DDE08FF3E63 | ad:e58a5ae5 | 176.429 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001882.pool.root.1 | 97985874-9BEC-554D-B331-92E0A6A89306 | ad:6fb5852d | 595.420 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001883.pool.root.1 | 4FEBEB21-07E8-8546-BC9C-76D4A778F39D | ad:14c69652 | 147.521 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001884.pool.root.1 | 9FB30939-9EF9-9141-AE80-245F3DE81D75 | ad:802787a6 | 147.769 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001887.pool.root.1 | 8BEAD144-92EA-2442-A634-D45D5E5DCEF6 | ad:0b920502 | 147.429 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001888.pool.root.1 | 41EF6E15-481C-364E-8F24-2AA48E746EBE | ad:7253dc50 | 147.481 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001889.pool.root.1 | BEF30B76-9B59-304A-B741-9FB05BD62FC8 | ad:280ad2a4 | 147.480 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001890.pool.root.1 | 91B5546A-1CE3-2943-AF4A-8E48BA1D3262 | ad:abb190a3 | 450.444 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001891.pool.root.1 | 899CBEC8-F5E0-2D4E-B73A-E47AEFD5BBAC | ad:ee8a580f | 498.656 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001892.pool.root.1 | F21124CD-EB36-DD46-9CFE-8A7C5FE23A8D | ad:33d698ab | 491.547 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001893.pool.root.1 | 4EA0AC98-35C6-A544-A999-4DFBB15E625A | ad:b3dd3d4b | 509.507 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001894.pool.root.1 | 2A9F3DA4-B8EA-E541-B71A-FFF1F2C1B53E | ad:93997fc1 | 500.822 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001895.pool.root.1 | C80FF488-E3D7-6D49-8D3C-07976AF9A0A5 | ad:4322b00e | 538.677 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001896.pool.root.1 | FF078968-F2AC-D24F-B53F-9FF228BDD3A3 | ad:d6913ff7 | 517.066 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001897.pool.root.1 | B6C24BDC-E783-2644-B2EB-FE3C518FBF0A | ad:0bba7a03 | 147.677 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001898.pool.root.1 | 169D54EA-C4FF-6F45-93FF-DC6ECA94BB9B | ad:3d401e80 | 504.651 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001899.pool.root.1 | 37ECEFD6-CC8B-8C4F-9E14-1F4446B02179 | ad:029e9465 | 477.686 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001900.pool.root.1 | 43D84A40-B9C1-0440-AC7F-4FDE1CD4BEBB | ad:982f7db1 | 147.459 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001901.pool.root.1 | C539430A-F2DA-DB48-8A3A-D2B860EA3BFF | ad:5ea91f23 | 477.570 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001902.pool.root.1 | 73902CFD-47F6-6644-8D1C-F440FF96A3C5 | ad:97ec5ac0 | 147.448 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001903.pool.root.1 | 522055CA-F084-2D46-B90E-6249C64B8667 | ad:cbeec858 | 474.473 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001904.pool.root.1 | F913FA37-F9ED-1E4B-BA86-DFD3BBFB6270 | ad:7dfa56ac | 147.468 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001905.pool.root.1 | 46595FB8-64DD-054D-AF29-16BE9EA728B7 | ad:6f8ba06d | 465.536 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001906.pool.root.1 | 5575D1D4-02EF-CD46-98CC-050B04439B6E | ad:f592dfa4 | 495.254 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001908.pool.root.1 | A2AE3825-5BE3-1A4D-83AE-B5995F708DBF | ad:fb9eb3e4 | 147.375 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001909.pool.root.1 | C7A7273F-64A8-6946-AE00-9D9058EA7A21 | ad:4acf64ee | 532.445 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001911.pool.root.1 | BD19D4D1-17AC-1A47-B221-B3E1A6789C4B | ad:54583773 | 574.303 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001912.pool.root.1 | EB1A32D2-F0EB-DF43-A3D3-9D23F9198391 | ad:e24d8ce0 | 147.459 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001913.pool.root.1 | 6291D0C0-5F7F-DE47-9049-3D07E90A79EB | ad:bda1d5ac | 484.073 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001914.pool.root.1 | 9717430B-5659-7943-8C56-BFA618624D2C | ad:8144c8fe | 147.541 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001915.pool.root.1 | 7698DB7B-047D-BB40-B6EE-560442877789 | ad:e341b773 | 439.769 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001916.pool.root.1 | E764E66C-6733-2A4A-87B7-06B8AC1688F7 | ad:412728c1 | 488.427 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001917.pool.root.1 | 4B6BC727-3E2F-5449-9866-DEE807C6721E | ad:6a843521 | 147.530 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001918.pool.root.1 | 60B8F0F8-A50E-4C49-9A4E-CBB705E0F633 | ad:40ab5301 | 147.403 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001919.pool.root.1 | DB820555-2517-3943-9FB5-D89CC2D1B7D3 | ad:e4492d3f | 443.654 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001920.pool.root.1 | 3D1E22E8-CD52-ED47-B1DE-3CDA3E47C1F3 | ad:6181b754 | 147.791 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001921.pool.root.1 | E74F1C9A-CBC1-8448-93A6-839BDEC0E364 | ad:a5212f87 | 473.024 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001922.pool.root.1 | 104A1CDE-4736-5D4F-9ADA-8368272E8507 | ad:3d869af9 | 147.416 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001923.pool.root.1 | D83698FA-DE4F-9F44-A4B2-9AB6DD681DD2 | ad:785a982d | 147.424 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001924.pool.root.1 | 68D7603C-CD1D-4748-A308-67272A51E930 | ad:020a1742 | 420.375 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001925.pool.root.1 | D4470C4E-CF1B-874E-8B49-692DC918B258 | ad:a1b691c6 | 147.411 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001926.pool.root.1 | D5EA6219-47CD-0A44-A5B4-28C82B111635 | ad:dab97d44 | 608.397 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001927.pool.root.1 | FB2ACB21-6F28-C74E-BE6C-505C87EDA530 | ad:0e841bc2 | 542.125 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001931.pool.root.1 | 3E64CF27-ADF1-314B-9BBF-44B18348D914 | ad:d3b35c8f | 456.253 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001932.pool.root.1 | B803B6F9-A27C-6D4A-B2F6-DD57A06350E2 | ad:f907e30e | 473.399 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001933.pool.root.1 | B17E4B1E-C5D8-C04A-9DE2-E20EE1E5CC00 | ad:8c19e0d4 | 181.794 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001934.pool.root.1 | DAA618C3-49D6-7940-B033-7639C030DB51 | ad:489bcb11 | 590.794 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001935.pool.root.1 | 7CBB6950-5B6C-D84D-BE1C-815ABDFE0504 | ad:ef9ba579 | 176.448 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001936.pool.root.1 | 99C5A698-740D-CA40-910D-668C176EE234 | ad:d369f9b4 | 516.135 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001937.pool.root.1 | 8D50B68F-C955-BD4C-B2F5-0E38C3D154FD | ad:403b99ae | 147.466 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001938.pool.root.1 | F084900E-0FDD-9649-9540-8106693FBB0C | ad:b2016837 | 694.944 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001939.pool.root.1 | 6E440F3A-6646-C640-9616-024386703704 | ad:956586fe | 697.825 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001940.pool.root.1 | 86129923-5945-4F41-A967-8238DD36A60B | ad:0a92d27a | 168.727 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001941.pool.root.1 | AC295DA4-059F-5244-AEB6-F25B753BDB1D | ad:3652c34d | 147.561 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001942.pool.root.1 | C95A8E55-F812-6245-92E8-34CBA0646720 | ad:da065e15 | 176.158 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001943.pool.root.1 | A155087B-1AF4-4545-9DEB-2DB799707A67 | ad:975c1f05 | 508.083 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001944.pool.root.1 | 286F44EF-5070-1C4C-8A22-6C11803C1DEC | ad:472dbf8a | 511.117 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001945.pool.root.1 | 6D994791-BCF7-BC49-A181-3B9EA676888C | ad:5c09281e | 495.964 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001946.pool.root.1 | 8C887E9C-8723-2645-9474-1E01B8CEDA82 | ad:285afec8 | 453.676 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001947.pool.root.1 | 99397ACE-537C-4A42-95C1-389814E49CE3 | ad:e96bb658 | 447.222 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001948.pool.root.1 | 2A379396-F543-5544-9127-F080E10E1BB9 | ad:9cb6a6ed | 534.527 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001949.pool.root.1 | 75743598-83E9-BB4B-A0AA-2D8DC7CF67EC | ad:69a0c13a | 490.526 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001951.pool.root.1 | C1B5A2E1-6751-6A40-86FC-0B17CC16844D | ad:664e2bef | 456.421 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001952.pool.root.1 | F2030534-A5F4-994D-99F3-A9ECF3139A5A | ad:1924ac19 | 147.434 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001953.pool.root.1 | A9A8CF3F-5BCA-1347-88C0-661880352E30 | ad:24d99ca7 | 586.271 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001954.pool.root.1 | 31E6D4B8-CED5-3D41-B822-FA3477826900 | ad:ba19ecfd | 514.120 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001955.pool.root.1 | 253F2680-4617-1B4C-B60E-2E416E3708B3 | ad:fb989c14 | 147.479 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001957.pool.root.1 | C3EE9296-947D-C641-80F7-83215A6EE8FA | ad:bebd977b | 173.925 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001958.pool.root.1 | E786412D-6F4E-EC46-BAE1-9D42EDC23F03 | ad:c33ed1e6 | 147.712 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001959.pool.root.1 | F77980E2-E7D4-A940-9C95-A7BAD2C41ABA | ad:a71ae00d | 536.600 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001960.pool.root.1 | E4AE177F-CD47-D846-ADC6-9F4EB6551A76 | ad:62a9b87d | 447.563 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001961.pool.root.1 | 5EE836B2-F51C-6B4F-ADE0-C2C3BF625BB9 | ad:26e17dfd | 534.759 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001963.pool.root.1 | 0D2B6FD8-5A50-8D46-A4F7-22C271F21EEC | ad:0b38b580 | 551.787 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001964.pool.root.1 | D2BA9933-F8EA-6B48-8C9B-CD162F0B80D1 | ad:52c61563 | 604.621 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001965.pool.root.1 | 1AE19C25-357E-B94E-A668-C591E66BC2D8 | ad:ebe6228d | 577.025 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001966.pool.root.1 | 0CC72972-05BB-8C4C-86C2-72BDEB390593 | ad:c11f2f65 | 674.915 kB |        4 |
| data15_13TeV:DAOD_EXOT15.10330628._001967.pool.root.1 | D0D36D11-23CA-0340-881F-C1E13B7A3F57 | ad:7a78dd38 | 147.416 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001968.pool.root.1 | 865FC216-0925-0449-A5B9-6035603C9DA4 | ad:91c6bda3 | 147.487 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001969.pool.root.1 | 8789F61A-61B4-3B4A-B3FC-E8360369FAA7 | ad:d6a8c9c8 | 464.997 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001970.pool.root.1 | B967E371-773F-9848-A0D7-651E8DAB80A0 | ad:66ca16c0 | 467.670 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001971.pool.root.1 | 00EED7E4-1D28-194D-A14B-D37D2F18D6FF | ad:712b565e | 467.627 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001972.pool.root.1 | F3A7DBE8-C83E-124F-85A2-695DD225CD92 | ad:9429a56e | 483.063 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001973.pool.root.1 | 3E2F2114-5CE5-0F40-9056-48DF93FFCCB8 | ad:6e2604cd | 526.458 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001974.pool.root.1 | 83E5C095-A0C0-CA42-9741-EF79B498F4FC | ad:e7dbf8b9 | 147.660 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001975.pool.root.1 | 5DA65ABD-68DF-F34B-82B0-D4ECB78A94A8 | ad:e526be85 | 147.424 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001976.pool.root.1 | 52EDAC99-E89E-6640-ACF1-FCDCE244A344 | ad:7e64cce2 | 536.957 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._001977.pool.root.1 | 852B7CB5-D8E5-0047-9DF9-700E961B2C79 | ad:0d45b433 | 176.375 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001978.pool.root.1 | 15CB14C5-CBA6-BC43-90AE-2028E9708356 | ad:8b078896 | 176.715 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001979.pool.root.1 | 4D2BDD51-40C3-F147-BDFB-1913E426C77E | ad:cd2293e3 | 147.880 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001980.pool.root.1 | 40A058E6-3BCC-5045-93AC-A50D31AA1824 | ad:828da753 | 147.463 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001981.pool.root.1 | 4B5CB927-EE09-D34A-84ED-459260DB1907 | ad:c88de165 | 481.556 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001982.pool.root.1 | FAA70915-1377-F347-9DD5-5ED0F26D8F51 | ad:ce57a1f5 | 681.193 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001983.pool.root.1 | CF6F229E-5326-0B45-A5D6-93D359D7BF87 | ad:650ef7f6 | 449.257 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001984.pool.root.1 | 02574441-5359-D24C-987F-400175FB0084 | ad:2d97adc3 | 481.364 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001985.pool.root.1 | 9D291F51-C997-734C-8DA9-20BCD2EECF25 | ad:2c5068bf | 147.715 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001986.pool.root.1 | 2C6029C6-0049-7645-A4B3-A3352A098C8B | ad:fe29bb59 | 300.147 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001987.pool.root.1 | 0B75E5D9-50ED-A44D-B674-83BB96170673 | ad:afeb79c7 | 147.520 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001988.pool.root.1 | B91D56CD-4925-9C4C-80F2-666F2CB7DD2B | ad:f8a7c9f8 | 174.201 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001989.pool.root.1 | DB77D217-2E20-4C4B-85B9-FFB354992440 | ad:c51ac740 | 147.385 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001990.pool.root.1 | 3BC9B5D4-8FDD-9541-9A81-C17A9B140869 | ad:b54f3b67 | 173.894 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001991.pool.root.1 | 4A4DCE5E-FDEC-9B48-9C6D-DB177079CD15 | ad:e7fe624c | 635.893 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._001992.pool.root.1 | 7C001973-393E-EF46-BF22-85F8C70377CB | ad:c8144399 | 147.423 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001993.pool.root.1 | 9771A4FE-719E-A845-B170-FD64813439BB | ad:e3974a9e | 147.489 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001994.pool.root.1 | 8C73BB6C-B4E8-824A-B94B-ED74EF31B517 | ad:fd8c33b3 | 147.419 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001995.pool.root.1 | 50277AF1-8A73-1445-9A7E-C52D4690A4C9 | ad:a6ea7124 | 467.893 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001996.pool.root.1 | 95F57F1F-B031-4743-8E11-EBB12B029924 | ad:1cd56d05 | 472.821 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001997.pool.root.1 | AD81A834-57EA-494E-9519-BF1A2E1D70E0 | ad:c520f1fe | 489.409 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._001998.pool.root.1 | 753128D5-A757-614A-AFB3-97CB009ED04D | ad:013e72f9 | 147.565 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._001999.pool.root.1 | B85CA087-65AC-5D42-98DC-E2FEF675D481 | ad:4138d96e | 639.018 kB |        4 |
| data15_13TeV:DAOD_EXOT15.10330628._002000.pool.root.1 | A1810104-BE8E-894C-BCB4-AD66BFB7AE31 | ad:21bd2245 | 147.468 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002001.pool.root.1 | 412B4F9F-8EBF-9741-BB3A-CA4A027E1F5C | ad:ddb5f955 | 429.127 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002003.pool.root.1 | 6DBA588A-2B6B-FB4F-856A-5ED9DDEAEC9C | ad:41262fa7 | 437.437 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002004.pool.root.1 | 7F478B92-CCDA-6E41-89E1-9CF1348BA170 | ad:9bd1acb7 | 480.286 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002005.pool.root.1 | 6B11AD0F-14C8-4C48-A9A7-045FB261F689 | ad:8afab367 | 209.624 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002006.pool.root.1 | E98431A8-9E29-EE42-8261-EB47516BF634 | ad:a5759796 | 535.538 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002007.pool.root.1 | 29A550A2-7537-F241-B905-A50DC800CC02 | ad:7ea6ff8b | 223.503 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002008.pool.root.1 | E848B91D-CE70-FD43-911C-DB7B576825BF | ad:67ded880 | 147.546 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002009.pool.root.1 | 1CE8742F-DD26-434A-B692-530E66C3FA7C | ad:3ee8a2a8 | 147.427 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002010.pool.root.1 | 1D707E33-EB63-B242-AD9F-174D7C6A5682 | ad:8ef301ac | 566.079 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002011.pool.root.1 | BC849453-09E4-BC4B-9541-C6A5D3E5CF8C | ad:fbe4fe37 | 278.844 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002012.pool.root.1 | 2E71EE68-6366-A144-83F4-59A61563DFB1 | ad:fa51915d | 651.450 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002013.pool.root.1 | D2690CD6-357F-FD43-A142-F7F885142C7A | ad:0aa252b7 | 671.413 kB |        4 |
| data15_13TeV:DAOD_EXOT15.10330628._002014.pool.root.1 | 126B6B10-1EA3-DA42-9995-D9AB77663ECB | ad:930540fa | 147.518 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002015.pool.root.1 | 4B2729E0-6D82-E34C-BD43-8688FDA4FFE5 | ad:d00170fc | 638.740 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._002016.pool.root.1 | 1B834439-3BD3-CB44-92DE-C4A00510EF39 | ad:784f0cbd | 773.909 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002017.pool.root.1 | 9FB5CB05-5743-6D4A-9538-2E078D9812D8 | ad:c77a6e4f | 511.459 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002018.pool.root.1 | C493B248-DC66-214C-AAB7-0B3F103A640B | ad:965b6ae4 | 147.868 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002019.pool.root.1 | 55D2D04E-134C-5546-B0F6-1C77A76662FA | ad:7888f1ed | 548.774 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002020.pool.root.1 | 3D36CFE6-3DE8-9142-BCDE-8FA5B1CC45E4 | ad:ef153630 | 147.636 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002021.pool.root.1 | 10EA74DD-A32D-ED48-95D6-8C52AC758F8C | ad:25f1526a | 476.123 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002022.pool.root.1 | 5DB30822-F728-6549-8D42-76DBB0C0B86D | ad:322a8540 | 693.830 kB |        4 |
| data15_13TeV:DAOD_EXOT15.10330628._002023.pool.root.1 | 505BC398-408D-EE40-A8BF-39C4A0A4A31A | ad:15c280d2 | 569.798 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002024.pool.root.1 | 725337B8-648B-9A40-9BB7-9A29B022ABAB | ad:e8ec667c | 551.816 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002025.pool.root.1 | DA0B68A2-FAD8-FB4F-980B-3A18DED491FB | ad:99ef01ea | 580.193 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002026.pool.root.1 | 648F27A8-87B0-2845-AA53-5E92E3DA8398 | ad:ea885a4f | 147.798 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002027.pool.root.1 | 78CF4D8F-CE68-B842-A15E-8CD7D5D03A9A | ad:f7b26a67 | 510.377 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002028.pool.root.1 | 15555F1B-F01F-7340-A7A2-971C50526F6F | ad:44f761b2 | 147.999 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002029.pool.root.1 | 41963F01-A2E0-F84E-ADEA-243D4C2C022B | ad:7fdbbfac | 597.861 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._002030.pool.root.1 | 060766B2-8C0C-D844-84CD-C04C05B8804C | ad:c8a1f9d2 | 485.581 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002031.pool.root.1 | 2A815A7F-DF21-1F4C-BF34-051BD8AC8EA0 | ad:e832d9aa | 472.651 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002032.pool.root.1 | 46BE9E7F-E0EF-824A-AF55-AF41B48080EA | ad:a69f82b5 | 600.089 kB |        3 |
| data15_13TeV:DAOD_EXOT15.10330628._002033.pool.root.1 | 715B8351-56DB-5440-AF4B-28446294C700 | ad:80267654 | 176.698 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002034.pool.root.1 | 5104383A-FDB9-E542-A12F-5E0C0E7DA0C8 | ad:0e097794 | 551.785 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002035.pool.root.1 | 11102770-90CC-C444-A390-87A7F44F61F4 | ad:8e772dc3 | 513.075 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002036.pool.root.1 | 59D9C6DF-5446-894B-9B09-D898A27E022A | ad:ad316a16 | 200.064 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002037.pool.root.1 | A9D89057-DEE7-9944-A829-DEC249414157 | ad:358f77de | 223.967 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002038.pool.root.1 | A77676BA-1B08-4D43-8A6F-3E7801B6333B | ad:fcf6f1ff | 522.998 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002039.pool.root.1 | 25EDC2D2-25DA-C944-BC8F-0EDC2E772902 | ad:47bc77b2 | 451.239 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002040.pool.root.1 | 77187052-0024-9745-B53C-6299B00F35E1 | ad:8334f5dc | 479.510 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002041.pool.root.1 | 2C072DB3-2EB9-B840-8145-59A86BEFFFF3 | ad:4225e3f7 | 197.502 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002042.pool.root.1 | 9B40AC6E-135C-DD49-94CF-B813C9E91C9D | ad:a2d8d7f5 | 147.531 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002043.pool.root.1 | 4BFBA8FD-0D75-5442-B1D4-28875A2CDD21 | ad:68df57da | 492.886 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002044.pool.root.1 | 5CC8DCF9-52E9-8541-B9AF-F84C490D18F6 | ad:c98f4b4c | 147.711 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002045.pool.root.1 | 8B7CD541-EC3E-0F46-9EDF-0DF127A2D991 | ad:1a3757e3 | 513.165 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002046.pool.root.1 | 8C593DF2-4DFD-CE40-9703-D753996DD103 | ad:4ef3823a | 558.743 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002047.pool.root.1 | 374D3467-B2C7-654D-8DC8-5AD1058F85D4 | ad:9f29811e | 147.358 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002048.pool.root.1 | 2814DD78-FB83-7744-92E3-EA85DF1FF0B5 | ad:920d0361 | 147.456 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002049.pool.root.1 | AA31D3EE-0250-BF43-B45F-B2CAD733752F | ad:e337d04b | 463.359 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002058.pool.root.1 | B3B1D346-7345-D945-9F28-55142DF8E853 | ad:5422155c | 168.568 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002059.pool.root.1 | D0ED3D59-1017-3144-ACF7-14672B9BD3EF | ad:00bc26db | 555.569 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002060.pool.root.1 | AA8E5111-0717-6248-9AE8-97419549E0E0 | ad:44d4b3f0 | 535.207 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002061.pool.root.1 | 96F4B91C-C6AD-1B49-B2A2-D96C4D5C5D52 | ad:5b5b8ef8 | 168.736 kB |        0 |
| data15_13TeV:DAOD_EXOT15.10330628._002062.pool.root.1 | D23C99D2-681B-E143-B0FA-1627B8485AD4 | ad:338ea804 | 477.705 kB |        1 |
| data15_13TeV:DAOD_EXOT15.10330628._002063.pool.root.1 | CB32E810-E8F0-344B-8E31-33FB358D7605 | ad:c1adfdda | 536.846 kB |        2 |
| data15_13TeV:DAOD_EXOT15.10330628._002064.pool.root.1 | 08D5389A-AB46-384B-9A7D-A48AAB9C976C | ad:7313ef6b | 147.387 kB |        0 |
+-------------------------------------------------------+--------------------------------------+-------------+------------+----------+
Total files : 228
Total size : 84.558 MB
Total events : 211
");
            }
            else
            {
                Assert.IsTrue(false, "Unknown dataset: " + dsname);
                return null;
            }
        }

        public static Dictionary<string,string> AddGoodDownloadResponses(this Dictionary<string, string> dict, string dsName, string linuxDir)
        {
            var d = dict
                .AddEntry($"rm -rf /tmp/{dsName}.filelist", "")
                .AddEntry($"mkdir -p {linuxDir}", "")
                ;

            // Stuff that is custom
            if (dsName == "mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
            {
                foreach (var fileIndex in Enumerable.Range(1,43))
                {
                    d = d.AddEntry($"echo mc15_13TeV:AOD.07483884._0000{fileIndex.ToString("00")}.pool.root.1 >> /tmp/{dsName}.filelist", "");
                }
                d = d.AddEntry("rucio -T 3600 download --dir /tmp/gwattsdownload/now `cat /tmp/mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282.filelist`",
                    @"[32;1m2016-06-18 18:07:45,515 INFO [Starting download for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 with 40 files][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000002.pool.root.1][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000001.pool.root.1][0m
[32;1m2016-06-18 18:07:46,096 INFO [Starting the download of mc15_13TeV:AOD.07787075._000003.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33; 1m2016 - 06 - 18 18:07:50,254 WARNING[The requested service is not available at the moment.
     Details: An unknown exception occurred.
     Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:35:53,426 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:35:54,464 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1688.36847115 seconds][0m
[32;1m2016-06-18 18:35:54,464 INFO [Starting the download of mc15_13TeV:AOD.07787075._000005.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:35:58,296 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:39:14,853 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,487 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,565 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1889.4696362 seconds][0m
[32;1m2016-06-18 18:39:15,565 INFO [Starting the download of mc15_13TeV:AOD.07787075._000006.pool.root.1][0m
[32;1m2016-06-18 18:39:16,196 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1890.09949017 seconds][0m
[32;1m2016-06-18 18:39:16,196 INFO [Starting the download of mc15_13TeV:AOD.07787075._000004.pool.root.1][0m
[33;1m2016-06-18 18:39:20,146 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:39:21,560 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 18:39:24,248 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:01:15,026 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:01:16,053 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.58848286 seconds][0m
[32;1m2016-06-18 19:01:16,053 INFO [Starting the download of mc15_13TeV:AOD.07787075._000009.pool.root.1][0m
[32;1m2016-06-18 19:03:47,019 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:03:47,721 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1471.52423596 seconds][0m
[32;1m2016-06-18 19:03:47,721 INFO [Starting the download of mc15_13TeV:AOD.07787075._000008.pool.root.1][0m
[33;1m2016-06-18 19:03:50,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:03:56,575 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:06:15,489 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:06:16,194 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1620.62827492 seconds][0m
[32;1m2016-06-18 19:06:16,194 INFO [Starting the download of mc15_13TeV:AOD.07787075._000007.pool.root.1][0m
[33;1m2016-06-18 19:06:20,145 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:22,284 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:24,319 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:24:17,962 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:24:18,848 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1382.79514408 seconds][0m
[32;1m2016-06-18 19:24:18,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000010.pool.root.1][0m
[33;1m2016-06-18 19:24:22,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:27:28,312 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:27:29,048 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1421.32698298 seconds][0m
[32;1m2016-06-18 19:27:29,048 INFO [Starting the download of mc15_13TeV:AOD.07787075._000014.pool.root.1][0m
[32;1m2016-06-18 19:29:24,995 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:29:25,749 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1389.55475783 seconds][0m
[32;1m2016-06-18 19:29:25,749 INFO [Starting the download of mc15_13TeV:AOD.07787075._000012.pool.root.1][0m
[33;1m2016-06-18 19:29:28,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:32,250 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:34,283 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:38,220 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:49:18,205 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:49:19,079 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1500.23046708 seconds][0m
[32;1m2016-06-18 19:49:19,079 INFO [Starting the download of mc15_13TeV:AOD.07787075._000011.pool.root.1][0m
[33;1m2016-06-18 19:49:26,028 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:32,014 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:34,173 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:36,219 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:38,260 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:52:40,827 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 19:54:14,416 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:54:15,126 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1606.07766199 seconds][0m
[32;1m2016-06-18 19:54:15,126 INFO [Starting the download of mc15_13TeV:AOD.07787075._000021.pool.root.1][0m
[32;1m2016-06-18 19:55:30,022 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:55:30,724 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1564.97463489 seconds][0m
[32;1m2016-06-18 19:55:30,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000015.pool.root.1][0m
[32;1m2016-06-18 20:14:40,857 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:14:41,724 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1522.64424896 seconds][0m
[32;1m2016-06-18 20:14:41,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000013.pool.root.1][0m
[33;1m2016-06-18 20:17:40,736 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:18:39,150 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:18:39,858 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1464.73195505 seconds][0m
[32;1m2016-06-18 20:18:39,859 INFO [Starting the download of mc15_13TeV:AOD.07787075._000024.pool.root.1][0m
[32;1m2016-06-18 20:19:40,268 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:19:41,027 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1450.30274987 seconds][0m
[32;1m2016-06-18 20:19:41,027 INFO [Starting the download of mc15_13TeV:AOD.07787075._000016.pool.root.1][0m
[32;1m2016-06-18 20:37:47,475 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:37:48,519 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1386.79511619 seconds][0m
[32;1m2016-06-18 20:37:48,519 INFO [Starting the download of mc15_13TeV:AOD.07787075._000017.pool.root.1][0m
[33;1m2016-06-18 20:40:47,697 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:42:01,174 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:42:01,906 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1402.04727793 seconds][0m
[32;1m2016-06-18 20:42:01,906 INFO [Starting the download of mc15_13TeV:AOD.07787075._000031.pool.root.1][0m
[32;1m2016-06-18 20:43:44,255 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:43:44,962 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1443.93537712 seconds][0m
[32;1m2016-06-18 20:43:44,963 INFO [Starting the download of mc15_13TeV:AOD.07787075._000018.pool.root.1][0m
[33;1m2016-06-18 20:43:51,870 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 20:44:01,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:07:57,006 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:07:58,069 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1809.55001307 seconds][0m
[32;1m2016-06-18 21:07:58,070 INFO [Starting the download of mc15_13TeV:AOD.07787075._000020.pool.root.1][0m
[33;1m2016-06-18 21:10:59,047 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:12:40,294 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:12:41,035 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1736.07202721 seconds][0m
[32;1m2016-06-18 21:12:41,035 INFO [Starting the download of mc15_13TeV:AOD.07787075._000019.pool.root.1][0m
[33;1m2016-06-18 21:12:44,872 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:48,865 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:52,808 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:56,797 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:13:00,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:14:13,346 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:14:14,057 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1932.15072584 seconds][0m
[32;1m2016-06-18 21:14:14,057 INFO [Starting the download of mc15_13TeV:AOD.07787075._000035.pool.root.1][0m
[33;1m2016-06-18 21:16:01,281 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:36:19,776 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:36:20,651 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1702.58084106 seconds][0m
[32;1m2016-06-18 21:36:20,651 INFO [Starting the download of mc15_13TeV:AOD.07787075._000025.pool.root.1][0m
[33;1m2016-06-18 21:39:19,710 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:40:38,774 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:40:39,508 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1678.47296882 seconds][0m
[32;1m2016-06-18 21:40:39,508 INFO [Starting the download of mc15_13TeV:AOD.07787075._000022.pool.root.1][0m
[33;1m2016-06-18 21:43:38,505 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:44:14,839 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:44:15,709 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1801.65164685 seconds][0m
[32;1m2016-06-18 21:44:15,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000038.pool.root.1][0m
[33;1m2016-06-18 21:44:21,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:44:31,331 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 22:07:58,031 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:07:58,977 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1898.32642102 seconds][0m
[32;1m2016-06-18 22:07:58,978 INFO [Starting the download of mc15_13TeV:AOD.07787075._000026.pool.root.1][0m
[32;1m2016-06-18 22:10:01,725 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:10:02,577 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1763.06805682 seconds][0m
[32;1m2016-06-18 22:10:02,577 INFO [Starting the download of mc15_13TeV:AOD.07787075._000023.pool.root.1][0m
[33;1m2016-06-18 22:10:57,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:12:01,940 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 22:12:02,640 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1666.92999101 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000003.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[33;1m2016-06-18 22:13:03,228 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:30:20,655 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:30:21,708 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1342.73055696 seconds][0m
[32;1m2016-06-18 22:30:21,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000027.pool.root.1][0m
[32;1m2016-06-18 22:31:43,652 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:31:44,417 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1301.840518 seconds][0m
[32;1m2016-06-18 22:31:44,418 INFO [Starting the download of mc15_13TeV:AOD.07787075._000029.pool.root.1][0m
[33;1m2016-06-18 22:33:20,693 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 22:34:43,338 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:58:49,731 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:58:50,692 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1708.98274994 seconds][0m
[32;1m2016-06-18 22:58:50,692 INFO [Starting the download of mc15_13TeV:AOD.07787075._000028.pool.root.1][0m
[32;1m2016-06-18 23:01:39,524 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:01:40,246 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1795.82815194 seconds][0m
[32;1m2016-06-18 23:01:40,246 INFO [Starting the download of mc15_13TeV:AOD.07787075._000037.pool.root.1][0m
[33;1m2016-06-18 23:01:49,569 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:04:39,307 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:24:42,104 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:24:42,997 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded. 3.9 GB bytes downloaded in 1382.75059414 seconds][0m
[32;1m2016-06-18 23:24:42,997 INFO [Starting the download of mc15_13TeV:AOD.07787075._000040.pool.root.1][0m
[32;1m2016-06-18 23:26:13,975 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:26:14,675 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1643.98309779 seconds][0m
[32;1m2016-06-18 23:26:14,675 INFO [Starting the download of mc15_13TeV:AOD.07787075._000030.pool.root.1][0m
[33;1m2016-06-18 23:27:43,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:29:13,601 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:41:29,306 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:41:30,184 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded. 2.7 GB bytes downloaded in 1007.18631005 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000001.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-18 23:52:33,220 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:52:34,316 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1579.64014387 seconds][0m
[32;1m2016-06-18 23:52:34,316 INFO [Starting the download of mc15_13TeV:AOD.07787075._000032.pool.root.1][0m
[33;1m2016-06-18 23:55:33,224 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:18:47,533 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:18:48,584 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1574.2682879 seconds][0m
[32;1m2016-06-19 00:18:48,585 INFO [Starting the download of mc15_13TeV:AOD.07787075._000033.pool.root.1][0m
[33;1m2016-06-19 00:21:47,434 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:45:02,570 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:45:03,900 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1575.31498194 seconds][0m
[32;1m2016-06-19 00:45:03,900 INFO [Starting the download of mc15_13TeV:AOD.07787075._000034.pool.root.1][0m
[33;1m2016-06-19 00:48:04,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:09:54,777 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:09:55,847 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1491.94715786 seconds][0m
[32;1m2016-06-19 01:09:55,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000036.pool.root.1][0m
[33;1m2016-06-19 01:12:54,892 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:34:38,077 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:34:38,972 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1483.12419295 seconds][0m
[32;1m2016-06-19 01:34:38,972 INFO [Starting the download of mc15_13TeV:AOD.07787075._000039.pool.root.1][0m
[33;1m2016-06-19 01:37:38,152 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:59:59,061 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 02:00:00,149 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.17686605 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000002.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-19 02:00:01,797 INFO [Download operation for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 done][0m
----------------------------------
Download summary
----------------------------------------
DID mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282
Total files :                                40
Downloaded files :                           40
Files already found locally :                 0
Files that cannot be downloaded :             0
");
            }
            else
            {
                Assert.IsTrue(false, $"Unknown dataset: {dsName}");
            }

            return d;
        }

        /// <summary>
        /// Generate stuff for a bad download.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dsName"></param>
        /// <param name="linuxDir"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AddBadDownloadResponses(this Dictionary<string, string> dict, string dsName, string linuxDir)
        {
            var d = dict
                .AddEntry($"rm -rf /tmp/{dsName}.filelist", "")
                .AddEntry($"mkdir -p {linuxDir}", "")
                ;

            // Stuff that is custom
            if (dsName == "mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
            {
                foreach (var fileIndex in Enumerable.Range(1, 43))
                {
                    d = d.AddEntry($"echo mc15_13TeV:AOD.07483884._0000{fileIndex.ToString("00")}.pool.root.1 >> /tmp/{dsName}.filelist", "");
                }
                d = d.AddEntry("rucio -T 3600 download --dir /tmp/gwattsdownload/now `cat /tmp/mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282.filelist`",
                    @"[32;1m2016-06-18 18:07:45,515 INFO [Starting download for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 with 40 files][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000002.pool.root.1][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000001.pool.root.1][0m
[32;1m2016-06-18 18:07:46,096 INFO [Starting the download of mc15_13TeV:AOD.07787075._000003.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33; 1m2016 - 06 - 18 18:07:50,254 WARNING[The requested service is not available at the moment.
     Details: An unknown exception occurred.
     Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:35:53,426 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:35:54,464 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1688.36847115 seconds][0m
[32;1m2016-06-18 18:35:54,464 INFO [Starting the download of mc15_13TeV:AOD.07787075._000005.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:35:58,296 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:39:14,853 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,487 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,565 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1889.4696362 seconds][0m
[32;1m2016-06-18 18:39:15,565 INFO [Starting the download of mc15_13TeV:AOD.07787075._000006.pool.root.1][0m
[32;1m2016-06-18 18:39:16,196 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1890.09949017 seconds][0m
[32;1m2016-06-18 18:39:16,196 INFO [Starting the download of mc15_13TeV:AOD.07787075._000004.pool.root.1][0m
[33;1m2016-06-18 18:39:20,146 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:39:21,560 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 18:39:24,248 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:01:15,026 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:01:16,053 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.58848286 seconds][0m
[32;1m2016-06-18 19:01:16,053 INFO [Starting the download of mc15_13TeV:AOD.07787075._000009.pool.root.1][0m
[32;1m2016-06-18 19:03:47,019 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:03:47,721 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1471.52423596 seconds][0m
[32;1m2016-06-18 19:03:47,721 INFO [Starting the download of mc15_13TeV:AOD.07787075._000008.pool.root.1][0m
[33;1m2016-06-18 19:03:50,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:03:56,575 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:06:15,489 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:06:16,194 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1620.62827492 seconds][0m
[32;1m2016-06-18 19:06:16,194 INFO [Starting the download of mc15_13TeV:AOD.07787075._000007.pool.root.1][0m
[33;1m2016-06-18 19:06:20,145 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:22,284 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:24,319 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:24:17,962 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:24:18,848 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1382.79514408 seconds][0m
[32;1m2016-06-18 19:24:18,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000010.pool.root.1][0m
[33;1m2016-06-18 19:24:22,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:27:28,312 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:27:29,048 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1421.32698298 seconds][0m
[32;1m2016-06-18 19:27:29,048 INFO [Starting the download of mc15_13TeV:AOD.07787075._000014.pool.root.1][0m
[32;1m2016-06-18 19:29:24,995 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:29:25,749 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1389.55475783 seconds][0m
[32;1m2016-06-18 19:29:25,749 INFO [Starting the download of mc15_13TeV:AOD.07787075._000012.pool.root.1][0m
[33;1m2016-06-18 19:29:28,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:32,250 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:34,283 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:38,220 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:49:18,205 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:49:19,079 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1500.23046708 seconds][0m
[32;1m2016-06-18 19:49:19,079 INFO [Starting the download of mc15_13TeV:AOD.07787075._000011.pool.root.1][0m
[33;1m2016-06-18 19:49:26,028 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:32,014 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:34,173 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:36,219 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:38,260 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:52:40,827 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 19:54:14,416 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:54:15,126 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1606.07766199 seconds][0m
[32;1m2016-06-18 19:54:15,126 INFO [Starting the download of mc15_13TeV:AOD.07787075._000021.pool.root.1][0m
[32;1m2016-06-18 19:55:30,022 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:55:30,724 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1564.97463489 seconds][0m
[32;1m2016-06-18 19:55:30,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000015.pool.root.1][0m
[32;1m2016-06-18 20:14:40,857 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:14:41,724 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1522.64424896 seconds][0m
[32;1m2016-06-18 20:14:41,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000013.pool.root.1][0m
[33;1m2016-06-18 20:17:40,736 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:18:39,150 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:18:39,858 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1464.73195505 seconds][0m
[32;1m2016-06-18 20:18:39,859 INFO [Starting the download of mc15_13TeV:AOD.07787075._000024.pool.root.1][0m
[32;1m2016-06-18 20:19:40,268 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:19:41,027 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1450.30274987 seconds][0m
[32;1m2016-06-18 20:19:41,027 INFO [Starting the download of mc15_13TeV:AOD.07787075._000016.pool.root.1][0m
[32;1m2016-06-18 20:37:47,475 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:37:48,519 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1386.79511619 seconds][0m
[32;1m2016-06-18 20:37:48,519 INFO [Starting the download of mc15_13TeV:AOD.07787075._000017.pool.root.1][0m
[33;1m2016-06-18 20:40:47,697 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:42:01,174 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:42:01,906 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1402.04727793 seconds][0m
[32;1m2016-06-18 20:42:01,906 INFO [Starting the download of mc15_13TeV:AOD.07787075._000031.pool.root.1][0m
[32;1m2016-06-18 20:43:44,255 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:43:44,962 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1443.93537712 seconds][0m
[32;1m2016-06-18 20:43:44,963 INFO [Starting the download of mc15_13TeV:AOD.07787075._000018.pool.root.1][0m
[33;1m2016-06-18 20:43:51,870 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 20:44:01,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:07:57,006 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:07:58,069 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1809.55001307 seconds][0m
[32;1m2016-06-18 21:07:58,070 INFO [Starting the download of mc15_13TeV:AOD.07787075._000020.pool.root.1][0m
[33;1m2016-06-18 21:10:59,047 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:12:40,294 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:12:41,035 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1736.07202721 seconds][0m
[32;1m2016-06-18 21:12:41,035 INFO [Starting the download of mc15_13TeV:AOD.07787075._000019.pool.root.1][0m
[33;1m2016-06-18 21:12:44,872 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:48,865 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:52,808 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:56,797 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:13:00,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:14:13,346 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:14:14,057 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1932.15072584 seconds][0m
[32;1m2016-06-18 21:14:14,057 INFO [Starting the download of mc15_13TeV:AOD.07787075._000035.pool.root.1][0m
[33;1m2016-06-18 21:16:01,281 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:36:19,776 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:36:20,651 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1702.58084106 seconds][0m
[32;1m2016-06-18 21:36:20,651 INFO [Starting the download of mc15_13TeV:AOD.07787075._000025.pool.root.1][0m
[33;1m2016-06-18 21:39:19,710 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:40:38,774 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:40:39,508 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1678.47296882 seconds][0m
[32;1m2016-06-18 21:40:39,508 INFO [Starting the download of mc15_13TeV:AOD.07787075._000022.pool.root.1][0m
[33;1m2016-06-18 21:43:38,505 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:44:14,839 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:44:15,709 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1801.65164685 seconds][0m
[32;1m2016-06-18 21:44:15,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000038.pool.root.1][0m
[33;1m2016-06-18 21:44:21,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:44:31,331 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 22:07:58,031 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:07:58,977 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1898.32642102 seconds][0m
[32;1m2016-06-18 22:07:58,978 INFO [Starting the download of mc15_13TeV:AOD.07787075._000026.pool.root.1][0m
[32;1m2016-06-18 22:10:01,725 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:10:02,577 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1763.06805682 seconds][0m
[32;1m2016-06-18 22:10:02,577 INFO [Starting the download of mc15_13TeV:AOD.07787075._000023.pool.root.1][0m
[33;1m2016-06-18 22:10:57,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:12:01,940 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 22:12:02,640 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1666.92999101 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000003.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[33;1m2016-06-18 22:13:03,228 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:30:20,655 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:30:21,708 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1342.73055696 seconds][0m
[32;1m2016-06-18 22:30:21,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000027.pool.root.1][0m
[32;1m2016-06-18 22:31:43,652 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:31:44,417 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1301.840518 seconds][0m
[32;1m2016-06-18 22:31:44,418 INFO [Starting the download of mc15_13TeV:AOD.07787075._000029.pool.root.1][0m
[33;1m2016-06-18 22:33:20,693 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 22:34:43,338 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:58:49,731 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:58:50,692 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1708.98274994 seconds][0m
[32;1m2016-06-18 22:58:50,692 INFO [Starting the download of mc15_13TeV:AOD.07787075._000028.pool.root.1][0m
[32;1m2016-06-18 23:01:39,524 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:01:40,246 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1795.82815194 seconds][0m
[32;1m2016-06-18 23:01:40,246 INFO [Starting the download of mc15_13TeV:AOD.07787075._000037.pool.root.1][0m
[33;1m2016-06-18 23:01:49,569 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:04:39,307 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:24:42,104 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:24:42,997 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded. 3.9 GB bytes downloaded in 1382.75059414 seconds][0m
[32;1m2016-06-18 23:24:42,997 INFO [Starting the download of mc15_13TeV:AOD.07787075._000040.pool.root.1][0m
[32;1m2016-06-18 23:26:13,975 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:26:14,675 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1643.98309779 seconds][0m
[32;1m2016-06-18 23:26:14,675 INFO [Starting the download of mc15_13TeV:AOD.07787075._000030.pool.root.1][0m
[33;1m2016-06-18 23:27:43,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:29:13,601 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:41:29,306 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:41:30,184 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded. 2.7 GB bytes downloaded in 1007.18631005 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000001.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-18 23:52:33,220 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:52:34,316 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1579.64014387 seconds][0m
[32;1m2016-06-18 23:52:34,316 INFO [Starting the download of mc15_13TeV:AOD.07787075._000032.pool.root.1][0m
[33;1m2016-06-18 23:55:33,224 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:18:47,533 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:18:48,584 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1574.2682879 seconds][0m
[32;1m2016-06-19 00:18:48,585 INFO [Starting the download of mc15_13TeV:AOD.07787075._000033.pool.root.1][0m
[33;1m2016-06-19 00:21:47,434 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:45:02,570 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:45:03,900 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1575.31498194 seconds][0m
[32;1m2016-06-19 00:45:03,900 INFO [Starting the download of mc15_13TeV:AOD.07787075._000034.pool.root.1][0m
[33;1m2016-06-19 00:48:04,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:09:54,777 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:09:55,847 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1491.94715786 seconds][0m
[32;1m2016-06-19 01:09:55,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000036.pool.root.1][0m
[33;1m2016-06-19 01:12:54,892 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:34:38,077 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:34:38,972 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1483.12419295 seconds][0m
[32;1m2016-06-19 01:34:38,972 INFO [Starting the download of mc15_13TeV:AOD.07787075._000039.pool.root.1][0m
[33;1m2016-06-19 01:37:38,152 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:59:59,061 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 02:00:00,149 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.17686605 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000002.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-19 02:00:01,797 INFO [Download operation for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 done][0m
----------------------------------
Download summary
----------------------------------------
DID mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282
Total files :                                40
Downloaded files :                           39
Files already found locally :                 0
Files that cannot be downloaded :             1");
            }
            else
            {
                Assert.IsTrue(false, $"Unknown dataset: {dsName}");
            }

            return d;
        }

        /// <summary>
        /// Download, with a repeated clock-skew failure.
        /// </summary>
        /// <param name="dict"></param>
        /// <param name="dsName"></param>
        /// <param name="linuxDir"></param>
        /// <returns></returns>
        public static Dictionary<string, string> AddClockSkewDownloadResponses(this Dictionary<string, string> dict, string dsName, string linuxDir)
        {
            var d = dict
                .AddEntry($"rm -rf /tmp/{dsName}.filelist", "")
                .AddEntry($"mkdir -p {linuxDir}", "")
                ;

            // Stuff that is custom
            if (dsName == "mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282")
            {
                foreach (var fileIndex in Enumerable.Range(1, 43))
                {
                    d = d.AddEntry($"echo mc15_13TeV:AOD.07483884._0000{fileIndex.ToString("00")}.pool.root.1 >> /tmp/{dsName}.filelist", "");
                }
                d = d.AddEntry("rucio -T 3600 download --dir /tmp/gwattsdownload/now `cat /tmp/mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282.filelist`",
                    @"2016-06-18 15:35:07,774 INFO [Starting download for user.gwatts:user.gwatts.8746548._000001.hist-output.root with 1 files]
2016-06-18 15:35:07,980 INFO [Starting the download of user.gwatts:user.gwatts.8746548._000001.hist-output.root]
No handlers could be found for logger ""gfal2""
2016 - 06 - 18 15:35:12, 260 WARNING[The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_ftp_client: the server responded with an error 530 530 - globus_xio: Authentication Error  530 - OpenSSL Error: s3_srvr.c:3288: in library: SSL routines, function SSL3_GET_CLIENT_CERTIFICATE: no certificate returned  530 - globus_gsi_callback_module: Could not verify credential  530 - globus_gsi_callback_module: The certificate is not yet valid: Cert with subject: / DC = ch / DC = cern / OU = Organic Units / OU = Users / CN = gwatts / CN = 517517 / CN = Gordon Watts / CN = proxy is not yet valid - check clock skew between hosts.  530 End.]
2016 - 06 - 18 15:35:15, 668 WARNING[The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_ftp_client: the server responded with an error 530 530 - globus_xio: Authentication Error  530 - OpenSSL Error: s3_srvr.c:3288: in library: SSL routines, function SSL3_GET_CLIENT_CERTIFICATE: no certificate returned  530 - globus_gsi_callback_module: Could not verify credential  530 - globus_gsi_callback_module: The certificate is not yet valid: Cert with subject: / DC = ch / DC = cern / OU = Organic Units / OU = Users / CN = gwatts / CN = 517517 / CN = Gordon Watts / CN = proxy is not yet valid - check clock skew between hosts.  530 End.]
2016 - 06 - 18 15:35:19, 176 WARNING[The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_ftp_client: the server responded with an error 530 530 - globus_xio: Authentication Error  530 - OpenSSL Error: s3_srvr.c:3288: in library: SSL routines, function SSL3_GET_CLIENT_CERTIFICATE: no certificate returned  530 - globus_gsi_callback_module: Could not verify credential  530 - globus_gsi_callback_module: The certificate is not yet valid: Cert with subject: / DC = ch / DC = cern / OU = Organic Units / OU = Users / CN = gwatts / CN = 517517 / CN = Gordon Watts / CN = proxy is not yet valid - check clock skew between hosts.  530 End.]
2016 - 06 - 18 15:35:22, 696 WARNING[The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_ftp_client: the server responded with an error 530 530 - globus_xio: Authentication Error  530 - OpenSSL Error: s3_srvr.c:3288: in library: SSL routines, function SSL3_GET_CLIENT_CERTIFICATE: no certificate returned  530 - globus_gsi_callback_module: Could not verify credential  530 - globus_gsi_callback_module: The certificate is not yet valid: Cert with subject: / DC = ch / DC = cern / OU = Organic Units / OU = Users / CN = gwatts / CN = 517517 / CN = Gordon Watts / CN = proxy is not yet valid - check clock skew between hosts.  530 End.]
2016 - 06 - 18 15:35:26, 186 WARNING[The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_ftp_client: the server responded with an error 530 530 - globus_xio: Authentication Error  530 - OpenSSL Error: s3_srvr.c:3288: in library: SSL routines, function SSL3_GET_CLIENT_CERTIFICATE: no certificate returned  530 - globus_gsi_callback_module: Could not verify credential  530 - globus_gsi_callback_module: The certificate is not yet valid: Cert with subject: / DC = ch / DC = cern / OU = Organic Units / OU = Users / CN = gwatts / CN = 517517 / CN = Gordon Watts / CN = proxy is not yet valid - check clock skew between hosts.  530 End.]
2016 - 06 - 18 15:35:26, 893 ERROR[Cannot download file user.gwatts:user.gwatts.8746548._000001.hist - output.root]
2016 - 06 - 18 15:35:28, 001 INFO[Download operation for user.gwatts:user.gwatts.8746548._000001.hist - output.root done]
----------------------------------
    Download summary
    ----------------------------------------
    DID user.gwatts:user.gwatts.8746548._000001.hist - output.root
    Total files :                                 1
    Downloaded files :                            0
    Files already found locally :                 0
    Files that cannot be downloaded :             1");
            }
            else
            {
                Assert.IsTrue(false, $"Unknown dataset: {dsName}");
            }

            return d;
        }

        /// <summary>
        /// After we see the download command, then make the second time it is run "ok".
        /// </summary>
        /// <param name="s"></param>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public static dummySSHConnection AddClockSkewDownloadOKResponses(this dummySSHConnection s, string dsname)
        {
            s.AddQueuedChange("rucio download --dir /tmp/gwattsdownload/now `cat /tmp/mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282.filelist`",
                "rucio download --dir /tmp/gwattsdownload/now `cat /tmp/mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282.filelist`",
                @"[32;1m2016-06-18 18:07:45,515 INFO [Starting download for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 with 40 files][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000002.pool.root.1][0m
[32;1m2016-06-18 18:07:46,095 INFO [Starting the download of mc15_13TeV:AOD.07787075._000001.pool.root.1][0m
[32;1m2016-06-18 18:07:46,096 INFO [Starting the download of mc15_13TeV:AOD.07787075._000003.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33; 1m2016 - 06 - 18 18:07:50,254 WARNING[The requested service is not available at the moment.
     Details: An unknown exception occurred.
     Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:35:53,426 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:35:54,464 INFO [File mc15_13TeV:AOD.07787075._000002.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1688.36847115 seconds][0m
[32;1m2016-06-18 18:35:54,464 INFO [Starting the download of mc15_13TeV:AOD.07787075._000005.pool.root.1][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:35:58,296 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 18:39:14,853 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,487 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 18:39:15,565 INFO [File mc15_13TeV:AOD.07787075._000001.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1889.4696362 seconds][0m
[32;1m2016-06-18 18:39:15,565 INFO [Starting the download of mc15_13TeV:AOD.07787075._000006.pool.root.1][0m
[32;1m2016-06-18 18:39:16,196 INFO [File mc15_13TeV:AOD.07787075._000003.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1890.09949017 seconds][0m
[32;1m2016-06-18 18:39:16,196 INFO [Starting the download of mc15_13TeV:AOD.07787075._000004.pool.root.1][0m
[33;1m2016-06-18 18:39:20,146 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
No handlers could be found for logger ""gfal2""
[33;1m2016-06-18 18:39:21,560 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 18:39:24,248 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:01:15,026 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:01:16,053 INFO [File mc15_13TeV:AOD.07787075._000005.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.58848286 seconds][0m
[32;1m2016-06-18 19:01:16,053 INFO [Starting the download of mc15_13TeV:AOD.07787075._000009.pool.root.1][0m
[32;1m2016-06-18 19:03:47,019 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:03:47,721 INFO [File mc15_13TeV:AOD.07787075._000004.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1471.52423596 seconds][0m
[32;1m2016-06-18 19:03:47,721 INFO [Starting the download of mc15_13TeV:AOD.07787075._000008.pool.root.1][0m
[33;1m2016-06-18 19:03:50,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:03:56,575 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:06:15,489 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:06:16,194 INFO [File mc15_13TeV:AOD.07787075._000006.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1620.62827492 seconds][0m
[32;1m2016-06-18 19:06:16,194 INFO [Starting the download of mc15_13TeV:AOD.07787075._000007.pool.root.1][0m
[33;1m2016-06-18 19:06:20,145 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:22,284 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:06:24,319 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:24:17,962 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:24:18,848 INFO [File mc15_13TeV:AOD.07787075._000009.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1382.79514408 seconds][0m
[32;1m2016-06-18 19:24:18,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000010.pool.root.1][0m
[33;1m2016-06-18 19:24:22,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:27:28,312 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:27:29,048 INFO [File mc15_13TeV:AOD.07787075._000008.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1421.32698298 seconds][0m
[32;1m2016-06-18 19:27:29,048 INFO [Starting the download of mc15_13TeV:AOD.07787075._000014.pool.root.1][0m
[32;1m2016-06-18 19:29:24,995 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:29:25,749 INFO [File mc15_13TeV:AOD.07787075._000007.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1389.55475783 seconds][0m
[32;1m2016-06-18 19:29:25,749 INFO [Starting the download of mc15_13TeV:AOD.07787075._000012.pool.root.1][0m
[33;1m2016-06-18 19:29:28,437 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:32,250 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:34,283 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:29:38,220 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 19:49:18,205 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:49:19,079 INFO [File mc15_13TeV:AOD.07787075._000010.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1500.23046708 seconds][0m
[32;1m2016-06-18 19:49:19,079 INFO [Starting the download of mc15_13TeV:AOD.07787075._000011.pool.root.1][0m
[33;1m2016-06-18 19:49:26,028 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:32,014 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:34,173 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:36,219 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:49:38,260 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 19:52:40,827 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 19:54:14,416 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:54:15,126 INFO [File mc15_13TeV:AOD.07787075._000014.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1606.07766199 seconds][0m
[32;1m2016-06-18 19:54:15,126 INFO [Starting the download of mc15_13TeV:AOD.07787075._000021.pool.root.1][0m
[32;1m2016-06-18 19:55:30,022 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 19:55:30,724 INFO [File mc15_13TeV:AOD.07787075._000012.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1564.97463489 seconds][0m
[32;1m2016-06-18 19:55:30,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000015.pool.root.1][0m
[32;1m2016-06-18 20:14:40,857 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:14:41,724 INFO [File mc15_13TeV:AOD.07787075._000011.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1522.64424896 seconds][0m
[32;1m2016-06-18 20:14:41,724 INFO [Starting the download of mc15_13TeV:AOD.07787075._000013.pool.root.1][0m
[33;1m2016-06-18 20:17:40,736 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:18:39,150 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:18:39,858 INFO [File mc15_13TeV:AOD.07787075._000021.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1464.73195505 seconds][0m
[32;1m2016-06-18 20:18:39,859 INFO [Starting the download of mc15_13TeV:AOD.07787075._000024.pool.root.1][0m
[32;1m2016-06-18 20:19:40,268 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:19:41,027 INFO [File mc15_13TeV:AOD.07787075._000015.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1450.30274987 seconds][0m
[32;1m2016-06-18 20:19:41,027 INFO [Starting the download of mc15_13TeV:AOD.07787075._000016.pool.root.1][0m
[32;1m2016-06-18 20:37:47,475 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 20:37:48,519 INFO [File mc15_13TeV:AOD.07787075._000013.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1386.79511619 seconds][0m
[32;1m2016-06-18 20:37:48,519 INFO [Starting the download of mc15_13TeV:AOD.07787075._000017.pool.root.1][0m
[33;1m2016-06-18 20:40:47,697 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 20:42:01,174 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:42:01,906 INFO [File mc15_13TeV:AOD.07787075._000024.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1402.04727793 seconds][0m
[32;1m2016-06-18 20:42:01,906 INFO [Starting the download of mc15_13TeV:AOD.07787075._000031.pool.root.1][0m
[32;1m2016-06-18 20:43:44,255 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 20:43:44,962 INFO [File mc15_13TeV:AOD.07787075._000016.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1443.93537712 seconds][0m
[32;1m2016-06-18 20:43:44,963 INFO [Starting the download of mc15_13TeV:AOD.07787075._000018.pool.root.1][0m
[33;1m2016-06-18 20:43:51,870 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 20:44:01,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:07:57,006 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:07:58,069 INFO [File mc15_13TeV:AOD.07787075._000017.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1809.55001307 seconds][0m
[32;1m2016-06-18 21:07:58,070 INFO [Starting the download of mc15_13TeV:AOD.07787075._000020.pool.root.1][0m
[33;1m2016-06-18 21:10:59,047 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:12:40,294 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:12:41,035 INFO [File mc15_13TeV:AOD.07787075._000018.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1736.07202721 seconds][0m
[32;1m2016-06-18 21:12:41,035 INFO [Starting the download of mc15_13TeV:AOD.07787075._000019.pool.root.1][0m
[33;1m2016-06-18 21:12:44,872 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:48,865 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:52,808 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:12:56,797 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:13:00,717 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 21:14:13,346 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:14:14,057 INFO [File mc15_13TeV:AOD.07787075._000031.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1932.15072584 seconds][0m
[32;1m2016-06-18 21:14:14,057 INFO [Starting the download of mc15_13TeV:AOD.07787075._000035.pool.root.1][0m
[33;1m2016-06-18 21:16:01,281 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:36:19,776 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:36:20,651 INFO [File mc15_13TeV:AOD.07787075._000020.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1702.58084106 seconds][0m
[32;1m2016-06-18 21:36:20,651 INFO [Starting the download of mc15_13TeV:AOD.07787075._000025.pool.root.1][0m
[33;1m2016-06-18 21:39:19,710 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:40:38,774 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 21:40:39,508 INFO [File mc15_13TeV:AOD.07787075._000019.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1678.47296882 seconds][0m
[32;1m2016-06-18 21:40:39,508 INFO [Starting the download of mc15_13TeV:AOD.07787075._000022.pool.root.1][0m
[33;1m2016-06-18 21:43:38,505 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE]
        [StatusOfGetRequest]
        [ETIMEDOUT]
        httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 21:44:14,839 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 21:44:15,709 INFO [File mc15_13TeV:AOD.07787075._000035.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1801.65164685 seconds][0m
[32;1m2016-06-18 21:44:15,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000038.pool.root.1][0m
[33;1m2016-06-18 21:44:21,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se05.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[33;1m2016-06-18 21:44:31,331 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: Could not open source: globus_xio: Unable to connect to se04.esc.qmul.ac.uk:2811 globus_xio: System error in connect: Connection refused globus_xio: A system call failed: Connection refused][0m
[32;1m2016-06-18 22:07:58,031 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:07:58,977 INFO [File mc15_13TeV:AOD.07787075._000025.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1898.32642102 seconds][0m
[32;1m2016-06-18 22:07:58,978 INFO [Starting the download of mc15_13TeV:AOD.07787075._000026.pool.root.1][0m
[32;1m2016-06-18 22:10:01,725 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:10:02,577 INFO [File mc15_13TeV:AOD.07787075._000022.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1763.06805682 seconds][0m
[32;1m2016-06-18 22:10:02,577 INFO [Starting the download of mc15_13TeV:AOD.07787075._000023.pool.root.1][0m
[33;1m2016-06-18 22:10:57,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:12:01,940 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded from UKI-LT2-QMUL_DATADISK][0m
[32;1m2016-06-18 22:12:02,640 INFO [File mc15_13TeV:AOD.07787075._000038.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1666.92999101 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000003.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[33;1m2016-06-18 22:13:03,228 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:30:20,655 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:30:21,708 INFO [File mc15_13TeV:AOD.07787075._000026.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1342.73055696 seconds][0m
[32;1m2016-06-18 22:30:21,709 INFO [Starting the download of mc15_13TeV:AOD.07787075._000027.pool.root.1][0m
[32;1m2016-06-18 22:31:43,652 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:31:44,417 INFO [File mc15_13TeV:AOD.07787075._000023.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1301.840518 seconds][0m
[32;1m2016-06-18 22:31:44,418 INFO [Starting the download of mc15_13TeV:AOD.07787075._000029.pool.root.1][0m
[33;1m2016-06-18 22:33:20,693 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 22:34:43,338 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 22:58:49,731 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 22:58:50,692 INFO [File mc15_13TeV:AOD.07787075._000027.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1708.98274994 seconds][0m
[32;1m2016-06-18 22:58:50,692 INFO [Starting the download of mc15_13TeV:AOD.07787075._000028.pool.root.1][0m
[32;1m2016-06-18 23:01:39,524 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:01:40,246 INFO [File mc15_13TeV:AOD.07787075._000029.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1795.82815194 seconds][0m
[32;1m2016-06-18 23:01:40,246 INFO [Starting the download of mc15_13TeV:AOD.07787075._000037.pool.root.1][0m
[33;1m2016-06-18 23:01:49,569 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:04:39,307 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:24:42,104 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:24:42,997 INFO [File mc15_13TeV:AOD.07787075._000037.pool.root.1 successfully downloaded. 3.9 GB bytes downloaded in 1382.75059414 seconds][0m
[32;1m2016-06-18 23:24:42,997 INFO [Starting the download of mc15_13TeV:AOD.07787075._000040.pool.root.1][0m
[32;1m2016-06-18 23:26:13,975 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:26:14,675 INFO [File mc15_13TeV:AOD.07787075._000028.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1643.98309779 seconds][0m
[32;1m2016-06-18 23:26:14,675 INFO [Starting the download of mc15_13TeV:AOD.07787075._000030.pool.root.1][0m
[33;1m2016-06-18 23:27:43,683 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[33;1m2016-06-18 23:29:13,601 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-18 23:41:29,306 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:41:30,184 INFO [File mc15_13TeV:AOD.07787075._000040.pool.root.1 successfully downloaded. 2.7 GB bytes downloaded in 1007.18631005 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000001.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-18 23:52:33,220 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-18 23:52:34,316 INFO [File mc15_13TeV:AOD.07787075._000030.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1579.64014387 seconds][0m
[32;1m2016-06-18 23:52:34,316 INFO [Starting the download of mc15_13TeV:AOD.07787075._000032.pool.root.1][0m
[33;1m2016-06-18 23:55:33,224 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:18:47,533 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:18:48,584 INFO [File mc15_13TeV:AOD.07787075._000032.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1574.2682879 seconds][0m
[32;1m2016-06-19 00:18:48,585 INFO [Starting the download of mc15_13TeV:AOD.07787075._000033.pool.root.1][0m
[33;1m2016-06-19 00:21:47,434 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 00:45:02,570 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 00:45:03,900 INFO [File mc15_13TeV:AOD.07787075._000033.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1575.31498194 seconds][0m
[32;1m2016-06-19 00:45:03,900 INFO [Starting the download of mc15_13TeV:AOD.07787075._000034.pool.root.1][0m
[33;1m2016-06-19 00:48:04,971 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:09:54,777 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:09:55,847 INFO [File mc15_13TeV:AOD.07787075._000034.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1491.94715786 seconds][0m
[32;1m2016-06-19 01:09:55,848 INFO [Starting the download of mc15_13TeV:AOD.07787075._000036.pool.root.1][0m
[33;1m2016-06-19 01:12:54,892 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:34:38,077 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 01:34:38,972 INFO [File mc15_13TeV:AOD.07787075._000036.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1483.12419295 seconds][0m
[32;1m2016-06-19 01:34:38,972 INFO [Starting the download of mc15_13TeV:AOD.07787075._000039.pool.root.1][0m
[33;1m2016-06-19 01:37:38,152 WARNING [The requested service is not available at the moment.
Details: An unknown exception occurred.
Details: SOURCE SRM_GET_TURL srm-ifce err: Connection timed out, err: [SE][StatusOfGetRequest][ETIMEDOUT] httpg://dcsrm.usatlas.bnl.gov:8443/srm/managerv2: User timeout over][0m
[32;1m2016-06-19 01:59:59,061 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded from BNL-OSG2_MCTAPE][0m
[32;1m2016-06-19 02:00:00,149 INFO [File mc15_13TeV:AOD.07787075._000039.pool.root.1 successfully downloaded. 4.4 GB bytes downloaded in 1521.17686605 seconds][0m
./mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282/AOD.07787075._000002.pool.root.1.part already exists, probably from a failed attempt. Will remove it
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
File downloaded. Will be validated
File validated
[32;1m2016-06-19 02:00:01,797 INFO [Download operation for mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282 done][0m
----------------------------------
Download summary
----------------------------------------
DID mc15_13TeV:mc15_13TeV.304805.MadGraphPythia8EvtGen_A14NNPDF23LO_HSS_LLP_mH200_mS25_lt5m.merge.AOD.e4754_s2698_r7146_r6282
Total files :                                40
Downloaded files :                           40
Files already found locally :                 0
Files that cannot be downloaded :             0");
            return s;
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

        public static Dictionary<string, string> AddCheckoutFromRevisionTrunk(this Dictionary<string, string> dict, string packagePath, string revision)
        {
            return dict
                .AddEntry(string.Format("rc checkout_pkg {0}@{1}", packagePath, revision), @"checking out trunk@704382
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
                .AddEntry(string.Format("mv {1}@{0} {1}", revision, packagePath.Split('/').Last()), "")
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
