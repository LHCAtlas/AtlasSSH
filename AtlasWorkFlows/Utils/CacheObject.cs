using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
