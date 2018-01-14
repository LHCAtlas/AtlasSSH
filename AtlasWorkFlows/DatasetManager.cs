using AtlasSSH;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static AtlasSSH.DiskCacheTypedHelpers;

namespace AtlasWorkFlows
{
    /// <summary>
    /// A singleton that is the master dataset manager
    /// </summary>
    public class DatasetManager
    {
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
        /// Return a list of files in a dataset. For high-speed access we cache everything.
        /// </summary>
        /// <param name="dsname"></param>
        /// <param name="statusUpdate"></param>
        /// <param name="failNow"></param>
        /// <param name="probabalLocation">The dataset is almost certainly here - look here first</param>
        /// <returns></returns>
        internal static async Task<string[]> ListOfFilenamesInDatasetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null, IPlace probabalLocation = null)
        {
            return await NonNullCacheInDiskAsync("AtlasWorkFlows-ListOfFilenamesInDataset", dsname, async () =>
            {
                // This used to be a clean LINQ expression. However, we want to make sure to evaluate things one at a time
                // and since the GetListOfFilesForDatasetAsync is expensive, we want to do it no more than is needed.
                var r = new IPlace[] { probabalLocation }.Concat(await _places)
                    .Where(p => p != null);
                foreach (var p in r)
                {
                    var files = await p.GetListOfFilesForDatasetAsync(dsname.Dataset(), statusUpdate, failNow: failNow);
                    if (files != null)
                    {
                        return files;
                    }
                }
                throw new DataSetDoesNotExistException($"Dataset {dsname} could not be found at any place.");
            });

        }

        /// <summary>
        /// Given a dataset name, fine its list of files.
        /// </summary>
        /// <param name="dsname">Dataset name to return file list for. Throw if it can't be found.</param>
        /// <returns>List of URI's of the files.</returns>
        /// <exception cref="DataSetDoesNotExistException">Throw if <paramref name="dsname"/> does not exist.</exception>
        /// <remarks>
        /// Look through the list of places in tier order (from closests to furthest) until we find a good dataset.
        /// </remarks>
        public static async Task<Uri[]> ListOfFilesInDatasetAsync(string dsname, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            return (await ListOfFilenamesInDatasetAsync(dsname, statusUpdate, failNow))
                    .Select(fs => new Uri($"gridds://{dsname.Dataset()}/{fs}"))
                    .ToArray();
        }

        /// <summary>
        /// For a given set of files, find all places we know about that contian the complete list.
        /// </summary>
        /// <param name="dsfiles"></param>
        /// <param name="maxDataTier">Don't look at any places with a data teir at or above this number</param>
        /// <returns></returns>
        public static async Task<string[]> ListOfPlacesHoldingAllFilesAsync(IEnumerable<Uri> dsfiles, int maxDataTier = 1000)
        {
            // Because of the async nature of HasFileAsync, we can't use a nice three line LINQ expression, unfortunately.
            List<IPlace> good_places = new List<IPlace>();
            foreach (var p in (await _places).Where(p => p.DataTier <= maxDataTier))
            {
                var r = await Task.WhenAll(dsfiles.Select(async f => await p.HasFileAsync(f)));
                if (r.All(f => f))
                {
                    good_places.Add(p);
                }
            }

            return good_places.OrderBy(p => p.DataTier)
                .Select(p => p.Name)
                .ToArray();
        }

        /// <summary>
        /// Return the path from the local place where the file can be found
        /// </summary>
        /// <param name="place"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static async Task<Uri> LocalPathToFileAsync(string place, Uri file)
        {
            return (await LocalPathToFilesAsync(place, new[] { file })).First();
        }

        /// <summary>
        /// Find the path on the local machine for a particlar place. This means the URI returned will often
        /// be useless on the current machine as it may refer to a file on another machine!
        /// </summary>
        /// <param name="place"></param>
        /// <param name="files"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<Uri>> LocalPathToFilesAsync(string place, IEnumerable<Uri> files)
        {
            var p = (await _places)
                .Where(pl => pl.Name == place)
                .FirstOrDefault()
                .ThrowIfNull(() => new ArgumentException($"I do not know about place {place}, so I can't find file {files.First().ToString()} on it!"));

            return await p.GetLocalFileLocationsAsync(files);
        }

        /// <summary>
        /// Light weight class to help with refactoring
        /// </summary>
        private struct RoutedFileInfo
        {
            public Route r;
            public Uri f;
        }

