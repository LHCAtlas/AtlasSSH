using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands
{
    /// <summary>
    /// Using our knowledge of the layout of the ATLAS job options svn package, fetch down the appropriate
    /// job options file.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ATLASMCJobOptions")]
    public class GetATLASMCJobOptions : PSCmdlet
    {
        /// <summary>
        /// The MC run number
        /// </summary>
        [Parameter(HelpMessage = "Run number to fetch", ValueFromPipeline = true ,Mandatory = true, Position = 1)]
        public int MCJobNumber;

        /// <summary>
        /// Get/Set the mc campaign.
        /// </summary>
        [Parameter(HelpMessage = "MC Campaign. Defaults to MC15 (MC10, MC12, etc. are possible)", Mandatory = false)]
        public string MCCampaign { get; set; }

        public GetATLASMCJobOptions()
        {
            MCCampaign = "MC15";
        }

        /// <summary>
        /// Client we will use for quick access
        /// </summary>
        public SvnClient _client = new SvnClient();

        /// <summary>
        /// List of the DSID's that this MC job options colleciton knows about.
        /// </summary>
        public Collection<SvnListEventArgs> _dsidList = null;

        /// <summary>
        /// Create the SSH link
        /// </summary>
        protected override void BeginProcessing()
        {
            // We will need to target where we are going for svn here. Get the top level directory
            // and cache it.


            //client.GetList(target, out _dsidList);

            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            // Make sure this is a 6 digit number as a string.
            var runID = MCJobNumber.ToString("D6");

            // First, for this run number, see if we can't figure out what DSID it is going to be
            // cached under.

            var DSIDDirectory = $"DSID{runID.Substring(0, 3)}xxx";
            var dsidListTarget = FetchListing(BuildTarget($"share/{DSIDDirectory}"));

            var myMCFile = dsidListTarget
                .Where(ads => ads.Name.Contains(runID))
                .ToArray();
            if (myMCFile.Length == 0)
            {
                var err = new ArgumentException($"Unable to find dataset run number for {runID}.");
                WriteError(new ErrorRecord(err, "NoSuchDataset", ErrorCategory.InvalidArgument, null));
            }
            if (myMCFile.Length != 1)
            {
                var err = new ArgumentException($"Found multiple dataset files for {runID} - giving up.");
                WriteError(new ErrorRecord(err, "MoreThanOneDataset", ErrorCategory.InvalidArgument, null));
            }
            var ds = myMCFile[0];

            // Next, fetch the file down.
            var targetTempPath = System.IO.Path.GetTempFileName();
            var args = new SvnExportArgs() { Overwrite = true };
            _client.Export(BuildTarget(ds.Uri), targetTempPath, args);

            // And read the temp file back.
            using (var rdr = System.IO.File.OpenText(targetTempPath))
            {
                string line = "";
                while (line != null)
                {
                    line = rdr.ReadLine();
                    if (line != null)
                    {
                        WriteObject(line);
                    }
                }
            }

            base.ProcessRecord();
        }

        /// <summary>
        /// Returns a list of the contents of the directory
        /// </summary>
        /// <param name="svnTarget"></param>
        /// <returns></returns>
        private Collection<SvnListEventArgs> FetchListing(SvnTarget svnTarget)
        {
            var result = new Collection<SvnListEventArgs>();
            _client.GetList(svnTarget, out result);
            return result;
        }

        /// <summary>
        /// Build a target for a particular directory or file from the base given by the current
        /// arguments.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private SvnTarget BuildTarget(string path)
        {
            SvnTarget target;
            var url = $"https://svn.cern.ch/reps/atlasoff/Generators/{MCCampaign}JobOptions/trunk/{path}";
            if (!SvnTarget.TryParse(url, out target))
            {
                var err = new ArgumentException($"Unable to parse svn url {url}.");
                WriteError(new ErrorRecord(err, "SVNUrlError", ErrorCategory.InvalidArgument, null));
                throw err;
            }

            return target;
        }

        /// <summary>
        /// Build the target from a uri.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private SvnTarget BuildTarget (Uri path)
        {
            SvnTarget target;
            SvnTarget.TryParse(path.OriginalString, out target);
            return target;
        }

        /// <summary>
        /// Kill off the SSH link.
        /// </summary>
        protected override void EndProcessing()
        {
            base.EndProcessing();
        }
    }
}
