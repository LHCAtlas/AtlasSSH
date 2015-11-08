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
        /// <returns></returns>
        public static PandaTask FindPandaJobWithTaskName(this string taskName)
        {
            var url = BuildTaskUri(string.Format("tasks/?taskname={0}", taskName));

            // Now we do the query...
            var tasks = GetTaskDataFromPanda(url);
            var mytask = tasks.Where(t => t.taskname == taskName).FirstOrDefault();
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
                        return JsonConvert.DeserializeObject<PandaTask[]>(r.ReadToEnd());
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
    }
}
