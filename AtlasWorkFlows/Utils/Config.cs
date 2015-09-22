using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Build config for various things.
    /// Eventually I hope most of this can be loaded dynamically. But for initial setup, this seems simplest.
    /// </summary>
    class Config
    {
        public static Dictionary<string, Dictionary<string, string>> GetLocationConfigs()
        {
            var r = new Dictionary<string, Dictionary<string, string>>();

            // CERN
            var c = new Dictionary<string, string>();
            c["DNSEndString"] = ".cern.ch";
            c["Name"] = "CERN";
            c["WindowsPath"] = @"\\uw01.myds.me\LLPData\GRIDDS";
            c["LinuxPath"] = "/LLPData/GRIDDS";
            c["LocationType"] = "LinuxWithWindowsReflector";
            c["LinuxHost"] = "pcatuw4.cern.ch";
            c["LinuxUserName"] = "gwatts";
            c["LinuxFetcherType"] = "LinuxFetcher";
            c["Priority"] = "10";

            r["CERN"] = c;

            // Local
            c = new Dictionary<string, string>();
            c["Name"] = "Local";
            c["Paths"] = @"D:\GRIDDS";
            c["LocationType"] = "LocalWindowsFilesystem";
            c["Priority"] = "100";

            c["LinuxFetcherType"] = "LinuxFetcher";
            c["LinuxHost"] = "tev01.phys.washington.edu";
            c["LinuxUserName"] = "gwatts";
            c["LinuxTempLocation"] = "/tmp/gwattsdownloads";

            r["Local"] = c;

            return r;
        }
    }
}
