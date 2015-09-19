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
        public static DirectoryInfo BuildSampleDirectory(string rootDirName, params string[] dsnames)
        {
            // Start clean!
            var root = new DirectoryInfo(rootDirName);
            if (root.Exists)
            {
                root.Delete(true);
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
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub1.FullName, "file.root.1")));
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub1.FullName, "file.root.2")));
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub2.FullName, "file.root.1")));
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub2.FullName, "file.root.2")));
                WriteShortRootFile(new FileInfo(Path.Combine(dsDirSub2.FullName, "file.root.3")));
            }

            return root;
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
    }
}