        /// <summary>
        /// Given a list of files in (one or more datasets) return the list of local file system URI's that
        /// point to the files. We will do real work here - copying files if we need to.
        /// </summary>
        /// <param name="files">A uri list of files that we need to make local. Must all be gridds URI's</param>
        /// <param name="failNow">Abort early if this function ever returns true</param>
        /// <param name="statusUpdate">Callback used to send out messages updating the progress.</param>
        /// <param name="timeout">How many minutes to allow things to progress without aborting</param>
        /// <returns>A list of local files that point to the file objects themselves.</returns>
        public static async Task<Uri[]> MakeFilesLocalAsync(Uri[] files, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeout = 60)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routedFilesList = goodFiles
                .Select(async u => new RoutedFileInfo { r = await FindRouteTo(u, p => p.IsLocal, statusUpdate: statusUpdate, failNow: failNow), f = u });
            var routedFiles = (await Task.WhenAll(routedFilesList))
                .GroupBy(info => info.r.Name)
                .ToArray();

            // Next, we can execute the copy. We do it by grouping as often the route is more efficient when a group of files
            // are done rather than a single file.
            var resultUris = await GetListOfRoutedFiles(statusUpdate, failNow, timeout, routedFiles);

            return resultUris.ToArray();
        }

        /// <summary>
        /// Get a list of routed files sorted by routes - taking the first one.
        /// </summary>
        /// <param name="statusUpdate"></param>
        /// <param name="failNow"></param>
        /// <param name="timeout"></param>
        /// <param name="routedFiles"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<Uri>> GetListOfRoutedFiles(Action<string> statusUpdate, Func<bool> failNow, int timeout, IGrouping<string, RoutedFileInfo>[] routedFiles)
        {
            var results = new List<Uri>();
            foreach (var rtGrouping in routedFiles)
            {
                var rt = rtGrouping.First().r;
                var uris = (await rt.ProcessFilesAsync(rtGrouping.Select(r => r.f), statusUpdate, failNow, timeout));
                results.AddRange(uris);
            }
            return results;
        }

        /// <summary>
        /// Find a route to move files from some location to a particular destination.
        /// </summary>
        /// <param name="destination">The place we want to move everything to.</param>
        /// <param name="files">List of gridds Uri's of the files that we should be moving</param>
        /// <param name="timeout">How many minutes before timeing out</param>
        /// <returns>List of local URI's if the <paramref name="destination"/> is local, otherwise the gridds type Uri's</returns>
        public static async Task<Uri[]> CopyFilesToAsync(IPlace destination, Uri[] files, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeout = 60)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            if (destination == null)
            {
                throw new ArgumentNullException("Can't have a null destination!");
            }
            if (!(await _places).Contains(destination))
            {
                throw new ArgumentException($"Place {destination.Name} is not on our master list of places!");
            }

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routedFilesList = goodFiles
                .Select(async u => new RoutedFileInfo { r = await FindRouteTo(u, p => p == destination, p => p == destination, statusUpdate: statusUpdate, failNow: failNow), f = u });
            var routedFiles = (await Task.WhenAll(routedFilesList))
                .GroupBy(info => info.r.Name)
                .ToArray();

            // To do the files, we need to first fine a routing from wherever they are to the final location.
            var resultUris = await GetListOfRoutedFiles(statusUpdate, failNow, timeout, routedFiles);

            return resultUris.ToArray();
        }

        /// <summary>
        /// Copy files one one location to another. This method has the start and destination locations "pinned".
        /// </summary>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="files"></param>
        /// <param name="statusUpdate"></param>
        /// <param name="failNow"></param>
        /// <param name="timeout">Minutes of no progress before canceling it</param>
        /// <returns></returns>
        public static async Task<Uri[]> CopyFilesAsync(IPlace source, IPlace destination, Uri[] files, Action<string> statusUpdate = null, Func<bool> failNow = null, int timeout = 60)
        {
            // Simple checks
            var goodFiles = files
                .ThrowIfNull(() => new ArgumentNullException($"files can't be a null argument"))
                .Throw(u => u.Scheme != "gridds", u => new UnknownUriSchemeException($"Can only deal with gridds:// uris - found: {u.OriginalString}"));

            if (destination == null)
            {
                throw new ArgumentNullException("Can't have a null destination!");
            }
            if (!(await _places).Contains(destination))
            {
                throw new ArgumentException($"Place {destination.Name} is not on our master list of places!");
            }

            if (source == null)
            {
                throw new ArgumentNullException("Can't have a null source!");
            }
            if (!(await _places).Contains(source))
            {
                throw new ArgumentException($"Place {source.Name} is not on our master list of places!");
            }

            var all_check = await Task.WhenAll(files.Select(async fl => await source.HasFileAsync(fl, statusUpdate, failNow)));
            if (!all_check.All(f => f))
            {
                throw new ArgumentException($"Place {source.Name} does not have all the requested files");
            }

            // Find a route to make them "local", and sort them by the route name (e.g. we can batch the file accesses).
            var routeSources = new IPlace[] { source };
            var routedFilesList = await Task.WhenAll(goodFiles
                .Select(async u => new RoutedFileInfo {
                    r = await FindRouteFromSources((await destination.HasFileAsync(u, statusUpdate, failNow)) ? routeSources.Concat(new IPlace[] { destination }).ToArray() : routeSources, u, p => p == destination, p => p == destination),
                    f = u
                }));
            var routedFiles = routedFilesList
                .GroupBy(info => info.r.Name)
                .ToArray();

            // To do the files, we need to first fine a routing from wherever they are to the final location.
            var resultUris = await GetListOfRoutedFiles(statusUpdate, failNow, timeout, routedFiles);

            return resultUris.ToArray();
        }

        /// <summary>
        /// Reset the connections on all of our places
        /// </summary>
        public static async Task ResetConnectionsAsync()
        {
            foreach (var p in await _places)
            {
                await p.ResetConnectionsAsync();
            }
        }

        /// <summary>
        /// Find a route from a file to something local we are allowed to use.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="endCondition">The condition which says we have made it to where we want to go</param>
        /// <param name="forceOk">Will make sure this particular place is considered in all of our searches no matter what</param>
        /// <returns>A route</returns>
        private static async Task<Route> FindRouteTo(Uri u, Func<IPlace, bool> endCondition, Func<IPlace, bool> forceOk = null, Action<string> statusUpdate = null, Func<bool> failNow = null)
        {
            // First we need to find a source for this file.
            // We have to be a little careful with processing - the HasFileAsync can be very expensive, so
            // we do not want to call mroe than we need.
            var goodPlaces = new List<IPlace>();
            foreach (var p in await _places)
            {
                if (await p.HasFileAsync(u, statusUpdate, failNow))
                {
                    if (endCondition(p))
                    {
                        goodPlaces.Add(p);
                        break;
                    }
                    goodPlaces.Add(p);
                }
            }

            if (goodPlaces.Count == 0)
            {
                throw new DataSetDoesNotExistException($"No place knows how to fetch '{u.OriginalString}'.");
            }

            return await FindRouteFromSources(goodPlaces.ToArray(), u, endCondition, forceOk);
        }

        /// <summary>
        /// From a list of sources, find a route to a destination.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="endCondition"></param>
        /// <param name="forceOk"></param>
        /// <param name="sources"></param>
        /// <returns></returns>
        private static async Task<Route> FindRouteFromSources(IPlace[] sources, Uri u, Func<IPlace, bool> endCondition, Func<IPlace, bool> forceOk)
        {
            // Now we have sources. We build a path for each, and select the path with the least number of steps.
            var results = new List<Route>();
            foreach (var r in sources.Select(s => new Route(s)))
            {
                foreach (var step in await FindStepsTo(r, endCondition, forceOk))
                {
                    results.Add(step);
                }
            }

            return results
                .OrderBy(r => r.Length)
                .FirstOrDefault()
                .ThrowIfNull(() => new NoLocalPlaceToCopyToException($"Unable to find a way to copy {u} locally. Could be you have to explicitly copy it to your local cache."));
        }

        /// <summary>
        /// Build routes until some sort of end condition is met. No circular routes allowed.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="endCondition"></param>
        /// <param name="forceOk">If true is returned, will make sure that these places aren't taken off the list for consideration</param>
        /// <returns></returns>
        private static async Task<IEnumerable<Route>> FindStepsTo(Route r, Func<IPlace, bool> endCondition, Func<IPlace, bool> forceOk = null)
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
            return await (await _places)
                .Where(p => !p.NeedsConfirmationCopy || forceOk(p))
                .Where(p => !r.Contains(p))
                .Where(p => r.LastPlace.CanSourceCopy(p) || p.CanSourceCopy(r.LastPlace))
                .Select(p => new Route(r, p))
                .SelectManyAsync(nr => FindStepsTo(nr, endCondition, forceOk));
        }

        /// <summary>
        /// Reset the IPlace list. Used only for testing, dangerous otherwise!
        /// </summary>
        /// <param name="places">List of places that we should use. If empty, then the default list will be used.</param>
        public static void ResetDSM(params IPlace[] places)
        {
            if (places.Length == 0)
            {
                _places = new AsyncLazy<IPlace[]>(() => LoadPlaces());
            } else
            {
                _places = new AsyncLazy<IPlace[]>(() => Task.FromResult(places.OrderBy(p => p.DataTier).ToArray()));
            }
            DiskCache.RemoveCache("AtlasWorkFlows-ListOfFilenamesInDataset");
        }

        /// <summary>
        /// Return the place with an exact match for the given name. Null if it isn't found.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<IPlace> FindLocation(string name)
        {
            return (await _places)
                .Where(p => p.Name == name)
                .FirstOrDefault();
        }

        /// <summary>
        /// Return the list of locations that we are managing right now.
        /// </summary>
        public static string[] ValidLocations
        {
            get { return _places.Task.Result.Select(p => p.Name).ToArray(); }
        }

        /// <summary>
        /// Keep a list of places. Only spin up if we need as it will involve some
        /// decent heavy-duty parsing of text files.
        /// </summary>
        /// <remarks>This list is sorted in Tier order all the time, starting from the lowest to the highest.</remarks>
        private static AsyncLazy<IPlace[]> _places = new AsyncLazy<IPlace[]>(() => LoadPlaces());

        /// <summary>
        /// Load places from the default text config file
        /// </summary>
        /// <returns></returns>
        private static async Task<IPlace[]> LoadPlaces()
        {
            var config = Config.GetLocationConfigs();
            var places = new List<IPlace>();
            foreach (var plst in config.Keys.Select(placeName => ParseSingleConfig(config[placeName])))
            {
                foreach (var p in (await plst))
                {
                    places.Add(p);
                }
            }

            return places
                .Where(p => p != null)
                .OrderBy(p => p.DataTier)
                .ToArray();
        }

        /// <summary>
        /// Given the dictionary of parameters, create a place.
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<IPlace>> ParseSingleConfig(Dictionary<string, string> info)
        {
            // Check to see if there are any restrictions on using these guys
            bool isGoodConfig = true;
            if (info.ContainsKey("UseOnlyWhenDNSEndStringIs"))
            {
                // We will use this iff the DNS string ends with the argument.
                isGoodConfig = (await CheckDNS(info["UseOnlyWhenDNSEndStringIs"])).Any(t => t);
            }
            if (info.ContainsKey("UseWhenDNSEndStringIsnt"))
            {
                // We will use this config only if the IP address doesn't end in one of the given ones here.
                if (isGoodConfig)
                {
                    isGoodConfig = (await CheckDNS(info["UseWhenDNSEndStringIsnt"])).All(t => !t);
                }
            }

            var result = new List<IPlace>();
            if (isGoodConfig)
            {
                // Now, create the end points as needed.
                var type = info["LocationType"];
                if (type == "LocalWindowsFilesystem")
                {
                    result.Add(CreateWindowsFilesystemPlace(info));
                }
                else if (type == "LinuxWithLocalAndGRID")
                {
                    // First, do the linux remote
                    var name = info["Name"];
                    var p_linux = CreateLinuxFilesystemPlace($"{name}-linux", info);
                    result.Add(p_linux);
                    // Next, the grid
                    var p_grid = new PlaceGRID($"{name}-GRID", p_linux);
                    result.Add(p_grid);
                    // Next, the local if that is "ok" to create.
                    var localVisible = info.ContainsKey("WindowsAccessibleDNSEndString")
                        ? (await CheckDNS(info["WindowsAccessibleDNSEndString"])).Any(t => t)
                        : true;
                    if (localVisible)
                    {
                        Trace.WriteLine($"Linux-GRID-Local {name} is locally visible.", "ParseSingleConfig");
                        // Create the local disk - but this is a big server, so a copy confirmation isn't needed
                        var ip = CreateWindowsFilesystemPlace(info, needsCopyConfirmation: false);
                        result.Add(ip);
                    }
                }
                else
                {
                    throw new UnknownLocationTypeException($"Location type {type} is not known as read from the configuration file.");
                }
            }
            return result;
        }

        /// <summary>
        /// See if a particular thign is in the DNS list.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private static async Task<IEnumerable<bool>> CheckDNS(string dnsList)
        {
            var result = new List<bool>();
            foreach (var dns in dnsList.Split(','))
            {
                bool endswith = (await IPLocationTests.FindLocalIpName()).EndsWith(dns.Trim());
                result.Add(endswith);
            }
            return result;
        }

        /// <summary>
        /// Create a new linux remote
        /// </summary>
        /// <param name="name"></param>
        /// <param name="info"></param>
        /// <returns></returns>
        private static PlaceLinuxRemote CreateLinuxFilesystemPlace(string name, Dictionary<string, string> info)
        {
            return new PlaceLinuxRemote(name, info["LinuxPath"], info["LinuxHost"]);
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
                var wp = info["WindowsPaths"];
                Trace.WriteLine($"No good path was found for a Local directory when I looked at {wp}.", "CreateWindowsFilesystemPlace");
                return null;
            }

            return new PlaceLocalWindowsDisk(info["Name"], goodPath, needsConfirmationOfCopy: needsCopyConfirmation);
        }
    }

    static class LINQUtils
    {
        public static async Task<IEnumerable<TResult>> SelectManyAsync<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Task<IEnumerable<TResult>>> selector)
        {
            var results = new List<TResult>();
            foreach (var s in source)
            {
                foreach (var r in await selector(s))
                {
                    results.Add(r);
                }
            }
            return results;
        }
    }
}
