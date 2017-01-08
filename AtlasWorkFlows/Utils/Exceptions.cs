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

        public static bool ThrowIfTrue(this bool r, Func<Exception> makeException)
        {
            if (r)
            {
                throw makeException();
            }
            return r;
        }

        public static bool ThrowIfFalse(this bool r, Func<Exception> makeException)
        {
            if (!r)
            {
                throw makeException();
            }
            return r;
        }

        public static T Throw<T>(this T obj, Func<T, bool> test, Func<T, Exception> makeException)
        {
            if (test(obj))
            {
                throw makeException(obj);
            }
            return obj;
        }

        public static IEnumerable<T> Throw<T>(this IEnumerable<T> obj, Func<T, bool> test, Func<T, Exception> makeException)
        {
            foreach(var t in obj)
            {
                if (test(t))
                {
                    throw makeException(t);
                }
                yield return t;
            }
        }
    }
}
