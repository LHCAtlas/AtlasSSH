using CredentialManagement;
using System.Linq;
using System.Text.RegularExpressions;

namespace AtlasSSH
{
    public static class CredentialUtils
    {
        /// <summary>
        /// Look for user credentials in the generic windows credential store on this machine.
        /// If we can't find them, use some heruistics to see if we can find them (tev10 => tev).
        /// </summary>
        /// <param name="host"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static Credential FetchUserCredentials(string host, string userName)
        {
            using (var sclist = new CredentialSet(host))
            {
                var passwordInfo = sclist.Load().Where(c => c.Username == userName).FirstOrDefault();
                if (passwordInfo == null)
                {
                    // See if this can be turned into a generic machine name
                    var m = Regex.Match(host, @"^(?<mroot>[^0-9\.]+)(?<mnumber>[0-9]+)(?<mfinal>\..+)$");
                    if (m.Success && !string.IsNullOrWhiteSpace(m.Groups["mnumber"].Value))
                    {
                        var newMachineName = m.Groups["mroot"].Value + m.Groups["mfinal"].Value;
                        using (var sclistNew = new CredentialSet(newMachineName))
                        {
                            passwordInfo = sclist.Load().Where(c => c.Username == userName).FirstOrDefault();
                        }
                    }
                }
                return passwordInfo;
            }
        }
    }
}
