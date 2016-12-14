using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands.Utils
{
    /// <summary>
    /// Fast listener for a PS command - attaches to trace. For the -Verbose option! :-)
    /// </summary>
    class PSListener : TextWriterTraceListener
    {
        private PSCmdlet _host;
        public PSListener(PSCmdlet c)
        {
            _host = c;
        }

        /// <summary>
        /// Write out a line - this is the most basic of the methods, everything else feeds through it.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
           _host.WriteVerbose(message);
        }
    }

}
