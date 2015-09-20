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

            // Do any filtering that needs to be done.
            const int nfiles = 5;
            var goodFileNames = Enumerable.Range(1, nfiles)
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
                    var dsDirForFile = index < (nfiles / 2) ? dsDirSub1 : dsDirSub2;
                    WriteShortRootFile(new FileInfo(Path.Combine(dsDirForFile.FullName, gfname)));
                    index++;
                }

                // Generate a .part files - a file that is only partially downloaded. Should never show up beyond this.
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub2.FullName, "file.weird.root.1.part")));
            }

            return root;
        }
        public static DirectoryInfo BuildSampleDirectoryBeforeBuild(string rootDirName, params string[] dsnames)
        {
            var root = new DirectoryInfo(rootDirName);
            if (root.Exists)
            {
                root.Delete(true);
            }

            var r = BuildSampleDirectory(rootDirName, null, dsnames);

            foreach (var ds in dsnames)
            {
                using (var wr = File.CreateText(Path.Combine(root.FullName, ds, "aa_dataset_complete_file_list.txt")))
                {
                    foreach (var fname in r.EnumerateFiles("*.root.*", SearchOption.AllDirectories).Where(f => !f.Name.EndsWith(".part")))
                    {
                        wr.WriteLine("user.norm:" + fname);
                    }
                }                
            }

            return r;
        }

        /// <summary>
        /// Mark the dataset as partially downloaded.
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
        public static Dictionary<string, Dictionary<string, string>> GetLocal(DirectoryInfo locLinuxWithWindows = null, DirectoryInfo locLocal = null)
        {
            var r = new Dictionary<string, Dictionary<string, string>>();

            // CERN
            if (locLinuxWithWindows != null)
            {
                var c = new Dictionary<string, string>();
                c["DNSEndString"] = ".cern.ch";
                c["Name"] = "MyTestLocation";
                c["WindowsPath"] = locLinuxWithWindows.FullName;
                c["LinuxPath"] = "/LLPData/GRIDDS";
                c["LocationType"] = "LinuxWithWindowsReflector";
                c["LinuxUserName"] = "gwatts";
                c["LinuxHost"] = "pcatuw4.cern.ch";

                r["MyTestLocation"] = c;
            }

            // Local
            if (locLocal != null)
            {
                var c = new Dictionary<string, string>();
                c["Name"] = "MyTestLocalLocation";
                c["Paths"] = locLocal.FullName;
                c["LocationType"] = "LocalWindowsFilesystem";

                r["MyTestLocalLocation"] = c; 
            }

            return r;
        }
    }
}
