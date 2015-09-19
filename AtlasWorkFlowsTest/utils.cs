using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlowsTest
{
    class utils
    {

        /// <summary>
        /// Build some dummy local files.
        /// </summary>
        /// <param name="rootDirName"></param>
        /// <returns></returns>
        public static DirectoryInfo BuildSampleDirectory(string rootDirName, Func<string[], string[]> fileFilter, params string[] dsnames)
        {
            // Start clean!
            var root = new DirectoryInfo(rootDirName);
            if (root.Exists)
            {
                root.Delete(true);
            }

            // Do any filtering that needs to be done.
            var goodFileNames = Enumerable.Range(1, 5)
                .Select(index => string.Format("file.root.{0}", index))
                .ToArray();

            if (fileFilter != null) {
                goodFileNames = fileFilter(goodFileNames);
            }

            // Now, create a dataset(s).
            foreach (var ds in dsnames)
            {
                var dsDir = new DirectoryInfo(Path.Combine(root.FullName, ds));
                dsDir.Create();
                var dsDirSub1 = new DirectoryInfo(Path.Combine(dsDir.FullName, "sub1"));
                dsDirSub1.Create();
                var dsDirSub2 = new DirectoryInfo(Path.Combine(dsDir.FullName, "sub2"));
                dsDirSub2.Create();

                int index = 0;
                foreach (var gfname in goodFileNames)
                {
                    var dsDirForFile = index < goodFileNames.Length / 2 ? dsDirSub1 : dsDirSub2;
                    WriteShortRootFile(new FileInfo(Path.Combine(dsDirForFile.FullName, gfname)));
                }

                // Generate a .part files - a file that is only partially downloaded. Should never show up beyond this.
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub2.FullName, "file.weird.root.1.part")));
            }

            return root;
        }
        public static DirectoryInfo BuildSampleDirectory(string rootDirName, params string[] dsnames)
        {
            return BuildSampleDirectory(rootDirName, null, dsnames);
        }

        /// <summary>
        /// Mark the ds as partially downloaded.
        /// </summary>
        /// <param name="rootDir"></param>
        /// <param name="dsnames"></param>
        public static void MakePartial(DirectoryInfo rootDir, params string[] dsnames)
        {
            foreach (var ds in dsnames)
            {
                WriteShortRootFile(new FileInfo(Path.Combine(rootDir.FullName, ds, "aa_download_not_finished.txt")));
            }
        }

        /// <summary>
        /// Write out a short empty file.
        /// </summary>
        /// <param name="fileInfo"></param>
        private static void WriteShortRootFile(FileInfo fileInfo)
        {
            using (var wr = fileInfo.Create())
            { }
        }


        /// <summary>
        /// Generate a location that is local for this test environment.
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> GetLocal(DirectoryInfo location)
        {
            var r = new Dictionary<string, Dictionary<string, string>>();

            // CERN
            var c = new Dictionary<string, string>();
            c["DNSEndString"] = ".cern.ch";
            c["Name"] = "MyTestLocation";
            c["WindowsPath"] = location.FullName;
            c["LinuxPath"] = "/LLPData/GRIDDS";
            c["LocationType"] = "LinuxWithWindowsReflector";
            c["LinuxUserName"] = "gwatts";
            c["LinuxHost"] = "pcatuw4.cern.ch";

            r["MyTestLocation"] = c;

            return r;
        }
    }
}
