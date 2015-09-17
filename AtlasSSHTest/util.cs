using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
