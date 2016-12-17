using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public class DiskCache : ObjectCache
    {
        private DirectoryInfo _dir;
        private string _name;

        /// <summary>
        /// Initialize a cache in the given directory
        /// </summary>
        /// <param name="cacheDir"></param>
        public DiskCache (string name)
        {
            _dir = new DirectoryInfo(Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), name));
            _name = name;
        }

        public void Clear()
        {
            if (_dir.Exists)
            {
                _dir.Delete(true);
                _dir.Refresh();
            }
        }

        /// <summary>
        /// Get or Set the value of the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override object this[string key]
        {
            get
            {
                return Get(key);
            }

            set
            {
                Set(key, value, null);
            }
        }

        /// <summary>
        /// We are pretty simple, and quite stuipd.
        /// </summary>
        public override DefaultCacheCapabilities DefaultCacheCapabilities
        {
            get { return DefaultCacheCapabilities.None;}
        }

        /// <summary>
        /// Name of this cache.
        /// </summary>
        public override string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// return old entry if it is there.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="policy"></param>
        /// <returns></returns>
        public override CacheItem AddOrGetExisting(CacheItem value, CacheItemPolicy policy)
        {
            var old = Get(value.Key, value.RegionName);

            Set(value, policy);
            return old == null
                ? null
                : new CacheItem(value.Key, old, value.RegionName);
        }

        public override object AddOrGetExisting(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            var o = AddOrGetExisting(new CacheItem(key, value, regionName), policy);
            return o?.Value;
        }

        public override object AddOrGetExisting(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            return AddOrGetExisting(key, value, null, regionName);
        }

        public override bool Contains(string key, string regionName = null)
        {
            return Get(key, regionName) != null;
        }

        public override CacheEntryChangeMonitor CreateCacheEntryChangeMonitor(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fetch from the system.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public override object Get(string key, string regionName = null)
        {
            var f = GetCacheFileName(key);
            if (!f.Exists)
            {
                return null;
            }
            using (var rd = f.OpenRead())
            {
                var bn = new BinaryFormatter();
                return bn.Deserialize(rd);
            }
        }

        /// <summary>
        /// Don't fetch.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="policy"></param>
        public override void Set(CacheItem item, CacheItemPolicy policy)
        {
            var f = GetCacheFileName(item.Key);
            if (!f.Directory.Exists)
            {
                f.Directory.Create();
            }
            using (var wr = f.Create())
            {
                var bn = new BinaryFormatter();
                bn.Serialize(wr, item.Value);
                wr.Close();
            }
        }

        public override CacheItem GetCacheItem(string key, string regionName = null)
        {
            var o = Get(key, regionName);
            return o == null
                ? null
                : new CacheItem(key, o, regionName);
            throw new NotImplementedException();
        }

        public override long GetCount(string regionName = null)
        {
            throw new NotImplementedException();
        }

        public override IDictionary<string, object> GetValues(IEnumerable<string> keys, string regionName = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete this item.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="regionName"></param>
        /// <returns></returns>
        public override object Remove(string key, string regionName = null)
        {
            var o = Get(key, regionName);
            if (o != null)
            {
                var f = GetCacheFileName(key);
                f.Delete();
                return o;
            }
            return null;
        }

        /// <summary>
        /// Return the filename we are going to cache against.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private FileInfo GetCacheFileName (string key)
        {
            return new FileInfo(Path.Combine(_dir.FullName, key.Replace(":", "_"), ".binary"));
        }

        public override void Set(string key, object value, CacheItemPolicy policy, string regionName = null)
        {
            Set(new CacheItem(key, value, regionName), policy);
        }

        public override void Set(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            Set(new CacheItem(key, value, regionName), null);
        }

        protected override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
