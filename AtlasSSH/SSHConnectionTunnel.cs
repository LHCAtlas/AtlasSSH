using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    /// <summary>
    /// A wrapper for a connection that enables you to use a "tunnel" syntax to connect
    /// between various machines. The following are all valid as arguments:
    ///     user@machine.com - Opens a connection to machine
    ///     user@machine.com -> user@machine2.com - opens a connection to machine 2 via a tunnel through machine 1
    ///     -tunneling as many hops as you want-
    /// 
    /// Connections can be added at anytime. So you can make a connection, do a few things, and then tunnel.
    /// If you issue an "exit" and a connection closes, this will not properly detect that.
    /// 
    /// Make sure to dispose of this object. All connections in the stack will be closed.
    /// 
    /// The connections aren't actually made until you access the Connection object.
    /// 
    /// Each connection tunnel isn't a seeprate connection, so it doesn't make sense to iterate through them.
    /// </summary>
    public class SSHConnectionTunnel : IEnumerable<SSHConnection>, IDisposable
    {
        /// <summary>
        /// Use a connection string like "user@machine1.com" or "user@machine1.com -> user@machine2.com" to
        /// make a connection via tunneling (or not!)
        /// </summary>
        /// <param name="connectionString"></param>
        public SSHConnectionTunnel(string connectionString = null)
        {
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                Add(connectionString);
            }
        }

        /// <summary>
        /// Used to hold connection strings
        /// </summary>
        private List<string> _machineConnectionStrings = new List<string>();

        /// <summary>
        /// Add a new tunnel. Use the format "user@machine.com" or "user@machine.com -> user@Machine2.com", etc.
        /// </summary>
        /// <param name="connection"></param>
        public void Add(string connection)
        {
            var mlist = connection
                .Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim());

            foreach (var c in mlist)
            {
                AddSingleConnection(c);
            }
        }

        /// <summary>
        /// Add a single connection to the list. If we are already opened, then we add another one.
        /// </summary>
        /// <param name="c"></param>
        private void AddSingleConnection(string c)
        {
            if (_deepestConnection == null)
            {
                _machineConnectionStrings.Add(c);
            }
            else
            {
                MakeSSHTunnelConnection(c);
            }
        }

        /// <summary>
        /// Returns the SSH connection to the remote machine via tunnels. The first time this is accessed
        /// the connection will be made.
        /// </summary>
        public SSHConnection Connection
        {
            get
            {
                var c = GetDeepestConnection();
                return c;
            }
        }

        /// <summary>
        /// The deepest connection - makes for easy access
        /// </summary>
        private SSHConnection _deepestConnection = null;

        /// <summary>
        /// List of connections that we have, each one feeding to the next.
        /// </summary>
        private List<IDisposable> _connectionStack = null;

        /// <summary>
        /// Make the connection if it hasn't been made already
        /// </summary>
        /// <returns></returns>
        private SSHConnection GetDeepestConnection()
        {
            MakeConnections();
            if (_deepestConnection == null)
            {
                throw new InvalidOperationException("Attempt to use the SSHTunnelConnection without initalizing any connections");
            }
            return _deepestConnection;
        }

        /// <summary>
        /// Called to make the connections
        /// </summary>
        private void MakeConnections()
        {
            if (_deepestConnection == null && _machineConnectionStrings.Count > 0)
            {
                _connectionStack = new List<IDisposable>();
                var userMachineInfo = ExtractUserAndMachine(_machineConnectionStrings[0]);
                _deepestConnection = new SSHConnection(userMachineInfo.Item2, userMachineInfo.Item1);

                foreach (var cinfo in _machineConnectionStrings.Skip(1))
                {
                    MakeSSHTunnelConnection(cinfo);
                }

                // We don't need to track the connection strings any more.
                _machineConnectionStrings = null;
            }
        }

        /// <summary>
        /// Given the connections already are made, add one to the stack.
        /// </summary>
        /// <param name="cinfo"></param>
        private void MakeSSHTunnelConnection(string cinfo)
        {
            var cUserMachine = ExtractUserAndMachine(cinfo);
            _connectionStack.Add(_deepestConnection.SSHTo(cUserMachine.Item2, cUserMachine.Item1));
        }

        /// <summary>
        /// Extract the username and the machine from the connection string
        /// </summary>
        /// <param name="v"></param>
        /// <returns>Tuple with 0 being the user and 1 the machine</returns>
        private Tuple<string, string> ExtractUserAndMachine(string v)
        {
            var t = v.Split('@');
            if (t.Length != 2)
            {
                throw new ArgumentException($"Looking for a SSH connection in the form of user@machine, but found '{v}' instead!");
            }
            return Tuple.Create(t[0].Trim(), t[1].Trim());
        }

        /// <summary>
        /// Get the enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<SSHConnection> GetEnumerator()
        {
            throw new InvalidOperationException("The tunnels are seperate SSH Connections, so you can't really enumerate through them");
        }

        /// <summary>
        /// Return the enumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new InvalidOperationException("The tunnels are seperate SSH Connections, so you can't really enumerate through them");
        }

        /// <summary>
        /// Close off everything.
        /// </summary>
        public void Dispose()
        {
            if (_connectionStack != null)
            {
                foreach (var c in _connectionStack.Reverse<IDisposable>())
                {
                    c.Dispose();
                }
            }
        }
    }
}
