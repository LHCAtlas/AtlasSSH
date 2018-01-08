using System;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    class CacheObject<T>
    {
        T _cache;

        bool _determined = false;

        public T Get (Func<T> calc)
        {
            if (!_determined)
            {
                _cache = calc();
                _determined = true;
            }
            return _cache;
        }

        public async Task<T> GetAsync(Func<Task<T>> calc)
        {
            if (!_determined)
            {
                _cache = await calc();
                _determined = true;
            }
            return _cache;
        }
    }
}
