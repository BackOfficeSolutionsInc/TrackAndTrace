using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static RadialReview.Controllers.ArchiveController.ArchiveVM;

namespace RadialReview.Controllers {
	public class ArchiveController : BaseController {

		public class ArchiveVM {
			public class ArchiveItemVM {
				public long Id { get; set; }
				public DateTime? DeleteTime { get; set; }
				public String Name { get; set; }
				public string Owner { get; internal set; }
			}

			public String Title { get; set; }
			public IEnumerable<ArchiveItemVM> Objects {get;set; }
			/// <summary>
			/// {0} is replaced by the Id
			/// </summary>
			public String UndeleteUrl { get; set; }
			public String AuditUrl { get; set; }
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Index() {
			return View();
		}


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

					return View(l10s.Select(x => new { Name = x.Name, Id = x.Id }).ToList());

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Measurables() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime != null && x.Organization.Id == user.Organization.Id).List().ToList();

					var model = new ArchiveVM {
						Title = "Measurables",
						Objects = measurables.Select(x => new ArchiveItemVM { Name = x.Title, Id = x.Id, DeleteTime = x.DeleteTime, Owner= x.AccountableUser.NotNull(y=>y.GetName()) }).ToList(),
						UndeleteUrl = "/measurable/undelete/{0}",
						AuditUrl = "/audit/measurables/{0}"
					};

					return View("Table",model );

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Rocks() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rocks = s.QueryOver<RockModel>().Where(x => x.DeleteTime != null && x.OrganizationId == user.Organization.Id).List().ToList();
					var model = new ArchiveVM {
						Title = "Rocks",
						Objects = rocks.Select(x => new ArchiveItemVM { Name = x.Rock, Id = x.Id, DeleteTime = x.DeleteTime, Owner = x.AccountableUser.NotNull(y => y.GetName()) }).ToList(),
						UndeleteUrl = "/rocks/undelete/{0}",
						AuditUrl = "/audit/rocks/{0}"
					};
					return View("Table", model);

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Roles() {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var rocks = s.QueryOver<RoleModel>().Where(x => x.DeleteTime != null && x.OrganizationId == user.Organization.Id).List().ToList();
					var model = new ArchiveVM {
						Title = "Roles",
						Objects = rocks.Select(x => new ArchiveItemVM { Name = x.Role, Id = x.Id, DeleteTime = x.DeleteTime, Owner = "" }).ToList(),
						UndeleteUrl = "/roles/undelete/{0}",
						AuditUrl = "/audit/roles/{0}"
					};
					return View("Table", model);

				}
			}
		}
	}
}