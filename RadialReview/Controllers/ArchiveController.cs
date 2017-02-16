using RadialReview.Models;
using RadialReview.Models.L10;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ArchiveController : BaseController {

		[Access(AccessLevel.Manager)]
		public ActionResult Users() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//tx.Commit();
					//s.Flush();

					var users = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id)
						.List().ToList();

					return View(users);

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult L10() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//tx.Commit();
					//s.Flush();

					var l10s = s.QueryOver<L10Recurrence>()
						.Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id)
						.List().ToList();
					
					return View(l10s.Select(x=>new { Name = x.Name, Id = x.Id }).ToList());

				}
			}
		}
	}
}