
namespace AtlasSSH
{
    static class DataSetUtils
    {
        /// <summary>
        /// Remove things like the scope, etc., from the dataset name. It should be storable on windows/Linux when this is done.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns></returns>
        public static string SantizeDSName(this string dsnameRaw)
        {
            string dsname = dsnameRaw;
            var scopeMarker = dsname.IndexOf(":");
            if (scopeMarker >= 0)
            {
                dsname = dsname.Substring(scopeMarker + 1);
            }

            return dsname;
        }
    }
}
