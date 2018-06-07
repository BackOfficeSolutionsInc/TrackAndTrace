using NHibernate;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class LegalController : BaseController {

		[Access(AccessLevel.Any)]
		public ActionResult Index() {
			return View("Privacy");
		}
		//
		// GET: /Legal/
		[Access(AccessLevel.Any)]
        public ActionResult Privacy()
        {
			var url = "";
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					url = s.GetSettingOrDefault(Variable.Names.PRIVACY_URL, "");
					tx.Commit();
					s.Flush();
				}
			}
			
			if (string.IsNullOrWhiteSpace(url))
				return View();
			else
				return Redirect(url);
        }
        [Access(AccessLevel.Any)]
        public ActionResult TOS() {
			var url = "";
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					url = s.GetSettingOrDefault(Variable.Names.TOS_URL, "");
					tx.Commit();
					s.Flush();
				}
			}
			if (string.IsNullOrWhiteSpace(url))
				return View();
			else
				return Redirect(url);
        }
	}
}