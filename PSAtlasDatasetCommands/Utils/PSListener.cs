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
        /// <summary>
        /// Use with a "Using" statement to make sure the current logging state is off.
        /// It will restore to the previous state when it goes out of scope and the Dispose method
        /// is called.
        /// </summary>
        private class PSListenerPause : IDisposable
        {
            private readonly PSListener _listener;
            private readonly bool _origianlState;

            /// <summary>
            /// Init with the listener.
            /// </summary>
            /// <param name="listener"></param>
            public PSListenerPause (PSListener listener)
            {
                _listener = listener;
                _origianlState = listener.LogState;
                _listener.LogState = false;
            }

            public void Dispose()
            {
                _listener.LogState = _origianlState;
            }
        }

        private PSCmdlet _host;

        /// <summary>
        /// The current logging state. True if mesages are being written to the Verbose stream, false
        /// otherwise.
        /// </summary>
        public bool LogState { get; private set; }

        public PSListener(PSCmdlet c)
        {
            _host = c;
            LogState = true;
        }

        /// <summary>
        /// Pause this listener from running
        /// </summary>
        /// <returns></returns>
        public IDisposable PauseListening()
        {
            return new PSListenerPause(this);
        }

        /// <summary>
        /// Write out a line - this is the most basic of the methods, everything else feeds through it.
        /// </summary>
        /// <param name="message"></param>
        public override void WriteLine(string message)
        {
            // Disabled till we can figure out how to better do this: http://stackoverflow.com/questions/41157349/how-to-avoid-writeverbose-writeobject-bad-thread
            if (LogState)
            {
                _host.WriteVerbose(message);
            }
        }
    }
}
