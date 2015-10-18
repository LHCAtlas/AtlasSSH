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

        /// <summary>
        /// Generate a parser for a single word and type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="identifier"></param>
        /// <param name="buildResult"></param>
        /// <returns></returns>
        private static Parser<T> ParseSingleAnyArg<T>(string identifier, Func<string, T> buildResult)
        {
            return from rid in Parse.String(identifier)
                   from wh in Parse.WhiteSpace.Many()
                   from arg in Parse.Contained(ParseNonArgumentString, Parse.Char('('), Parse.Char(')'))
                   select buildResult(arg);
        }

        /// <summary>
        /// Parse a release command
        /// </summary>
        public static Parser<Release> ParseRelease =
            ParseSingleAnyArg("release", n => new Release() { Name = n });
            
        /// <summary>
        /// Parse a command line
        /// </summary>
        public static Parser<Command> ParseCommand =
            ParseSingleAnyArg("command", n => new Command() { CommandLine = n });

        /// <summary>
        /// Parse a submit line
        /// </summary>
        public static Parser<Submit> ParseSubmit =
            ParseSingleAnyArg("submit", n => new Submit() { SubmitCommand = new Command() { CommandLine = n } });

        /// <summary>
        /// Parse a package definition
        /// </summary>
        public static Parser<Package> ParsePackage =
            from rid in Parse.String("package")
            from wh1 in Parse.WhiteSpace.Many()
            from openp in Parse.Char('(')
            from wh2 in Parse.WhiteSpace.Many()
            from args in Parse.Identifier(Parse.LetterOrDigit, Parse.LetterOrDigit).DelimitedBy(Parse.WhiteSpace.Many().Then(pold => Parse.Char(',')).Then(pold => Parse.WhiteSpace.Many()))
            from wh3 in Parse.WhiteSpace.Many()
            from closep in Parse.Char(')')
            select PackageBuilder(args);

        /// <summary>
        /// Build a package, fail if we can't.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="args"></param>
        /// <returns></returns>
        private static Package PackageBuilder(IEnumerable<string> args)
        {
            var a = args.ToArray();
            if (a.Length != 2)
            {
                throw new ArgumentException("package primitive needs two arguments: a name and a source control tag");
            }
            return new Package() { Name = a[0], SCTag = a[1] };
        }
    }
}
