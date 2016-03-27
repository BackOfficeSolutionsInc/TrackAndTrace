using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;

namespace TractionTools.Tests.TestUtils {
    public class RouteHandler : IRouteHandler{
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            return new HttpHandler();
        }

        public class HttpHandler : DefaultHttpHandler{

        }
    }
}
