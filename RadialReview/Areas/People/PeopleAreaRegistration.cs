using System.Web.Mvc;

namespace RadialReview.Areas.People
{
    public class PeopleAreaRegistration : AreaRegistration 
    {
        public override string AreaName 
        {
            get 
            {
                return "People";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context) 
        {
            context.MapRoute(
                "People_default",
                "People/{controller}/{action}/{id}",
                new { controller = "Main", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}