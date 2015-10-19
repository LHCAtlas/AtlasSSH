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
    static class JobParser
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
        /// Grab whatever is surrounded by these guys
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U1"></typeparam>
        /// <typeparam name="U2"></typeparam>
        /// <param name="inside"></param>
        /// <param name="open"></param>
        /// <param name="close"></param>
        /// <returns></returns>
        public static Parser<T> ParseInterior<T, U1, U2> (this Parser<T> inside, Parser<U1> open, Parser<U2> close)
        {
            return from o in open
                   from r in inside.Token()
                   from c in close
                   select r;
        }

        private static Parser<IEnumerable<string>> ParseArgumentList =
            from args in Parse.Identifier(Parse.LetterOrDigit, Parse.LetterOrDigit).DelimitedBy(Parse.Char(',').Token()).ParseInterior(Parse.Char('('), Parse.Char(')'))
            select args;

        /// <summary>
        /// Parse a package definition
        /// </summary>
        public static Parser<Package> ParsePackage =
            from rid in Parse.String("package").Token()
            from args in ParseArgumentList
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

        /// <summary>
        /// Parse and convert
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="mainParser"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        private static Parser<T> ParseAs<T, U> (this Parser<U> mainParser, Func<U, T> converter)
        {
            return mainParser.Select(t => converter(t));
        }

        /// <summary>
        /// Parse a complete job
        /// </summary>
        public static Parser<Job> ParseJob
            = from rid in Parse.String("job").Token()
              from args in ParseArgumentList.Token()
              from interior in (
                    ParseRelease.ParseAs<Action<Job>, Release>(r => (Job j) => j.Release = r)
                    .Or(ParseCommand.ParseAs<Action<Job>, Command>(c => (Job j) => j.Commands = j.Commands.Append(c)))
                    .Or(ParsePackage.ParseAs<Action<Job>, Package>(p => (Job j) => j.Packages = j.Packages.Append(p)))
                    .Or(ParseSubmit.ParseAs<Action<Job>,Submit>(s => (Job j) => j.SubmitCommand = s))
                  ).Token().Many().ParseInterior(Parse.Char('{'), Parse.Char('}'))
              select AsJob(args, interior);

        /// <summary>
        /// Build a job from a list of items to add. Make sure that nothing is null that shouldn't be even if it wasn't specified.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="interior"></param>
        /// <returns></returns>
        private static Job AsJob(IEnumerable<string> args, IEnumerable<Action<Job>> interior)
        {
            var a = args.ToArray();
            if (a.Length != 2)
            {
                throw new ArgumentException("Unable to parse job - needs two arguments: name, version");
            }

            var j = new Job() { Name = a[0], Version = int.Parse(a[1]) };
            foreach (var act in interior)
            {
                act(j);
            }

            // Make sure things that are null are no longer null.
            if (j.Commands == null) j.Commands = new Command[0];
            if (j.Packages == null) j.Packages = new Package[0];
            if (j.Release == null) j.Release = new Release() { Name = "" };
            if (j.SubmitCommand == null) j.SubmitCommand = new Submit() { SubmitCommand = new Command() { CommandLine = "" } };

            return j;
        }
    }
}
