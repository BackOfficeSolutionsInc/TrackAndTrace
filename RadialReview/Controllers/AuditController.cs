using NHibernate.Envers;
using RadialReview.Models.Askables;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;
using RadialReview.Utilities.NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class AuditController : BaseController {
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Index() {
			return View();
		}
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Rocks(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var revIds = s.AuditReader().GetRevisions(typeof(RockModel), id);
					var models = revIds.Select(revId => new Tuple<long, DateTime, RockModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<RockModel>(id, revId))).ToList();
					return View(models);
				}
			}
		}
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Scores(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var revIds = s.AuditReader().GetRevisions(typeof(ScoreModel), id);
					var models = revIds.Select(revId => new Tuple<long, DateTime, ScoreModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<ScoreModel>(id, revId))).ToList();
					return View(models);
				}
			}
		}
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Measurables(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var revIds = s.AuditReader().GetRevisions(typeof(MeasurableModel), id);
					var models = revIds.Select(revId => new Tuple<long, DateTime, MeasurableModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<MeasurableModel>(id, revId))).ToList();

					models.ForEach(x => {
						x.Item3.AccountableUser.GetName();
						x.Item3.AdminUser.GetName();
					});

					return View(models);
				}
			}
		}

		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Roles(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var revIds = s.AuditReader().GetRevisions(typeof(RoleModel), id);
					var models = revIds.Select(revId => new Tuple<long, DateTime, RoleModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<RoleModel>(id, revId))).ToList();

					return View(models);
				}
			}
		}

	}
}