using AtlasSSH;
using AtlasWorkFlows.Utils;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static AtlasSSH.SSHConnection;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Helper things for SSH access.
    /// </summary>
    static class SSHUtils
    {
        public class SSHHostPair
        {
            public string Host;
            public string Username;
        }

        /// <summary>
        /// Generate the tunnel.
        /// </summary>
        /// <param name="connectionInfo"></param>
        /// <returns></returns>
        public static Tuple<SSHConnection, List<IDisposable>> MakeConnection(this SSHHostPair[] connectionInfo)
        {
            return Policy
                .Handle<UnableToCreateSSHTunnelException>()
                .WaitAndRetry(new [] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Execute(() =>
                {
                    SSHConnection r = null;
                    var l = new List<IDisposable>();
                    foreach (var pair in connectionInfo)
                    {
                        if (r == null)
                        {
                            Trace.WriteLine($"Creating new connection to {pair.Username}@{pair.Host}");
                            r = new SSHConnection(pair.Host, pair.Username);
                        }
                        else
                        {
                            Trace.WriteLine($"MakeConnection: Running ssh to tunnel through to {pair.Username}@{pair.Host}.");
                            l.Add(r.SSHTo(pair.Host, pair.Username));
                        }
                    }
                    return Tuple.Create(r, l);
                });
        }

        /// <summary>
        /// Thrown if we can't figure out how to parse the username/host string.
        /// </summary>
        [Serializable]
        public class InvalidHostSpecificationException : Exception
        {
            public InvalidHostSpecificationException() { }
            public InvalidHostSpecificationException(string message) : base(message) { }
            public InvalidHostSpecificationException(string message, Exception inner) : base(message, inner) { }
            protected InvalidHostSpecificationException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Given a string of username@host -> username@host return a chain of SSH specs
        /// </summary>
        /// <param name="sshChain">String to be parsed for usernames and hosts, seperated by "->"</param>
        /// <returns>List of username hosts in the array understood by MakeConnection</returns>
        public static SSHHostPair[] ParseHostPairChain(this string sshChain)
        {
            return sshChain
                .Split(new string[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Select(s => s.Split('@'))
                .Throw<string[]>(sarr => sarr.Length != 2, sarr => new InvalidHostSpecificationException($"The host specification string '{sshChain}' is in an invalid format (user@host -> user@host -> ...)"))
                .Select(sarr => new SSHHostPair() { Host = sarr[1], Username = sarr[0] })
                .ToArray();
        }
    }
}
