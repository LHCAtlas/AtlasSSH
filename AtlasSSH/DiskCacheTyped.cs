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
}
