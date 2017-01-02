using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// A singleton that is the master dataset manager
    /// </summary>
    public class DatasetManager
    {
        /// <summary>
        /// Thrown if a dataset does not exist out there.
        /// </summary>
        [Serializable]
        public class DatasetDoesNotExistException : Exception
        {
            public DatasetDoesNotExistException() { }
            public DatasetDoesNotExistException(string message) : base(message) { }
            public DatasetDoesNotExistException(string message, Exception inner) : base(message, inner) { }
            protected DatasetDoesNotExistException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        [Serializable]
        public class NoLocalPlaceToCopyToException : Exception
        {
            public NoLocalPlaceToCopyToException() { }
            public NoLocalPlaceToCopyToException(string message) : base(message) { }
            public NoLocalPlaceToCopyToException(string message, Exception inner) : base(message, inner) { }
            protected NoLocalPlaceToCopyToException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Given a dataset name, fine its list of files.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns>List of URI's of the files</returns>
        /// <exception cref="DatasetDoesNotExistException">Throw if <paramref name="dsname"/> does not exist.</exception>
        /// <remarks>
        /// Look through the list of places in tier order (from closests to furthest) until we find a good dataset.
        /// </remarks>
        public static Uri[] ListOfFilesInDataset (string dsname)
        {
            // Simply look through the list looking for anyone that has the dataset.
            // Recall the list is in tier order.
            return _places.Value
                .Select(p => p.GetListOfFilesForDataset(dsname))
                .Where(fs => fs != null)
                .Select(fs => fs.Select(f => new Uri($"gridds://{dsname}/{f}")).ToArray())
                .FirstOrDefault()
                .ThrowIfNull(() => new DatasetDoesNotExistException($"Dataset {dsname} could not be found at any place."));
        }

        /// <summary>
        /// Given a list of files in (one or more datasets) return the list of local file system URI's that
        /// point to the files. We will do real work here - copying files if we need to.
        /// </summary>
        /// <param name="files">A uri list of files that we need to make local. Must all be gridds URI's</param>
        /// <returns>A list of local files that point to the file objects themselves.</returns>
        public static Uri[] MakeFilesLocal(params Uri[] files)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routedFiles = goodFiles
                .Select(u => new { Route = FindRouteToLocalFile(u), File = u })
                .GroupBy(info => info.Route.Name)
                .ToArray();

            // Next, we can execute the copy. We do it by grouping as often the route is more efficient when a group of files
            // are done rather than a single file.
            var resultUris = from rtGrouping in routedFiles
                             let rt = rtGrouping.First().Route
                             from uri in rt.ProcessFiles(rtGrouping.Select(r => r.File))
                             select uri;

            return resultUris.ToArray();
        }

        /// <summary>
        /// Find a route from a file to something local we are allowed to use.
        /// </summary>
        /// <param name="u"></param>
        /// <returns>A route</returns>
        private static Route FindRouteToLocalFile(Uri u)
        {
            // First we need to find a source for this file.
            var sources = _places.Value
                .Where(p => p.HasFile(u))
                .ToArray()
                .Throw(places => places.Length == 0,  places => new DatasetDoesNotExistException($"No place knows how to fetch '{u.OriginalString}'."));

            // Now we ahve sources. We build a path for each, and select the path with the least number of steps.
            var bestRoute = sources
                .Select(s => new Route(s))
                .SelectMany(r => FindStepsToLocal(r))
                .OrderBy(r => r.Length)
                .FirstOrDefault()
                .ThrowIfNull(() => new NoLocalPlaceToCopyToException($"Unable to find a way to copy {u} locally. Could be you have to explicitly copy it to your local cache."));

            return bestRoute;
        }

        /// <summary>
        /// Look through the places and explore all possibilities of getting the file to a local dude.
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        private static IEnumerable<Route> FindStepsToLocal(Route r)
        {
            // Stop the recursion if we've found a place we should be happy with!
            if (r.LastPlace.IsLocal)
            {
                return new Route[] { r };
            }

            // Go through all the places. Don't allow repeats, and make sure
            // nothing is included that needs explicit permission.
            return _places.Value
                .Where(p => !p.NeedsConfirmationCopy)
                .Where(p => !r.Contains(p))
                .Where(p => r.LastPlace.CanSourceCopy(p) || p.CanSourceCopy(r.LastPlace))
                .Select(p => new Route(r, p))
                .SelectMany(nr => FindStepsToLocal(nr));
        }

        /// <summary>
        /// Reset the IPlace list. Used only for testing, dangerous otherwise!
        /// </summary>
        /// <param name="places">List of places that we should use. If empty, then the default list will be used.</param>
        public static void ResetDSM(params IPlace[] places)
        {
            if (places.Length == 0)
            {
                _places = new Lazy<IPlace[]>(() => LoadPlaces());
            } else
            {
                _places = new Lazy<IPlace[]>(() => places.OrderBy(p => p.DataTier).ToArray());
            }
        }

        /// <summary>
        /// Keep a list of places. Only spin up if we need as it will involve some
        /// decent heavy-duty parsing of text files.
        /// </summary>
        /// <remarks>This list is sorted in Tier order all the time, starting from the lowest to the highest.</remarks>
        private static Lazy<IPlace[]> _places = new Lazy<IPlace[]>(() => LoadPlaces());

        /// <summary>
        /// Load places from the default place
        /// </summary>
        /// <returns></returns>
        private static IPlace[] LoadPlaces()
        {
            throw new NotImplementedException();
        }
    }
}
