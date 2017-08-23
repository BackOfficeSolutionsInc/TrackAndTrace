using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CamundaCSharpClient.Model.Deployment
{
    [Serializable]
    public class EngineException : Exception
    {
        public EngineException() { }

        public EngineException(string message) : base(message) { }

        public EngineException(string message, Exception innerException) : base(message, innerException) { }

        protected EngineException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
