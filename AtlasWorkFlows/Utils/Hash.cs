using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Help with string hashes
    /// </summary>
    internal static class Hash
    {
        /// <summary>
        /// Hash algorithm (which will work no matter what machine or bitness we are in).
        /// </summary>
        private static Lazy<MD5> _hasher = new Lazy<MD5>(() => MD5.Create());

        /// <summary>
        /// Computes a unique cross-platform/bitness hash for a string.
        /// </summary>
        /// <param name="jobspec"></param>
        /// <returns></returns>
        public static string ComputeMD5Hash(this string jobspec)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = _hasher.Value.ComputeHash(Encoding.UTF8.GetBytes(jobspec));
            var sBuilder = data.Where((x, i) => i % 4 == 0).Aggregate(new StringBuilder(), (bld, d) => { bld.Append(d.ToString("X2")); return bld; });

            Trace.WriteLine($"Hash {sBuilder.ToString()} for job spec {jobspec}");
            return sBuilder.ToString();
        }
    }
}
