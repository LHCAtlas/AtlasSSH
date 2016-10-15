using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;
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
        [Parameter(HelpMessage = "Run number to fetch", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public int MCJobNumber;

        /// <summary>
        /// Get/Set the mc campaign.
        /// </summary>
        [Parameter(HelpMessage = "MC Campaign. Defaults to MC15 (MC10, MC12, etc. are possible)", Mandatory = false)]
        public string MCCampaign { get; set; }

        /// <summary>
        /// Get/Set if we are going to expand include lines.
        /// </summary>
        [Parameter(HelpMessage = "Should include files be expanded inline?", Mandatory = false)]
        public SwitchParameter ExpandIncludeFiles { get; set; }

        public GetATLASMCJobOptions()
        {
            MCCampaign = "MC15";
        }

        /// <summary>
        /// Client we will use for quick access
        /// </summary>
        public SvnClient _client = new SvnClient();

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

            IEnumerable<string> lines = GetSvnFileLines(BuildTarget(ds.Uri));

            foreach (var line in lines)
            {
                WriteObject(line);
            }

            base.ProcessRecord();
        }

        /// <summary>
        /// Given a listing, return an item.
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private IEnumerable<string> GetSvnFileLines(SvnTarget ds)
        {
            // Next, fetch the file down.
            var targetTempPath = Path.GetTempFileName();
            var args = new SvnExportArgs() { Overwrite = true };
            WriteVerbose($"Downloading svn file {ds.TargetName}");
            _client.Export(ds, targetTempPath, args);

            // Transfer process the lines
            var lines = new FileInfo(targetTempPath)
                .ReadLines()
                .SelectMany(l => ReplaceIncludeFiles(l, ExpandIncludeFiles));
            return lines;
        }

        /// <summary>
        /// Returns a list of the contents of the directory
        /// </summary>
        /// <param name="svnTarget"></param>
        /// <returns></returns>
        private Collection<SvnListEventArgs> FetchListing(SvnTarget svnTarget, SvnListArgs args = null)
        {
            args = args == null ? new SvnListArgs() : args;
            var result = new Collection<SvnListEventArgs>();
            WriteVerbose($"Fetching svn listing from {svnTarget.TargetName}");
            _client.GetList(svnTarget, args, out result);
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
        private SvnTarget BuildTarget(Uri path)
        {
            SvnTarget target;
            SvnTarget.TryParse(path.OriginalString, out target);
            return target;
        }

        /// <summary>
        /// If we have an include, try to fetch the include files and download them.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        public IEnumerable<string> ReplaceIncludeFiles(string l, bool expandIncludeFiles)
        {
            if (!expandIncludeFiles)
            {
                yield return l;
            }
            else
            {
                var includeInfo = ExtractIncludeInformation(l);
                if (includeInfo == null)
                {
                    yield return l;
                }
                else
                {
                    yield return "#";
                    yield return $"# Including File {l}";
                    yield return "#";
                    var lines = FetchIncludeContents(includeInfo)
                        .SelectMany(ll => ReplaceIncludeFiles(ll, expandIncludeFiles));
                    foreach (var ln in lines)
                    {
                        yield return ln;
                    }
                    yield return "#";
                    yield return $"# Done including file {l}";
                    yield return "#";
                }
            }
        }


        /// <summary>
        /// Return the contents of an include file.
        /// </summary>
        /// <param name="includeInfo"></param>
        /// <returns></returns>
        private IEnumerable<string> FetchIncludeContents(IncludeInfo includeInfo)
        {
            // Find the include file
            var f = FindIncludeFile(includeInfo.includeName);
            if (f == null)
            {
                yield return "# *** Unable to find include file in svn!";
            } else
            {
                foreach (var l in GetSvnFileLines(f))
                {
                    yield return l;
                }
            }
        }

        /// <summary>
        /// Find an include file by looking at the target. Return null if it doesn't work.
        /// </summary>
        /// <param name="includeName"></param>
        /// <returns></returns>
        private SvnTarget FindIncludeFile(string includeName)
        {
            var f = ScanCache(includeName);
            if (f == null)
            {
                ReloadCache();
                f = ScanCache(includeName);
            }

            return f;
        }

        /// <summary>
        /// Update the cache with all the files.
        /// </summary>
        private void ReloadCache()
        {
            // Build the ls target.
            var lsTargetCommon = BuildTarget("common");
            var lsTargetNonStandard = BuildTarget("nonStandard");

            // Now do a recursve ls to get back everything.
            var opt = new SvnListArgs() { Depth = SvnDepth.Infinity };
            var linesCommon = FetchListing(lsTargetCommon, opt);
            var linesNonStandard = FetchListing(lsTargetNonStandard, opt);

            // Next, write out the cache
            var cacheFile = GetCacheFileInfo();
            using (var writer = cacheFile.CreateText())
            {
                foreach (var item in linesCommon.Concat(linesNonStandard))
                {
                    writer.WriteLine(item.Uri.OriginalString);
                }
            }
        }

        /// <summary>
        /// Return a reference to the cache file.
        /// </summary>
        /// <returns></returns>
        private FileInfo GetCacheFileInfo()
        {
            var f = new FileInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"ATLASGeneratorSVNCaches\include_file_cache.txt"));
            if (!f.Directory.Exists)
            {
                f.Directory.Create();
            }
            return f;
        }

        /// <summary>
        /// Scan the cache of files to see if we can find one that fits. Return a
        /// svn target for downloading.
        /// </summary>
        /// <param name="includeName"></param>
        /// <returns></returns>
        private SvnTarget ScanCache(string includeName)
        {
            // Strip off the first job options.
            var stub = $"{MCCampaign}JobOptions/";
            if (!includeName.StartsWith(stub))
            {
                // We definately don't know how to look outside of the current campaign.
                return null;
            }
            var rawIncludeFile = includeName.Substring(stub.Length-1);

            // Now we have to see if there is a sub-directory in there. If there is, then our search is going to be a little more
            // complex.
            Func<string, bool> matchInclude = line => line.Contains(rawIncludeFile);
            if (rawIncludeFile.Substring(1).Contains("/"))
            {
                var subDir = "/" + rawIncludeFile.Split('/')[1] + "/";
                var restOfIt = rawIncludeFile.Substring(subDir.Length - 1);
                matchInclude = line => line.Contains(subDir) && line.Contains(restOfIt);
            }

            // Now look for it in the list.
            var cacheFile = GetCacheFileInfo();
            var f = cacheFile.ReadLines()
                .Where(l => matchInclude.Invoke(l))
                .FirstOrDefault();

            if (f == null)
            {
                return null;
            }
            return BuildTarget(new Uri(f));
        }

        /// <summary>
        /// Basic info about an include
        /// </summary>
        private class IncludeInfo
        {
            public string includeName;
        }

        private static Regex _findInclude = new Regex(@"include\(['\""]([^'""]+)['""]\)");

        /// <summary>
        /// Inspect the line for a python include line.
        /// </summary>
        /// <param name="l"></param>
        /// <returns></returns>
        private static IncludeInfo ExtractIncludeInformation(string l)
        {
            // Skip commented out lines.
            if (l.Trim().StartsWith("#"))
            {
                return null;
            }

            // Now look for a nice include line.
            var m = _findInclude.Match(l);
            return m.Success
                ? new IncludeInfo() { includeName = m.Groups[1].Value }
                : null;
        }
    }
}
