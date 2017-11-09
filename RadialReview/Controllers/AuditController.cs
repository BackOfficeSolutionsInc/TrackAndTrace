using NHibernate;
using NHibernate.Envers;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
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

		public class L10AuditData {
			public L10Recurrence L10 { get; set; }
			public List<L10Recurrence.L10Recurrence_Measurable> Measurables { get; set; }
		}

		[Access(AccessLevel.Radial)]
		public ActionResult L10(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var l10 = s.Get<L10Recurrence>(id);


					var measurables = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Fetch(x=>x.Measurable).Eager.Where(x => x.L10Recurrence.Id==id).List().ToList();
				//	var measurableModels = s.QueryOver<MeasurableModel>().WhereRestrictionOn(x => x.Id).IsIn(measurables.Where(x=>x.Measurable!=null).Select(x => x.Measurable.Id).ToList()).List().ToList();


					foreach (var m in measurables) {
						if (m.Measurable != null) {
							var a = m.Measurable.Title;
							var b = m.Measurable.DataContract_AccountableUser.Name;
							var c = m.Measurable.DataContract_AdminUser.Name;
						}
					}

					//var revIds = s.AuditReader().GetRevisions(typeof(MeasurableModel), id);
					//var models = revIds.Select(revId => new Tuple<long, DateTime, MeasurableModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<MeasurableModel>(id, revId))).ToList();

					//models.ForEach(x => {
					//	x.Item3.AccountableUser.GetName();
					//	x.Item3.AdminUser.GetName();
					//});

					return View(new L10AuditData() {
						L10 = l10,
						Measurables = measurables.OrderByDescending(x=>x.DeleteTime??DateTime.MaxValue).ToList(),
					});
				}
			}
		}

		public class MeasurableScoresAudit {
			public MeasurableModel Measurable { get; set; }
			public List<ScoreModel> ExistingScores { get; set; }
			public Dictionary<long, List<Revision<ScoreModel>>> Revisions {get;set;}
		}


		public class Revision<T> {
			public long RevId { get; set; }
			public DateTime RevDate { get; set; }
			public T Model { get; set; }
			public bool Error { get; set; }

			public Revision(long revId,DateTime revDate,T model) {
				RevId = revId;
				RevDate = revDate;
				Model = model;
			}

			public Revision() {

			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult UpdateScore(long scoreId,decimal? value) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var score = s.Get<ScoreModel>(scoreId);
					score.Measured = value;

					s.Update(score);

					tx.Commit();
					s.Flush();
				}
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}



		// GET: Audit
		[Access(AccessLevel.Radial)]
		public ActionResult MeasurableScores(long id) {
			var measurableId = id;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var measurable = s.Get<MeasurableModel>(id);

					var existingScores = s.QueryOver<ScoreModel>().Where(x => x.MeasurableId == measurableId).List().OrderBy(x=>x.ForWeek).ToList();

					var revisions = new Dictionary<long, List<Revision<ScoreModel>>>();

					foreach (var es in existingScores) {
						var revIds = s.AuditReader().GetRevisions(typeof(ScoreModel), es.Id);
						var models = revIds.Select(revId => {
							try {
								return new Revision<ScoreModel>(revId, s.AuditReader().GetRevisionDate(revId), s.AuditReader().Find<ScoreModel>(es.Id, revId));
							} catch (Exception e) {
								return new Revision<ScoreModel>() {
									Error = true,
								};
							}
						}).ToList();

						revisions[es.Id] = models;

					}

					return View(new MeasurableScoresAudit() {
						Measurable = measurable,
						ExistingScores = existingScores,
						Revisions = revisions,
					});
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