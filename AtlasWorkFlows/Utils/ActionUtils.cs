using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    static class ActionUtils
    {
        /// <summary>
        /// Make the call on the action only if the func is not null.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="arg"></param>
        public static void PCall<T>(this Action<T> func, T arg)
        {
            if (func != null)
                func(arg);
        }

        public static T PCall<T>(this Func<T> func, T def)
        {
            if(func != null)
            {
                return func();
            }
            return def;
        }
    }
}
