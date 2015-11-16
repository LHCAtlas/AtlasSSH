using CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
    /// <summary>
    /// Deal with dataset functions
    /// </summary>
    public static class Datasets
    {
        /// <summary>
        /// Returns a dataset name that this job should produce given the original dataset name.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="orignalDSName"></param>
        /// <returns></returns>
        public static string ResultingDatasetName (this AtlasJob job, string orignalDSName, string scopeDSName)
        {
            // Remove the scope if it is there.
            var result = orignalDSName.RemoveBefore(":");

            // Split the dataset into its constituent parts.
            var dsParts = result.Split('.').ToList();

            // If the first item in the list is a user, remove that and the next item.
            if (dsParts[0] == "user")
            {
                dsParts.RemoveAt(0);
                dsParts.RemoveAt(0);
            }

            // Remove a few key words
            dsParts = dsParts
                .Where(i => i != "merge")
                .Where(i => i != "AOD")
                .ToList();

            // Look for the tid job spec. Remove it if it is there.
            var containsTID = dsParts.Where(i => i.Contains("tid")).FirstOrDefault();
            if (containsTID != null)
            {
                var minusTID = containsTID.RemoveTIDString();
                dsParts.Replace(containsTID, minusTID);
            }

            // Ok, if the second guy is a number, then it is a run number. That means the first one is almost certainly a scope. So remove it.
            int runNumber = 0;
            if (dsParts.Count > 1 && int.TryParse(dsParts[1], out runNumber))
            {
                dsParts.RemoveAt(0);
            }

            // Done. Combine into a single string and check length!
            var dsNew = $"{scopeDSName}.{dsParts.AsSingleString(".")}.{job.Name}_v{job.Version}_{job.Hash()}";
            while ((dsNew + "_hist-output.root/").Length > 133)
            {
                var oneLess = dsNew.RemoveATag();
                if (oneLess == dsNew)
                {
                    StringBuilder bld = new StringBuilder();
                    bld.AppendFormat("Dataset '{0}' is more than 133 characters after accounting for EventLoop naming (from ds '{1}'). It needs to be made shorter", dsNew, orignalDSName);
                    throw new ArgumentException(bld.ToString());
                }
                dsNew = oneLess;
            }

            return dsNew;
        }

        /// <summary>
        /// Return the resulting dataset name given a credential for the user name
        /// </summary>
        /// <param name="job"></param>
        /// <param name="origDSName"></param>
        /// <param name="gridCredentials"></param>
        /// <returns></returns>
        public static string ResultingDatasetName(this AtlasJob job, string origDSName, Credential gridCredentials)
        {
            var scope = string.Format("user.{0}", gridCredentials.Username);
            return job.ResultingDatasetName(origDSName, scope);
        }

        /// <summary>
        /// Fetch from GRID the credential name.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="origDSName"></param>
        /// <returns></returns>
        public static string ResultingDatasetName(this AtlasJob job, string origDSName)
        {
            var gridCredentials = new CredentialSet("GRID").Load().FirstOrDefault();
            if (gridCredentials == null)
            {
                throw new ArgumentException("Please create a generic windows credential with the target 'GRID' with the user name as the rucio grid user name and the password to be used with voms proxy init");
            }
            return job.ResultingDatasetName(origDSName, gridCredentials);
        }

        /// <summary>
        /// Remove everything in the string before the search string. Return full string if the search string is not found.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="removeThisAndBefore"></param>
        /// <returns></returns>
        private static string RemoveBefore (this string src, string removeThisAndBefore)
        {
            var i = src.IndexOf(removeThisAndBefore);
            if (i < 0)
                return src;

            return src.Substring(i + removeThisAndBefore.Length);
        }

        /// <summary>
        /// Combine a list of strings into a single string with a seperator.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="seperator"></param>
        /// <returns></returns>
        private static string AsSingleString (this IEnumerable<string> items, string seperator)
        {
            var bld = new StringBuilder();
            bool first = true;
            foreach (var item in items)
            {
                if (!first)
                {
                    bld.Append(".");
                }
                bld.Append(item);
                first = false;
            }
            return bld.ToString();
        }

        static Regex gTIDFinder = new Regex("_tid[0-9]+_[0-9]+");

        /// <summary>
        /// Remove a string that contains tid-nubmers-_-numbers-.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static string RemoveTIDString(this string input)
        {
            return gTIDFinder.Replace(input, "");
        }

        /// <summary>
        /// Replace an element of a list. Not efficient!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items"></param>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        private static void Replace<T> (this List<T> items, T oldValue, T newValue)
        {
            var loc = items.IndexOf(oldValue);
            if (loc > 0)
            {
                items[loc] = newValue;
            }
        }

        static Regex gTagFinder = new Regex(@"[_\.][a-z][0-9]+[_\.]");

        /// <summary>
        /// Attempt to remove a reconstuction tag.
        /// </summary>
        /// <param name="dsName"></param>
        /// <returns></returns>
        private static string RemoveATag (this string dsName)
        {
            var m = gTagFinder.Match(dsName);
            if (m.Success)
            {
                return dsName.Replace(m.Groups[0].Value, ".");
            }
            // No more tags to replace
            return dsName;
        }

        /// <summary>
        /// Make sure the number is postive.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static int AsPositiveNumber(this int num)
        {
            if (num < 0)
                return -num;
            return num;
        }
    }
}
