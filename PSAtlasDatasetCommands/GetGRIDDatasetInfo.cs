using AtlasSSH;
using AtlasWorkFlows.Jobs;
using CredentialManagement;
using PSAtlasDatasetCommands.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using static AtlasSSH.AtlasCommands;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Return info about a GRID dataset. We query rucio to get the info
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "GRIDDatasetInfo")]
    public class GetGRIDDatasetInfo : PSCmdlet
    {
        /// <summary>
        /// Get/Set the name of the dataset we are being asked to fetch.
        /// </summary>
        [Parameter(Mandatory = true, HelpMessage = "Rucio dataset name to fetch", ValueFromPipeline = true, Position = 1)]
        public string DatasetName { get; set; }

        /// <summary>
        /// Hold onto the connection
        /// </summary>
        private SSHConnection _connection = null;

        /// <summary>
        /// Hold onto the grid credentials
        /// </summary>
        private Credential _gridCredentials = null;

        /// <summary>
        /// Fetch grid credentials for later use
        /// </summary>
        protected override void BeginProcessing()
        {
            // Setup for verbosity if we need it.
            var listener = new PSListener(this);
            Trace.Listeners.Add(listener);
            try
            {
                // Get the grid credentials
                _gridCredentials = new CredentialSet("GRID").Load().FirstOrDefault();
                if (_gridCredentials == null)
                {
                    throw new ArgumentException("Please create a generic windows credential with the target 'GRID' with the username as the rucio grid username and the password to be used with voms proxy init");
                }
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
        }

        /// <summary>
        /// The PS output object
        /// </summary>
        [Serializable]
        public class PSGRIDDatasetInfo
        {
            public string DatasetName;
            public int nFiles;
            public int TotalSizeMB;

            public GRIDFileInfo[] FileInfo;
        }

        /// <summary>
        /// Cache results so we don't have to re-ping.
        /// </summary>
        private Lazy<DiskCacheTyped<PSGRIDDatasetInfo>> _resultsCache = new Lazy<DiskCacheTyped<PSGRIDDatasetInfo>>(() => new DiskCacheTyped<PSGRIDDatasetInfo>("Get-GRIDDatasetInfo"));

        /// <summary>
        /// Process a single dataset and fetch the info about it.
        /// </summary>
        protected override void ProcessRecord()
        {
            var cHit = _resultsCache.Value[DatasetName] as PSGRIDDatasetInfo;
            if (cHit != null)
            {
                WriteObject(cHit);
            }
            else
            {
                // Setup for verbosity if we need it.
                var listener = new PSListener(this);
                Trace.Listeners.Add(listener);
                try
                {
                    // Where are we going to be doing query on - we need a machine.
                    var sm = JobParser.GetSubmissionMachine();

                    // Get the remove environment configured if it needs to be
                    if (_connection == null)
                    {
                        _connection = new SSHConnection(sm.MachineName, sm.Username);
                        _connection
                            .Apply(() => DisplayStatus("Setting up ATLAS"))
                            .setupATLAS()
                            .Apply(() => DisplayStatus("Setting up Rucio"))
                            .setupRucio(_gridCredentials.Username)
                            .Apply(() => DisplayStatus("Acquiring GRID credentials"))
                            .VomsProxyInit("atlas", failNow: () => Stopping);
                    }

                    // Great - get the info on this dataset.
                    var fileInfo = _connection
                        .Apply(() => DisplayStatus($"Checking for info on {DatasetName}."))
                        .FileInfoFromGRID(DatasetName, failNow: () => Stopping);

                    // Next, build the resulting thingy.
                    var r = new PSGRIDDatasetInfo()
                    {
                        DatasetName = DatasetName,
                        nFiles = fileInfo.Count,
                        TotalSizeMB = (int)fileInfo.Sum(fi => fi.size),
                        FileInfo = fileInfo.ToArray()
                    };
                    _resultsCache.Value[DatasetName] = r;
                    WriteObject(r);
                }
                finally
                {
                    Trace.Listeners.Remove(listener);
                }
            }
        }

        /// <summary>
        /// Called to build a status object
        /// </summary>
        /// <param name="fname"></param>
        private void DisplayStatus(string message)
        {
            var pr = new ProgressRecord(1, $"Getting Info For {DatasetName}", message);
            WriteProgress(pr);
        }

    }
}
