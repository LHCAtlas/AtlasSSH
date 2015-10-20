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
    static class PrintUtils
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

        public static string Print(this Command r)
        {
            return r.Print(new StringBuilder()).ToString();
        }

        private static StringBuilder Print(this Command r, StringBuilder bld)
        {
            bld.AppendFormat("command({0})", r.CommandLine);
            return bld;
        }

        public static string Print(this Submit r)
        {
            return r.Print(new StringBuilder()).ToString();
        }

        private static StringBuilder Print(this Submit r, StringBuilder bld)
        {
            bld.AppendFormat("submit({0})", r.SubmitCommand.CommandLine);
            return bld;
        }

        public static string Print(this Package r)
        {
            return r.Print(new StringBuilder()).ToString();
        }

        private static StringBuilder Print(this Package r, StringBuilder bld)
        {
            bld.AppendFormat("package({0},{1})", r.Name, r.SCTag);
            return bld;
        }

        public static string Print(this Job r)
        {
            return r.Print(new StringBuilder()).ToString();
        }

        private static StringBuilder Print(this Job r, StringBuilder bld)
        {
            bld.AppendFormat("job({0},{1}){{", r.Name, r.Version);
            if (r.Release != null) r.Release.Print(bld);
            if (r.Packages != null)
            {
                foreach (var p in r.Packages)
                {
                    p.Print(bld);
                }
            }
            if (r.SubmitCommand != null) r.SubmitCommand.Print(bld);
            bld.Append("}");
            return bld;
        }
    }
}
