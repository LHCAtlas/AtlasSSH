using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    public static class FileInfoUtils
    {
        /// <summary>
        /// Read a file to the end, as a string
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string[] ReadToEnd (this FileInfo file)
        {
            using (var rdr = file.OpenText())
            {
                var lines = new List<string>();
                string line = "";
                while ((line = rdr.ReadLine()) != null)
                {
                    lines.Add(line);
                }

                return lines.ToArray();
            }
        }
    }
}
