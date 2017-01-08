using Microsoft.Win32;
using Sprache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AtlasWorkFlows.Utils
{
    /// <summary>
    /// Build configuration for various things.
    /// Eventually I hope most of this can be loaded dynamically. But for initial setup, this seems simplest.
    /// </summary>
    class Config
    {
        /// <summary>
        /// Get the list of configurations
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> GetLocationConfigs()
        {
            var goodFile = GoodConfigFilesOfName("AtlasSSHConfig.txt").FirstOrDefault();

            if (goodFile == null)
            {
                throw new FileNotFoundException(string.Format("Unable to find the file 'AtlasSSHConfig.txt' in the current directory, the Documents folder, or the root OneDrive folder! Without configuration we can't continue!"));
            }

            return ParseConfigFile(goodFile);
        }

        /// <summary>
        /// Given a default filename, return all filenames that match in our
        /// list of knonw locations.
        /// </summary>
        /// <param name="filename">The filename to search for</param>
        /// <returns></returns>
        public static FileInfo[] GoodConfigFilesOfName(string filename)
        {
            if(filename == null)
            {
                throw new ArgumentException("Parameter filename to GoodCOnfigFilesOfName can't be null.");
            }

            var files = GetFilesSafely("./", filename)
                .Concat(GetFilesSafely(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)), filename))
                .Concat(GetFilesSafely(Path.Combine(getOneDriveFolderPath()), filename))
                .Concat(GetFilesSafely(Path.Combine(getOneDriveFolderPath(), ".AtlasSSH"), filename));

            var goodFiles = files
                .Where(f => f.Exists)
                .ToArray();

            return goodFiles;
        }

        /// <summary>
        /// Returns a list of files in a directory that match a pattern. Safe even if directory
        /// does not exist.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static FileInfo[] GetFilesSafely(string directory, string filename)
        {
            var di = new DirectoryInfo(directory);
            if (!di.Exists)
                return new FileInfo[0];
            return di.GetFiles(filename);
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
        /// Parse a configuration text file.
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

        /// <summary>
        /// Return the path of the OneDrive folder on this machine.
        /// </summary>
        /// <returns></returns>
        private static string getOneDriveFolderPath()
        {
            var value1 = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\SkyDrive",
                @"UserFolder", null);

            var path1 = value1 as string;
            if (path1 != null && Directory.Exists(path1)) return path1;

            var value2 = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\SkyDrive",
                @"UserFolder", null);

            var path2 = value2 as string;
            if (path2 != null && Directory.Exists(path2)) return path2;

            var value3 = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\OneDrive",
                @"UserFolder", null);

            var path3 = value3 as string;
            if (path3 != null && Directory.Exists(path3)) return path3;

            throw new InvalidOperationException("Onedrive is not setup on this machine. I cannot determine the onedrive path.");
        }
    }
}
