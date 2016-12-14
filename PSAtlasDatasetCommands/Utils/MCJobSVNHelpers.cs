using SharpSvn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands.Utils
{
    class MCJobSVNHelpers
    {
        /// <summary>
        /// Build a target for a particular directory or file from the base given by the current
        /// arguments.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static SvnTarget BuildTarget(string path, string MCCampaign)
        {
            SvnTarget target;
            var url = $"https://svn.cern.ch/reps/atlasoff/Generators/{MCCampaign}JobOptions/trunk/{path}";
            if (!SvnTarget.TryParse(url, out target))
            {
                var err = new ArgumentException($"Unable to parse svn url {url}.");
                throw err;
            }

            return target;
        }

        /// <summary>
        /// The SVN client.
        /// </summary>
        private static Lazy<SvnClient> _client = new Lazy<SvnClient>();

        /// <summary>
        /// Returns a list of the contents of the directory
        /// </summary>
        /// <param name="svnTarget"></param>
        /// <returns></returns>
        public static Collection<SvnListEventArgs> FetchListing(SvnTarget svnTarget, SvnListArgs args = null)
        {
            args = args == null ? new SvnListArgs() : args;
            var result = new Collection<SvnListEventArgs>();
            _client.Value.GetList(svnTarget, args, out result);
            return result;
        }

        /// <summary>
        /// Extract a file from svn and write it locally (and overwrite what was already taken out!).
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="outfile"></param>
        public static void ExtractFile(SvnTarget ds, FileInfo outfile)
        {
            var args = new SvnExportArgs() { Overwrite = true };
            _client.Value.Export(ds, outfile.FullName, args);
        }


    }
}
