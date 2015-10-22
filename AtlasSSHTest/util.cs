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
            return dict;
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
