using DNS.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        static CacheObject<string> _ipDNSLookupCache = new CacheObject<string>();

        /// <summary>
        /// For testing
        /// </summary>
        /// <param name="name"></param>
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

        private static Lazy<DnsClient> _clientDNS = new Lazy<DnsClient>(() => new DnsClient("8.8.8.8"));

        /// <summary>
        /// Fetch the host IP name
        /// </summary>
        public static string FindLocalIpName()
        {
            if (_ipNameFound)
                return _ipName;

            return _ipDNSLookupCache.Get(() =>
            {
                // We have to get the IP address that the external world sees, rather than the one we see.
                // This is to deal with internal DNS reverse lookup problems.
                using (var wc = new WebClient())
                {
                    // Get the external ip address. This tells us where we are located, generally.
                    string iptext = "";
                    try
                    {
                        iptext = wc.DownloadString("http://ipv4bot.whatismyipaddress.com/");
                        Trace.WriteLine($"IP address seen by external world is {iptext}.", "FindLocalIpName");
                    }
                    catch (WebException)
                    {
                        Trace.WriteLine("Unable to reach whatsmyipaddress.com - perhaps not connected to the Internet?", "FindLocalIpName");
                        return "";
                    }

                    // Next, do a reverse lookup in DNS.
                    string rs = "";
                    try
                    {
                        rs = _clientDNS.Value.Reverse(iptext).Result;
                        Trace.WriteLine($"Reverse lookup of ip address is {rs}.", "FindLocalIpName");
                        return rs;
                    }
                    catch
                    {

                    }

                    // Can we get something out of windows for this now?\
                    try
                    {
                        var address = Dns.GetHostEntry(iptext);
                        if (address == null)
                        {
                            Trace.WriteLine("Unable to find any DNS name for this computer.", "FindLocalIpName");
                            return "";
                        }

                        Trace.WriteLine(string.Format("DNS name for this computer is '{0}'", address.HostName), "FindLocalIpName");
                        return address.HostName;
                    }
                    catch
                    {
                        Trace.WriteLine($"Unable to reverse look up {iptext}.");
                        return "";
                    }
                }
            });
        }
    }
}
