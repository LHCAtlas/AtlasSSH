using AtlasWorkFlows;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Fetch a dataset from the grid using a location profile. Passwords must be set in the credential cache for this to work!
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDDataset")]
    public class GetGRIDDataset : PSCmdlet
    {
        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory=true, HelpMessage="Rucio dataset name to fetch", ValueFromPipeline=true, Position=1)]
        public string DatasetName { get; set; }

        /// <summary>
        /// The location against which we will be doing all the download and fetching work
        /// </summary>
        private Location _location = null;

        /// <summary>
        /// Fast listener
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

        /// <summary>
        /// Initialize the context for making whatever connections we are going to need.
        /// </summary>
        protected override void BeginProcessing()
        {
        }

        /// <summary>
        /// Fetch a particular dataset.
        /// </summary>
        protected override void ProcessRecord()
        {
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                var r = GRIDDatasetLocator.FetchDatasetUris(DatasetName, fname => DisplayStatus(fname));
                foreach (var ds in r)
                {
                    WriteObject(ds);
                }
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="fname"></param>
        private void DisplayStatus(string fname)
        {
            var pr = new ProgressRecord(1, "Downloading", fname);
            WriteProgress(pr);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();
        }
    }
}
