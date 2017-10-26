using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;
using AtlasWorkFlows.Utils;
using System.Diagnostics;
using Polly;

namespace AtlasWorkFlows.Panda
{
    public static class PandaUtils
    {
        /// <summary>
        /// This returns the info for a task that has the name given. It returns
        /// null if it wasn't found.
        /// </summary>
        /// <param name="taskName">Name of the task. Must be the complete name or this will fail.</param>
        /// <param name="withDetailedDSInfo">If we want detailed dataset info</param>
        /// <returns></returns>
        public static PandaTask FindPandaJobWithTaskName(this string taskName, bool withDetailedDSInfo = false, bool useCacheIfPossible = true)
        {
            var url = BuildTaskUri(string.Format("tasks/?taskname={0}", taskName));

            // Now we do the query...
            var tasks = GetTaskDataFromPanda(url, useCacheIfPossible);
            var mytask = tasks.Where(t => t.taskname == taskName).FirstOrDefault();

            // Next, see if we need detailed info
            if (mytask != null && withDetailedDSInfo)
            {
                mytask = mytask.jeditaskid.FindPandaJobWithTaskName(true);
            }

            return mytask;
        }

        /// <summary>
        /// Given an ID fetch the task info
        /// </summary>
        /// <param name="taskID"></param>
        /// <param name="withDetailedDSInfo">Set true if we want detailed dataset info. This method always returns it, so setting it to false does nothing.</param>
        /// <returns></returns>
        public static PandaTask FindPandaJobWithTaskName(this int taskID, bool withDetailedDSInfo = false)
        {
            var url = BuildTaskUri(string.Format("tasks/?jeditaskid={0}", taskID));

            // Now we do the query...
            var tasks = GetTaskDataFromPanda(url);
            var mytask = tasks.FirstOrDefault();
            return mytask;
        }

        /// <summary>
        /// Gets the list of tasks back
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static PandaTask[] GetTaskDataFromPanda(Uri url, bool useCacheIfPossible = true)
        {
            // If it is located in the cache, then pull it.
            // If we aren't spposed to used the cache, then ignore it - unless the status is "done".
            var cached = PullFromCache(url);
            if (cached != null && (useCacheIfPossible || cached[0].status == "done"))
            {
                return cached;
            }

            // Do a full web request.
            // We've seen some 
            Trace.WriteLine($"GetTaskDataFromPanda: Querying PandDA at {url.OriginalString}.", "PandaUtils");
            var wr = WebRequest.CreateHttp(url);
            wr.Accept = "application/json";

            return Policy
                .Handle<WebException>()
                .WaitAndRetry(10, cnt => TimeSpan.FromSeconds(5))
                .Execute(() =>
                {
                    using (var data = wr.GetResponse())
                    {
                        using (var rdr = data.GetResponseStream())
                        {
                            using (var r = new StreamReader(rdr))
                            {
                                var text = r.ReadToEnd();
                                try
                                {
                                    var result = JsonConvert.DeserializeObject<PandaTask[]>(text);
                                    if (!result.Where(t => t.status != "done" && t.status != "finished").Any())
                                    {
                                        SendToCache(url, result);
                                    }
                                    return result;
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine($"Error parsing JSON back from {url.OriginalString}. JSON was: '{text}' (error: {e.Message}).");
                                    throw;
                                }
                            }
                        }
                    }
                });
        }

        /// <summary>
        /// What directory in app data should we store the cache in?
        /// </summary>
        private static string _cacheDirectoryName = "PandaCache";

        /// <summary>
        /// Reset the Lazy cache object. Used only for testing.
        /// </summary>
        public static void ResetCache(string cacheDirName = null)
        {
            _cacheDirectoryName = cacheDirName == null ? "PandaCache" : cacheDirName;
            if (_cacheLocation.IsValueCreated)
            {
                _cacheLocation = new Lazy<DirectoryInfo>(() => {
                    var d = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _cacheDirectoryName));
                    if (!d.Exists)
                    {
                        d.Create();
                    }
                    return d;
                });
            }
        }

        /// <summary>
        /// Where the directory cache for panda info is located.
        /// </summary>
        private static Lazy<DirectoryInfo> _cacheLocation = new Lazy<DirectoryInfo> (() => {
            var d = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), _cacheDirectoryName));
            if (!d.Exists)
            {
                d.Create();
            }
            return d;
        });

        /// <summary>
        /// Write out the result to a cache so we don't have to do the lookup next time.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="result"></param>
        private static void SendToCache(Uri url, PandaTask[] result)
        {
            if (result.Length == 0)
            {
                return;
            }

            FileInfo cFile = CacheFile(url);
            using (var wrt = cFile.CreateText())
            {
                wrt.Write(JsonConvert.SerializeObject(result));
                wrt.Close();
            }
        }

        /// <summary>
        /// Generate the cache filename
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static FileInfo CacheFile(Uri url)
        {
            return new FileInfo($"{Path.Combine(_cacheLocation.Value.FullName, url.OriginalString.ComputeMD5Hash())}.json");
        }

        /// <summary>
        /// See if we can pull it from the cache.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static PandaTask[] PullFromCache(Uri url)
        {
            var f = CacheFile(url);
            if (!f.Exists)
            {
                return null;
            }
            Trace.WriteLine($"PullFromCache: Pulling PandDA data for {url.OriginalString} from cache file {f.FullName}.", "PandaUtils");
            using (var rdr = f.OpenText())
            {
                return JsonConvert.DeserializeObject<PandaTask[]>(rdr.ReadToEnd());
            }
        }

        /// <summary>
        /// Simple formatter to build the stem.
        /// </summary>
        /// <param name="stem"></param>
        /// <returns></returns>
        private static Uri BuildTaskUri(string stem)
        {
            return new Uri(string.Format("http://bigpanda.cern.ch/{0}&days=365", stem));
        }

        /// <summary>
        /// Returns all the dataset containers that are used in a particular task.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="streamname"></param>
        /// <returns></returns>
        public static string[] DatasetNames(this PandaTask task, string streamname)
        {
            return task.datasets.Where(ds => ds.streamname == streamname).GroupBy(ds => ds.containername).Select(k => k.Key).ToArray();
        }

        public static string[] DatasetNamesIN(this PandaTask task)
        {
            return task.DatasetNames("IN");
        }

        public static string[] DatasetNamesOUT(this PandaTask task)
        {
            return task.DatasetNames("OUTPUT0");
        }
    }
}
