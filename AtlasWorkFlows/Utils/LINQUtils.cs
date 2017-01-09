using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    static class LINQUtils
    {
        /// <summary>
        /// Take elements in a sequence until the condition is true. Then take takeAfter elements after that. If takeAfter == 1, then take
        /// just the element that first matched the condition, if 2, then that one and the first one after, etc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="condition"></param>
        /// <param name="takeAfter"></param>
        /// <returns></returns>
        public static IEnumerable<T> TakeUntil<T>(this IEnumerable<T> source, Func<T, bool> condition, int takeAfter)
        {
            var iter = source.GetEnumerator();
            iter.MoveNext();
            while (iter.Current != null)
            {
                if (!condition(iter.Current))
                {
                    yield return iter.Current;
                    iter.MoveNext();
                } else
                {
                    break;
                }
            }

            // We have hit the condition, so now just walk till the end of the iterator.
            int counter = 0;
            while (counter < takeAfter && iter.Current != null)
            {
                yield return iter.Current;
                counter++;
                if (counter < takeAfter)
                {
                    iter.MoveNext();
                }
            }
        }
    }
}
