using NHibernate;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Flags;
using RadialReview.Models;
using RadialReview.Models.ClientSuccess;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.Payments;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ClientSuccessController : BaseController {

		public class CloseVM {
			public long Id { get; set; }
			public string Name { get; set; }
			public DateTime CreateTime { get; set; }
			public DateTime? Expiration { get; set; }
			public int MeetingCount { get; set; }
			public AccountType AccountType { get; set; }
			public List<OrganizationFlag> Flags { get; set; }
			public DateTime? LastLogin { get; set; }
		}


		[HttpGet]
		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> ToggleOrganizationFlag(OrganizationFlagType type, long orgId, bool enabled) {
			await OrganizationAccessor.SetFlag(GetUser(), orgId, type, enabled);
			return Json(enabled, JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Close(AccountType accountType = AccountType.Demo) {
			Response.Cache.SetCacheability(HttpCacheability.NoCache);

			var output = new List<CloseVM>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var orgsQ = s.QueryOver<OrganizationModel>()
								.Where(x => x.DeleteTime == null && x.AccountType == accountType)
								.Future();

					var tokensQ = s.QueryOver<PaymentSpringsToken>()
									.Where(x => x.Active == true && x.DeleteTime == null)
									.Future();

					//var orgs = orgsQ.ToList();-
					OrganizationModel orgAlias = null;
					var meetingsQ = s.QueryOver<L10Meeting>()
											.JoinAlias(x => x.Organization, () => orgAlias)
											.Where(x => x.DeleteTime == null && x.CompleteTime != null && orgAlias.AccountType == accountType)
											.Select(x => x.CreateTime, x => x.CompleteTime, x => x.OrganizationId)
											.Future<object[]>();
					var flagsQ = s.QueryOver<OrganizationFlag>()
									.JoinAlias(x => x.Organization, () => orgAlias)
									.Where(x => x.DeleteTime == null && orgAlias.AccountType == accountType)
									.Future();

					var maxLoginQ = s.QueryOver<UserLookup>()
									.JoinAlias(x => x._Organization, () => orgAlias)
									.Where(x => x.DeleteTime == null && orgAlias.AccountType == accountType)
									.SelectList(list => list.SelectGroup(x => x.OrganizationId).SelectMax(x => x.LastLogin))
									.Future<object[]>();

					var meetings = meetingsQ.Select(x => new {
						Duration = ((DateTime)x[1] - (DateTime)x[0]),
						OrgId = (long)x[2]
					}).ToList();

					var lastLogin = maxLoginQ.Select(x => new { OrgId = (long)x[0], LastLogin = (DateTime?)x[1] }).ToDictionary(x => x.OrgId, x => x.LastLogin);

					var flagsLu = flagsQ.GroupBy(x => x.OrganizationId).ToDictionary(x => x.Key, x => x.ToList());
					var orgMeetingCount = meetings.Where(x => x.Duration > TimeSpan.FromMinutes(30))
												.GroupBy(x => x.OrgId)
												.ToDictionary(x => x.Key, x => x.Count());
					var tokens = tokensQ.ToDictionary(x => x.OrganizationId, x => x);
					var orgs = orgsQ.ToList();
					foreach (var org in orgs) {
						DateTime? trialEnd = DateTime.MinValue;
						try {
							trialEnd = tokens.ContainsKey(org.Id) ? (DateTime?)null : org.NotNull(u => u.PaymentPlan.NotNull(z => z.FreeUntil));
						} catch (Exception) {
						}

						var o = new CloseVM() {
							CreateTime = org.CreationTime,
							Name = org.GetName(),
							Expiration = trialEnd,
							MeetingCount = orgMeetingCount.GetOrDefault(org.Id, 0),
							Id = org.Id,
							Flags = flagsLu.GetOrDefault(org.Id, new List<OrganizationFlag>()),
							AccountType = org.AccountType,
							LastLogin = lastLogin.GetOrDefault(org.Id, null)
						};
						output.Add(o);

					}
				}
			}

			return View(output);
		}

		// GET: ClientSuccess
		[Access(AccessLevel.UserOrganization)]
		public JsonResult MarkTooltip(long id) {
			try {
				SupportAccessor.MarkTooltipSeen(GetUser(), id);
				return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
			} catch (Exception e) {
				var result = ResultObject.SilentSuccess(e);
				result.Error = true;
				result.ForceNoErrorReport();
				return Json(result, JsonRequestBehavior.AllowGet);
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Tooltips() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var tips = s.QueryOver<TooltipTemplate>().List().ToList();
					return View(tips);
				}
			}
		}
		[Access(AccessLevel.Radial)]
		public PartialViewResult Modal(long id = 0) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var tip = s.Get<TooltipTemplate>(id) ?? new TooltipTemplate();
					return PartialView(tip);
				}
			}
		}
		[Access(AccessLevel.Radial)]
		[HttpPost]
		public JsonResult Modal(TooltipTemplate model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Merge(model);
					tx.Commit();
					s.Flush();
					return Json(ResultObject.Create(model));
				}
			}
		}
	}
}
