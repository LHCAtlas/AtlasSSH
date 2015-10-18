using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
    /// <summary>
    /// Extension methods to print out the job data structure as we read it back in.
    /// </summary>
    public static class PrintUtils
    {
        public static string Print (this Release r)
        {
            return r.Print(new StringBuilder()).ToString();
        }

        private static StringBuilder Print (this Release r, StringBuilder bld)
        {
            bld.AppendFormat("release({0})", r.Name);
            return bld;
        }
    }
}
