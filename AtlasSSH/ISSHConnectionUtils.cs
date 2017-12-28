using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public static class ISSHConnectionUtils
    {
        /// <summary>
        /// Use apply to make repeated application of things to a connection easy to read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="what"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static ISSHConnection Apply<T>(this ISSHConnection connection, IEnumerable<T> what, Action<ISSHConnection, T> doit)
        {
            foreach (var w in what)
            {
                doit(connection, w);
            }
            return connection;
        }

        /// <summary>
        /// Apply a async function to a connection.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connection"></param>
        /// <param name="what"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static async Task<ISSHConnection> ApplyAsync<T>(this ISSHConnection connection, IEnumerable<T> what, Func<ISSHConnection, T, Task> doit)
        {
            foreach (var w in what)
            {
                await doit(connection, w);
            }
            return connection;
        }

        /// <summary>
        /// Helper function to use in the middle of this thing
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="doit"></param>
        /// <returns></returns>
        public static ISSHConnection Apply(this ISSHConnection connection, Action doit)
        {
            doit();
            return connection;
        }

    }
}
