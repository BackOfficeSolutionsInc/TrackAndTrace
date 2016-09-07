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

		}
	}
}
