using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net;

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
        public static PandaTask FindPandaJobWithTaskName(this string taskName, bool withDetailedDSInfo = false)
        {
            var url = BuildTaskUri(string.Format("tasks/?taskname={0}", taskName));

            // Now we do the query...
            var tasks = GetTaskDataFromPanda(url);
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
        private static PandaTask[] GetTaskDataFromPanda(Uri url)
        {
            var wr = WebRequest.CreateHttp(url);
            wr.Accept = "application/json";
            //wr.ContentType = "application/json";

            using (var data = wr.GetResponse())
            {
                using (var rdr = data.GetResponseStream())
                {
                    using (var r = new StreamReader(rdr))
                    {
                        var text = r.ReadToEnd();
                        try
                        {
                            return JsonConvert.DeserializeObject<PandaTask[]>(text);
                        } catch (Exception e)
                        {
                            Console.WriteLine($"Error parsing JSON back from {url.OriginalString}. JSON was: '{text}' (error: {e.Message}).");
                            throw;
                        }
                    }
                }
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
