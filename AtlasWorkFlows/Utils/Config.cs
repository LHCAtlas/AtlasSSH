using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Build config for various things.
    /// Eventually I hope most of this can be loaded dynamically. But for initial setup, this seems simplest.
    /// </summary>
    class Config
    {
        public static Dictionary<string, Dictionary<string, string>> GetLocationConfigs()
        {
            var r = new Dictionary<string, Dictionary<string, string>>();

            // CERN
            var c = new Dictionary<string, string>();
            c["DNSEndString"] = ".cern.ch";
            c["Name"] = "CERN";
            c["WindowsPath"] = @"\\uw01.myds.me\LLPData\GRIDDS";
            c["LinuxPath"] = "/LLPData/GRIDDS";
            c["LocationType"] = "LinuxWithWindowsReflector";
            c["LinuxHost"] = "pcatuw4.cern.ch";
            c["LinuxUserName"] = "gwatts";
            c["LinuxFetcherType"] = "LinuxFetcher";
            c["Priority"] = "10";

            r["CERN"] = c;

            // Local
            c = new Dictionary<string, string>();
            c["Name"] = "Local";
            c["Paths"] = @"D:\GRIDDS";
            c["LocationType"] = "LocalWindowsFilesystem";
            c["Priority"] = "100";

            c["LinuxFetcherType"] = "LinuxFetcher";
            c["LinuxHost"] = "tev01.phys.washington.edu";
            c["LinuxUserName"] = "gwatts";
            c["LinuxTempLocation"] = "/tmp/gwattsdownloads";

            r["Local"] = c;

            return r;
        }

        #region Parsers

        private static readonly Parser<string> Comment =
            from c in ParseCharText('#')
            from t in RestOfLine
            select t;

        private static readonly Parser<char> EndOfText =
            Parse.LineTerminator.Or(Comment).Return(' ');

        private static readonly Parser<string> Identifier =
            Parse.Identifier(Parse.Letter, Parse.LetterOrDigit.Or(Parse.Char('_')));

        private static readonly Parser<string> RestOfLine =
            Parse.AnyChar.Until(EndOfText).Text();

        private static Parser<char> ParseCharText (char c)
        {
            return from wh in Parse.WhiteSpace.Many()
                   from cp in Parse.Char(c)
                   select cp;
        }

        private static readonly Parser<System.Tuple<string, string, string>> Line
            = from blankLines in EndOfText.Many().Optional()
              from configName in Identifier
              from dot1 in ParseCharText('.')
              from parameter in Identifier
              from dot2 in ParseCharText('=')
              from value in RestOfLine
              select Tuple.Create(configName, parameter, value.Trim());

        private static readonly Parser<IEnumerable<Tuple<string, string, string>>> File
            = Line.Many();

        #endregion

        /// <summary>
        /// Parse a Config text file.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        internal static Dictionary<string, Dictionary<string, string>> ParseConfigFile(System.IO.FileInfo f)
        {
            using (var rd = f.OpenText())
            {
                var input = rd.ReadToEnd();
                var results = File.Parse(input);

                var r = new Dictionary<string, Dictionary<string, string>>();
                foreach (var t in results)
                {
                    if (!r.ContainsKey(t.Item1))
                    {
                        r[t.Item1] = new Dictionary<string, string>();
                    }
                    r[t.Item1][t.Item2] = t.Item3;
                }

                return r;
            }
        }
    }
}
