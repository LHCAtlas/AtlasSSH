using AtlasSSH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LongRunTest
{
    class Program
    {
        /// <summary>
        /// Make a long connection to a remote host. This is to help with debugging
        /// broken connections.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                throw new InvalidOperationException("Usage <prog> host username");
            }
            var c = new SSHConnection(args[0], args[1]);
            Console.WriteLine("Sending a ls command");
            c.ExecuteLinuxCommand("ls -l", s => Console.WriteLine(s));
            Console.WriteLine("Sleeping for 5 minutes");
            c.ExecuteLinuxCommand($"sleep {60 * 5}", s => Console.WriteLine(s));
            Console.WriteLine("Done!");
        }
    }
}
