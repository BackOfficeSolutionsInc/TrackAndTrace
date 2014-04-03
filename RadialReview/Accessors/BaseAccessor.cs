using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RadialReview.Accessors
{
    public class BaseAccessor
    {
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected static Dictionary<string, object> CacheLookup = new Dictionary<string, object>();

    }
}
