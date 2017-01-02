using AtlasWorkFlows.Jobs;
using AtlasWorkFlows.Locations;
using AtlasWorkFlows.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows
{
    /// <summary>
    /// A singleton that is the master dataset manager
    /// </summary>
    public class DatasetManager
    {
        /// <summary>
        /// Thrown if a dataset does not exist out there.
        /// </summary>
        [Serializable]
        public class DatasetDoesNotExistException : Exception
        {
            public DatasetDoesNotExistException() { }
            public DatasetDoesNotExistException(string message) : base(message) { }
            public DatasetDoesNotExistException(string message, Exception inner) : base(message, inner) { }
            protected DatasetDoesNotExistException(
              System.Runtime.Serialization.SerializationInfo info,
              System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
        }

        /// <summary>
        /// Given a dataset name, fine its list of files.
        /// </summary>
        /// <param name="dsname"></param>
        /// <returns>List of URI's of the files</returns>
        /// <exception cref="DatasetDoesNotExistException">Throw if <paramref name="dsname"/> does not exist.</exception>
        /// <remarks>
        /// Look through the list of places in tier order (from closests to furthest) until we find a good dataset.
        /// </remarks>
        public static Uri[] ListOfFilesInDataset (string dsname)
        {
            // Simply look through the list looking for anyone that has the dataset.
            // Recall the list is in tier order.
            return _places.Value
                .Select(p => p.GetListOfFilesForDataset(dsname))
                .Where(fs => fs != null)
                .Select(fs => fs.Select(f => new Uri($"gridds://{dsname}/{f}")).ToArray())
                .FirstOrDefault()
                .ThrowIfNull(() => new DatasetDoesNotExistException($"Dataset {dsname} could not be found at any place."));
        }

        /// <summary>
        /// Reset the IPlace list. Used only for testing, dangerous otherwise!
        /// </summary>
        /// <param name="places">List of places that we should use. If empty, then the default list will be used.</param>
        public static void ResetDSM(params IPlace[] places)
        {
            if (places.Length == 0)
            {
                _places = new Lazy<IPlace[]>(() => LoadPlaces());
            } else
            {
                _places = new Lazy<IPlace[]>(() => places.OrderBy(p => p.DataTier).ToArray());
            }
        }

        /// <summary>
        /// Keep a list of places. Only spin up if we need as it will involve some
        /// decent heavy-duty parsing of text files.
        /// </summary>
        /// <remarks>This list is sorted in Tier order all the time, starting from the lowest to the highest.</remarks>
        private static Lazy<IPlace[]> _places = new Lazy<IPlace[]>(() => LoadPlaces());

        /// <summary>
        /// Load places from the default place
        /// </summary>
        /// <returns></returns>
        private static IPlace[] LoadPlaces()
        {
            throw new NotImplementedException();
        }
    }
}
