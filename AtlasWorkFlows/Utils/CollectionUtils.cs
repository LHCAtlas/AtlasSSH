using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    static class CollectionUtils
    {
        /// <summary>
        /// Helper function for adding a range of items to a hash.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="set"></param>
        /// <param name="items"></param>
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                set.Add(item);
            }
        }
    }
}
