using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    class InMemoryObjectCache<T>
    {
        private Dictionary<string, T> _keyStore = new Dictionary<string, T>();

        /// <summary>
        /// Invalidate an item
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
            if (_keyStore.ContainsKey(key))
            {
                _keyStore.Remove(key);
            }
        }

        /// <summary>
        /// Get or calc a cache line.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="calculate"></param>
        /// <returns></returns>
        public T GetOrCalc(string key, Func<T> calculate)
        {
            if (_keyStore.ContainsKey(key))
            {
                return _keyStore[key];
            }

            var r = calculate();
            _keyStore[key] = r;
            return r;
        }
    }
}
