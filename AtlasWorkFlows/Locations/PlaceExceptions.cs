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
    public class DataSetDoesNotExistInThisReproException : Exception
    {
        public DataSetDoesNotExistInThisReproException() { }
        public DataSetDoesNotExistInThisReproException(string message) : base(message) { }
        public DataSetDoesNotExistInThisReproException(string message, Exception inner) : base(message, inner) { }
        protected DataSetDoesNotExistInThisReproException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DataSetFileNotLocalException : Exception
    {
        public DataSetFileNotLocalException() { }
        public DataSetFileNotLocalException(string message) : base(message) { }
        public DataSetFileNotLocalException(string message, Exception inner) : base(message, inner) { }
        protected DataSetFileNotLocalException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
