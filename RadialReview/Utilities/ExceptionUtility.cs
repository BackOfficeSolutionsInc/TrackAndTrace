using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Web;

namespace RadialReview.Utilities {
    public class ExceptionUtility {
        public static void PreserveStackTrace(Exception e)
        {
            var ctx = new StreamingContext(StreamingContextStates.CrossAppDomain);
            var mgr = new ObjectManager(null, ctx);
            var si = new SerializationInfo(e.GetType(), new FormatterConverter());

            e.GetObjectData(si, ctx);
            mgr.RegisterObject(e, 1, si); // prepare for SetObjectData
            mgr.DoFixups(); // ObjectManager calls SetObjectData

            // voila, e is unmodified save for _remoteStackTraceString
        }

		public static bool IsInException() {
			return Marshal.GetExceptionPointers() != IntPtr.Zero || Marshal.GetExceptionCode() != 0;
		}
    }
}