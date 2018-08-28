using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Synchronize {
    public class CancellationException : Exception {
        public CancellationException() { }
        public CancellationException(string message) : base(message) { }
    }
}
