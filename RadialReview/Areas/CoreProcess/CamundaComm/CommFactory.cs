using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
    public class CommFactory {
        private CommFactory() {}
        /// <summary>
        /// Allows us to hot swap the implementation of ICommClass any time. Very useful for testing or for replacing certain methods in the implementation
        /// </summary>
        /// <returns></returns>
        public static ICommClass Get() {
            return new CommClass();
        }
    }
}