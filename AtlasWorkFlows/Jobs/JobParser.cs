using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sprache;

namespace AtlasWorkFlows.Jobs
{
    /// <summary>
    /// Code to parse a job definition(s) in a text file
    /// </summary>
    class JobParser
    {
        /// <summary>
        /// Parse everything until closing parenthesis (but not those).
        /// </summary>
        private static Parser<string> ParseNonArgumentString =
            from rs in Parse.CharExcept(')').Many().Text()
            select rs.Trim();

        public static Parser<Release> ParseRelease =
            from rid in Parse.String("release")
            from wh in Parse.WhiteSpace.Many()
            from arg in Parse.Contained(ParseNonArgumentString, Parse.Char('('), Parse.Char(')'))
            select new Release() { Name = arg };
    }
}
