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
        public static string Print (this Release r, bool prettyPrint = false)
        {
            return r.Print(new StringBuilder(), prettyPrint: prettyPrint).ToString();
        }

        private static StringBuilder Print (this Release r, StringBuilder bld, bool prettyPrint = false)
        {
            if (prettyPrint)
                bld.Append("  ");
            bld.AppendFormat("release({0})", r.Name);
            if (prettyPrint)
                bld.AppendLine();
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

        public static string Print(this Submit r, bool prettyPrint = false)
        {
            return r.Print(new StringBuilder(), prettyPrint: prettyPrint).ToString();
        }

        private static StringBuilder Print(this Submit r, StringBuilder bld, bool prettyPrint = false)
        {
            if (prettyPrint)
                bld.Append("  ");
            bld.AppendFormat("submit({0})", r.SubmitCommand.CommandLine);
            if (prettyPrint)
                bld.AppendLine();
            return bld;
        }

        private static StringBuilder Print(this SubmitPattern p, StringBuilder bld, bool prettyPrint = false)
        {
            if (prettyPrint)
                bld.Append("  ");
            bld.Append($"submit_pattern({p.Regex}, {p.SubmitCommand.SubmitCommand})");
            if (prettyPrint)
                bld.AppendLine();
            return bld;
        }

        public static string Print(this Package r, bool prettyPrint = false)
        {
            return r.Print(new StringBuilder(), prettyPrint: prettyPrint).ToString();
        }

        private static StringBuilder Print(this Package r, StringBuilder bld, bool prettyPrint = false)
        {
            if (prettyPrint)
                bld.Append("  ");
            bld.AppendFormat("package({0},{1})", r.Name, r.SCTag);
            if (prettyPrint)
                bld.AppendLine();
            return bld;
        }

        public static string Print(this AtlasJob r, bool prettyPrint = false)
        {
            return r.Print(new StringBuilder(), prettyPrint: prettyPrint).ToString();
        }

        private static StringBuilder Print(this AtlasJob r, StringBuilder bld, bool prettyPrint = false)
        {
            if (prettyPrint && bld.Length > 0)
                bld.AppendLine();

            bld.AppendFormat("job({0},{1}){{", r.Name, r.Version);
            if (prettyPrint)
                bld.AppendLine();

            if (r.Release != null) r.Release.Print(bld, prettyPrint: prettyPrint);
            if (r.Packages != null)
            {
                foreach (var p in r.Packages.OrderBy(p => $"{p.Name}-{p.SCTag}"))
                {
                    p.Print(bld, prettyPrint: prettyPrint);
                }
            }
            if (r.SubmitPatternCommands != null && r.SubmitPatternCommands.Length > 0)
            {
                foreach (var sc in r.SubmitPatternCommands.OrderBy(p => $"{p.Regex}-{p.SubmitCommand.SubmitCommand}"))
                {
                    sc.Print(bld, prettyPrint: prettyPrint);
                }
            }
            else
            {
                if (r.SubmitCommand != null) r.SubmitCommand.Print(bld, prettyPrint: prettyPrint);
            }
            bld.Append("}");
            return bld;
        }
    }
}
