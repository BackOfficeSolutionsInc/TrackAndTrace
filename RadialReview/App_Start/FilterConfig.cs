using System.Web;
using System.Web.Mvc;

namespace RadialReview
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
			//GlobalFilters.Filters.Add(new RequireHttpsAttribute());
			filters.Add(new HandleErrorAttribute());
			filters.Add(new HttpRequestValidationExceptionAttribute());

		}

		public class HttpRequestValidationExceptionAttribute : FilterAttribute, IExceptionFilter {

			public void OnException(ExceptionContext filterContext) {
				if (!filterContext.ExceptionHandled && filterContext.Exception is HttpRequestValidationException) {
					filterContext.Result = new RedirectResult("~/");
					filterContext.ExceptionHandled = true;
				}
			}
		}
	}
}
