using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Tools to help with understanding where this computer is currently located by IP.
    /// Basic assumption: Once this starts running, the computer's IP does not change.
    /// </summary>
    class IPLocationTests
    {
        static bool _ipNameFound = false;
        static string _ipName;

        static internal void SetIpName (string name)
        {
            _ipName = name;
            _ipNameFound = true;
        }

        /// <summary>
        /// Reset the IP name. Used for testing.
        /// </summary>
        static internal void ResetIpName()
        {
            _ipNameFound = false;
            _ipName = null;
        }

        /// <summary>
        /// Fetch the host IP name
        /// </summary>
        public static string FindLocalIpName()
        {
            if (_ipNameFound)
                return _ipName;

            // Do the look up. We have to be a little careful here since the HostName isn't (often) what we want.
            var address = Dns.GetHostAddresses(Dns.GetHostName())
                .Where(addr => !IPAddress.IsLoopback(addr))
                .Select(addr => Dns.GetHostEntry(addr))
                .FirstOrDefault();

            if (address == null)
                return "";

            return address.HostName;

#if false
            var name = Dns.GetHostName();
            SetIpName(name);
            return _ipName;
#endif
        }
    }
}
