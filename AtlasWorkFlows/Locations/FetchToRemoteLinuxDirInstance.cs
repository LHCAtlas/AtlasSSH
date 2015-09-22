using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Look for a remote Linux instance given the parameter bag, and configure 
    /// </summary>
    static class FetchToRemoteLinuxDirInstance
    {
        /// <summary>
        /// Allows you to set the fetcher so it can be used as a test
        /// </summary>
        public static IFetchToRemoteLinuxDir _test = null;

        /// <summary>
        /// Fetch a remote instance for the Linux fetcher. Throw if we don't know how to build it.
        /// </summary>
        /// <param name="props">Property bag to configure the Linux instance.</param>
        /// <returns></returns>
        public static IFetchToRemoteLinuxDir FetchRemoteLinuxInstance (this Dictionary<string, string> props)
        {
            IFetchToRemoteLinuxDir fetcher = null;

            if (!props.ContainsKey("LinuxFetcherType")) {
                throw new ArgumentException("The configuration doesn't know about 'LinuxFetcherType', so I can't create one!");
            }
            var name = props["LinuxFetcherType"];

            if (name == "LinuxFetcher") {
                fetcher = new LinuxFetcher(props["LinuxHost"], props["LinuxUserName"]);
            } else if (name == "Test") {
                fetcher = _test;
            }

            // Double check
            if (fetcher == null && !(name == "Test"))
            {
                throw new ArgumentException(string.Format("Do not know how to instantiate Fetcher of type '{0}'.", name));
            }

            return fetcher;
        }
    }
}
