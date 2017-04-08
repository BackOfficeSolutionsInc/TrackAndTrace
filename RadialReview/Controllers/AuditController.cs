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

namespace RadialReview.Controllers
{
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
					var revIds = s.AuditReader().GetRevisions(typeof(RockModel),id);
					var models = revIds.Select(revId => new KeyValuePair<long,RockModel>(revId,s.AuditReader().Find<RockModel>(id,revId))).ToList();
					return View(models);
				}
			}
		}
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Score(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var ids = s.Auditer().GetRevisions(typeof(ScoreModel), id);
					var models = s.Auditer().FindRevisions<ScoreModel>(ids).ToList();
					return View(models);
				}
			}
		}
		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult Measurable(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var ids = s.Auditer().GetRevisions(typeof(MeasurableModel), id);
					var models = s.Auditer().FindRevisions<MeasurableModel>(ids).ToList();
					return View(models);
				}
			}
		}
	}
}