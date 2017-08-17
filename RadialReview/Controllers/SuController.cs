using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class SuController : Controller
    {
        // GET: Su
        public ActionResult Index(string id="")
        {
			var firstName = "";
			var lastName = "";
			var phone = "";
			var type = "ForumTxt";
			
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var number = s.QueryOver<ExternalUserPhone>().Where(x => x.LookupGuid == id.ToUpper()).List().FirstOrDefault();
					if (number != null) {
						if (number.Name != null) {
							var nameParts = number.Name.Split(' ');
							if (nameParts.Count() > 0)
								firstName = nameParts.First().ToTitleCase();
							if (nameParts.Count() > 1)
								lastName = nameParts.Last().ToTitleCase();
						}
						phone = number.UserNumber;

						if (phone.Length == 11 && phone.ElementAt(0) == '1')
							phone = phone.Substring(1, 10);
						if (phone.Length == 12 && phone.StartsWith("+1"))
							phone = phone.Substring(2, 10);

					}
				}
			}

            return Redirect("https://mytractiontools.com/contact-us/?first="+firstName+"&last="+ lastName + "&phone="+phone+"&type="+type+ "&source=TractionForums#gform_1");

		}
    }
}