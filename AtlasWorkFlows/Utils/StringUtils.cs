using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Utils
{
    static class StringUtils
    {
        public static string Dataset(this string dsName)
        {
            var colon = dsName.IndexOf(':');
            if (colon >= 0)
            {
                dsName = dsName.Substring(colon + 1);
            }
            return dsName;
        }
    }
}
