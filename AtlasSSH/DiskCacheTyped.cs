using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public class DiskCacheTyped<T>
        where T: class
    {
        /// <summary>
        /// Track where we store this stuff.
        /// </summary>
        readonly DiskCache _cache;

        public DiskCacheTyped(string name)
        {
            _cache = new DiskCache(name);
        }
        
        public T this[string key]
        {
            get
            {
                var o = _cache.Get(key) as string;
                return o == null
                    ? (T) null
                    : JsonConvert.DeserializeObject<T>(o);
            }

            set
            {
                _cache.Set(key, JsonConvert.SerializeObject(value), null);
            }
        }

    }

    /// <summary>
    /// Helper fuctions
    /// </summary>
    public static class DiskCacheTypedHelpers
    {
        /// <summary>
        /// Return the cache value, or do the calculation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheName"></param>
        /// <param name="key"></param>
        /// <param name="calculateValue"></param>
        /// <returns></returns>
        public static T NonNullCacheInDisk<T>(string cacheName, string key, Func<T> calculateValue)
            where T : class
        {
            var c = new DiskCacheTyped<T>(cacheName);
            var r = c[key];
            if (r == null)
            {
                r = calculateValue();
                if (r != null)
                {
                    c[key] = r;
                }
            }
            return r;
        }
        /// <summary>
        /// Return the cache value, or do the calculation.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheName"></param>
        /// <param name="key"></param>
        /// <param name="calculateValue"></param>
        /// <returns></returns>
        public static async Task<T> NonNullCacheInDiskAsync<T>(string cacheName, string key, Func<Task<T>> calculateValue)
            where T : class
        {
            var c = new DiskCacheTyped<T>(cacheName);
            var r = c[key];
            if (r == null)
            {
                r = await calculateValue();
                if (r != null)
                {
                    c[key] = r;
                }
            }
            return r;
        }
    }
}
