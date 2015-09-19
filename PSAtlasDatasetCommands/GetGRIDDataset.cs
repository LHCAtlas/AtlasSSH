using AtlasWorkFlows;
using System;
using System.Collections.Generic;
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
            var r = GRIDDatasetLocator.FetchDatasetUris(DatasetName);
            foreach (var ds in r)
            {
                WriteObject(r);
            }
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
