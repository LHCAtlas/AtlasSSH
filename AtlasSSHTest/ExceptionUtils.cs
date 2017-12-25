using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSHTest
{
    public static class ExceptionUtils
    {
        /// <summary>
        /// Unroll nested aggregate exceptions. If there is only one down there, just return it.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static Exception UnrollAggregateExceptions(this Exception exp)
        {
            while (exp is AggregateException)
            {
                var a = exp as AggregateException;
                if (a.InnerExceptions.Count == 1)
                {
                    exp = a.InnerExceptions.First();
                }
                else
                {
                    return exp;
                }
            }
            return exp;
        }
    }
}
