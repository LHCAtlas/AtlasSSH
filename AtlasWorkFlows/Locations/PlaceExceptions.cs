using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasWorkFlows.Locations
{
    /// <summary>
    /// Thrown when we are expecting a gridds but find something else.
    /// </summary>
    [Serializable]
    public class UnknownUriSchemeException : Exception
    {
        public UnknownUriSchemeException() { }
        public UnknownUriSchemeException(string message) : base(message) { }
        public UnknownUriSchemeException(string message, Exception inner) : base(message, inner) { }
        protected UnknownUriSchemeException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DatasetDoesNotExistInThisReproException : Exception
    {
        public DatasetDoesNotExistInThisReproException() { }
        public DatasetDoesNotExistInThisReproException(string message) : base(message) { }
        public DatasetDoesNotExistInThisReproException(string message, Exception inner) : base(message, inner) { }
        protected DatasetDoesNotExistInThisReproException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
