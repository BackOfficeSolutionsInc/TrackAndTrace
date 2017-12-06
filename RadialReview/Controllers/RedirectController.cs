using NHibernate;
using RadialReview.Accessors;
using RadialReview.Utilities;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class RedirectController : BaseController {

		[Access(AccessLevel.Any)]
		public ActionResult BuyBooks() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var url = s.GetSettingOrDefault("EOS_LIBRARY_LINK", "https://www.eosworldwide.com/traction-library");
					tx.Commit();
					s.Flush();
					return Redirect(url);
				}
			}
		}

	}
}