using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public static class SSHConnectionUtils
    {
        /// <summary>
        /// Use apply to make repeated application of things to a connection easy to read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="what"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static SSHConnection Apply<T>(this SSHConnection connection, IEnumerable<T> what, Action<SSHConnection, T> doit)
        {
            foreach (var w in what)
            {
                doit(connection, w);
            }
            return connection;
        }

        /// <summary>
        /// Helper function to use in the middle of this thing
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static SSHConnection Apply(this SSHConnection connection, Action doit)
        {
            doit();
            return connection;
        }

    }
}
