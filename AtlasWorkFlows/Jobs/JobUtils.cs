using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Jobs
{
    /// <summary>
    /// Some utility routines to help with working with jobs
    /// </summary>
    static class JobUtils
    {
        /// <summary>
        /// Append an item to the array. Not efficeint, but...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="old"></param>
        /// <param name="newItem"></param>
        /// <returns></returns>
        public static T[] Append<T>(this T[] old, T newItem)
        {
            var addon = new T[] { newItem };
            if (old == null)
            {
                return addon;
            }
            return old.Concat(addon).ToArray();
        }

        /// <summary>
        /// Return the hash code for this job definition.
        /// </summary>
        /// <param name="J"></param>
        /// <returns></returns>
        public static int Hash (this AtlasJob J)
        {
            return J.Print().GetHashCode();
        }
    }
}
