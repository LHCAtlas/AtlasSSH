using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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


        [Serializable]
        public class UnknownLocationTypeException : Exception
        {
            public UnknownLocationTypeException() { }
            public UnknownLocationTypeException(string message) : base(message) { }
            public UnknownLocationTypeException(string message, Exception inner) : base(message, inner) { }
            protected UnknownLocationTypeException(
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
        public static Uri[] ListOfFilesInDataset (string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simply look through the list looking for anyone that has the dataset.
            // Recall the list is in tier order.
            return _places.Value
                .Select(p => p.GetListOfFilesForDataset(dsname.Dataset(), statusUpdate, failNow: failNow))
                .Where(fs => fs != null)
                .Select(fs => fs.Select(f => new Uri($"gridds://{dsname.Dataset()}/{f}")).ToArray())
                .FirstOrDefault()
                .ThrowIfNull(() => new DatasetDoesNotExistException($"Dataset {dsname} could not be found at any place."));
        }

        /// <summary>
        /// Given a list of files in (one or more datasets) return the list of local file system URI's that
        /// point to the files. We will do real work here - copying files if we need to.
        /// </summary>
        /// <param name="files">A uri list of files that we need to make local. Must all be gridds URI's</param>
        /// <returns>A list of local files that point to the file objects themselves.</returns>
        public static Uri[] MakeFilesLocal(Uri[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routedFiles = goodFiles
                .Select(u => new { Route = FindRouteTo(u, p => p.IsLocal, statusUpdate: statusUpdate, failNow: failNow), File = u })
                .GroupBy(info => info.Route.Name)
                .ToArray();

            // Next, we can execute the copy. We do it by grouping as often the route is more efficient when a group of files
            // are done rather than a single file.
            var resultUris = from rtGrouping in routedFiles
                             let rt = rtGrouping.First().Route
                             from uri in rt.ProcessFiles(rtGrouping.Select(r => r.File), statusUpdate, failNow)
                             select uri;

            return resultUris.ToArray();
        }

        /// <summary>
        /// Find a route to move files from some location to a particular destination.
        /// </summary>
        /// <param name="destination">The place we want to move everything to.</param>
        /// <param name="files">List of gridds Uri's of the files that we should be moving</param>
        /// <returns>List of local URI's if the <paramref name="destination"/> is local, otherwise the gridds type Uri's</returns>
        public static Uri[] CopyFilesTo(IPlace destination, Uri[] files, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            if (destination == null)
            {
                throw new ArgumentNullException("Can't have a null destination!");
            }
            if (!_places.Value.Contains(destination))
            {
                throw new ArgumentException($"Place {destination.Name} is not on our master list of places!");
            }

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routedFiles = goodFiles
                .Select(u => new { Route = FindRouteTo(u, p => p == destination, p => p == destination, statusUpdate: statusUpdate, failNow: failNow), File = u })
                .GroupBy(info => info.Route.Name)
                .ToArray();

            // To do the files, we need to first fine a routing from wherever they are to the final location.
            var resultUris = from rtGrouping in routedFiles
                             let rt = rtGrouping.First().Route
                             from uri in rt.ProcessFiles(rtGrouping.Select(r => r.File), statusUpdate, failNow)
                             select uri;

            return resultUris.ToArray();
        }

        /// <summary>
        /// Reset the connections on all of our places
        /// </summary>
        public static void ResetConnections()
        {
            foreach (var p in _places.Value)
            {
                p.ResetConnections();
            }
        }

        /// <summary>
        /// Find a route from a file to something local we are allowed to use.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="endCondition">The condition which says we have made it to where we want to go</param>
        /// <param name="forceOk">Will make sure this particular place is considered in all of our searches no matter what</param>
        /// <returns>A route</returns>
        private static Route FindRouteTo(Uri u, Func<IPlace, bool> endCondition, Func<IPlace, bool> forceOk = null, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // First we need to find a source for this file.
            var sources = _places.Value
                .Where(p => p.HasFile(u, statusUpdate, failNow))
                .TakeUntil(p => endCondition(p), 1)
                .ToArray()
                .Throw(places => places.Length == 0,  places => new DatasetDoesNotExistException($"No place knows how to fetch '{u.OriginalString}'."));

            // Now we have sources. We build a path for each, and select the path with the least number of steps.
            var bestRoute = sources
                .Select(s => new Route(s))
                .SelectMany(r => FindStepsTo(r, endCondition, forceOk))
                .OrderBy(r => r.Length)
                .FirstOrDefault()
                .ThrowIfNull(() => new NoLocalPlaceToCopyToException($"Unable to find a way to copy {u} locally. Could be you have to explicitly copy it to your local cache."));

            return bestRoute;
        }

        /// <summary>
        /// Build routes until some sort of end condition is met. No circular routes allowed.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="endCondition"></param>
        /// <param name="forceOk">If true is returned, will make sure that these places aren't taken off the list for consideration</param>
        /// <returns></returns>
        private static IEnumerable<Route> FindStepsTo(Route r, Func<IPlace, bool> endCondition, Func<IPlace, bool> forceOk = null)
        {
            // If nothing extra is going on, then don't force anything.
            if (forceOk == null)
            {
                forceOk = p => false;
            }

            // Stop the recursion if we've found a place we should be happy with!
            if (endCondition(r.LastPlace))
            {
                return new Route[] { r };
            }

            // Go through all the places. Don't allow repeats, and make sure
            // nothing is included that needs explicit permission.
            return _places.Value
                .Where(p => !p.NeedsConfirmationCopy || forceOk(p))
                .Where(p => !r.Contains(p))
                .Where(p => r.LastPlace.CanSourceCopy(p) || p.CanSourceCopy(r.LastPlace))
                .Select(p => new Route(r, p))
                .SelectMany(nr => FindStepsTo(nr, endCondition, forceOk));
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
        /// Return the place with an exact match for the given name. Null if it isn't found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IPlace FindLocation(string name)
        {
            return _places.Value
                .Where(p => p.Name == name)
                .FirstOrDefault();
        }

        /// <summary>
        /// Return the list of locations that we are managing right now.
        /// </summary>
        public static string[] ValidLocations
        {
            get { return _places.Value.Select(p => p.Name).ToArray(); }
        }

        /// <summary>
        /// Keep a list of places. Only spin up if we need as it will involve some
        /// decent heavy-duty parsing of text files.
        /// </summary>
        /// <remarks>This list is sorted in Tier order all the time, starting from the lowest to the highest.</remarks>
        private static Lazy<IPlace[]> _places = new Lazy<IPlace[]>(() => LoadPlaces());

        /// <summary>
        /// Load places from the default text config file
        /// </summary>
        /// <returns></returns>
        private static IPlace[] LoadPlaces()
        {
            var config = Config.GetLocationConfigs();
            return config.Keys
                .SelectMany(placeName => ParseSingleConfig(config[placeName]))
                .Where(p => p != null)
                .OrderBy(p => p.DataTier)
                .ToArray();
        }

        /// <summary>
        /// Given the dictionary of parameters, create a place.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static IEnumerable<IPlace> ParseSingleConfig(Dictionary<string, string> info)
        {
            // Check to see if there are any restrictions on using these guys
            bool isGoodConfig = true;
            if (info.ContainsKey("UseOnlyWhenDNSEndStringIs"))
            {
                // We will use this iff the DNS string ends with the argument.
                isGoodConfig = info["UseOnlyWhenDNSEndStringIs"].Split(',').Select(s => IPLocationTests.FindLocalIpName().EndsWith(s.Trim())).Any(t => t);
            }
            if (info.ContainsKey("UseWhenDNSEndStringIsnt"))
            {
                // We will use this config only if the IP address doesn't end in one of the given ones here.
                if (isGoodConfig)
                {
                    isGoodConfig = info["UseWhenDNSEndStringIsnt"].Split(',').Select(s => IPLocationTests.FindLocalIpName().EndsWith(s.Trim())).All(t => !t);
                }
            }

            if (isGoodConfig)
            {
                // Now, create the end points as needed.
                var type = info["LocationType"];
                if (type == "LocalWindowsFilesystem")
                {
                    yield return CreateWindowsFilesystemPlace(info);
                }
                else if (type == "LinuxWithLocalAndGRID")
                {
                    // First, do the linux remote
                    var name = info["Name"];
                    var p_linux = CreateLinuxFilesystemPlace($"{name}-linux", info);
                    yield return p_linux;
                    // Next, the grid
                    var p_grid = new PlaceGRID($"{name}-GRID", p_linux);
                    yield return p_grid;
                    // Next, the local if that is "ok" to create.
                    var localVisible = info.ContainsKey("WindowsAccessibleDNSEndString")
                        ? info["WindowsAccessibleDNSEndString"].Split(',').Select(s => IPLocationTests.FindLocalIpName().EndsWith(s.Trim())).Any(t => t)
                        : true;
                    if (localVisible)
                    {
                        // Create the local disk - but this is a big server, so a copy confirmation isn't needed
                        yield return CreateWindowsFilesystemPlace(info, needsCopyConfirmation: false);
                    }
                }
                else
                {
                    throw new UnknownLocationTypeException($"Location type {type} is not known as read from the configuration file.");
                }
            }
        }

        /// <summary>
        /// Create a new linux remote
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private static PlaceLinuxRemote CreateLinuxFilesystemPlace(string name, Dictionary<string, string> info)
        {
            return new PlaceLinuxRemote(name, info["LinuxPath"], info["LinuxHost"].ParseHostPairChain());
        }

        /// <summary>
        /// Create a place that is a Windows filesystem.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="needsCopyConfirmation">If false, then this can be used as part of default routing</param>
        /// <returns></returns>
        private static IPlace CreateWindowsFilesystemPlace(Dictionary<string, string> info, bool needsCopyConfirmation = true)
        {
            // Look at the paths. And if we can't find something, then there is no location!
            var goodPath = info["WindowsPaths"]
                .Split(',')
                .Select(n => n.Trim())
                .Where(p => Directory.Exists(p))
                .Select(p => new DirectoryInfo(p))
                .FirstOrDefault();

            if (goodPath == null)
            {
                return null;
            }

            return new PlaceLocalWindowsDisk(info["Name"], goodPath, needsConfirmationOfCopy: needsCopyConfirmation);
        }
    }
}
