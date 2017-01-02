using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    static class Exceptions
    {
        /// <summary>
        /// Functional throw.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static T ThrowIfNull<T>(this T obj, Func<Exception> makeException)
        {
            if (obj != null)
                return obj;

            throw makeException();
        }

    }
}
