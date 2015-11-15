using AtlasWorkFlows;
using PSAtlasDatasetCommands.Utils;
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
        /// How many files should be downloaded. They are sorted before making a determination (so it isn't random).
        /// </summary>
        [Parameter(HelpMessage="Maximum number of files to fetch")]
        public int nFiles { get; set; }

        /// <summary>
        /// The location where we should download the dataset to.
        /// </summary>
        [Parameter(HelpMessage="Location where the dataset should be downloaded to")]
        public string Location { get; set; }

        /// <summary>
        /// If true, then when the destination is local, that will not be used.
        /// Ignored unless used with --Location.
        /// </summary>
        [Parameter(HelpMessage = "Do use any other intermediate locations (only valid with --Location)")]
        public SwitchParameter DoNotUseRelay { get; set; }

        [Parameter(HelpMessage = "Timeout in seconds between updates from the download. Defaults to 1 hour")]
        public int Timeout { get; set; }

        /// <summary>
        /// Setup defaults.
        /// </summary>
        public GetGRIDDataset()
        {
            DoNotUseRelay = false;
            Timeout = 3600;
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
                Func<string[], string[]> filter = nFiles == 0 ? (Func<string[],string[]>) null : flist => flist.OrderBy(f => f).Take(nFiles).ToArray();
                Uri[] r = null;
                if (!string.IsNullOrWhiteSpace(Location))
                {
                    using (var holder = GRIDDatasetLocator.SetLocationFilter(loc => false))
                    {
                        r = GRIDDatasetLocator.FetchDatasetUrisAtLocation(Location, DatasetName, fname => DisplayStatus(fname), fileFilter: filter, failNow: () => Stopping, timeoutDownloadSecs: Timeout);
                    }
                }
                else
                {
                    r = GRIDDatasetLocator.FetchDatasetUris(DatasetName, fname => DisplayStatus(fname), fileFilter: filter, failNow: () => Stopping, timeoutDownloadSecs: Timeout);
                }
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
