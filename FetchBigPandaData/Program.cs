using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FetchBigPandaData
{
    class Program
    {
        /// <summary>
        /// Fetch data from big-panda in JSON format
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // http://bigpanda.cern.ch/

            // Things known to work: 
            //   tasks/?taskname=user.gwatts*&days=20
            //   tasks/?jeditaskid=6868837

            // Stuff that doesn't seem to work:
            //   task/6868837

            if (args.Length != 1)
            {
                Console.WriteLine("Only one argument, the stem after the http://bigpanda.cern.ch, is accepted.");
                return;
            }
            var url = string.Format("http://bigpanda.cern.ch/{0}", args[0]);

            var wr = WebRequest.CreateHttp(url);
            wr.Accept = "application/json";
            //wr.ContentType = "application/json";

            using (var data = wr.GetResponse())
            {
                using (var rdr = data.GetResponseStream())
                {
                    using (var r = new StreamReader(rdr))
                    {
                        Console.WriteLine(r.ReadToEnd());
                    }
                }
            }
            
        }
    }
}
