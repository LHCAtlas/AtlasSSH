using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSAtlasDatasetCommands.Utils
{
    static class FileInfoUtils
    {
        /// <summary>
        /// Read lines as an iterator if the file exists. Otherwise return the empty string.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static IEnumerable<string> ReadLinesIfExists(this FileInfo source)
        {
            if (source.Exists)
            {
                return source.ReadLines();
            } else
            {
                return Enumerable.Empty<string>();
            }
        }
    }
}
