using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Pdf {
    public class LayoutTimeoutException : Exception {
        public LayoutTimeoutException() { }
        public LayoutTimeoutException(string message) : base(message) { }
        public LayoutTimeoutException(string message, Exception inner) : base(message, inner) { }
       
    }
}