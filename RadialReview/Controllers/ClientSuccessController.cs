using FluentNHibernate.Mapping;
using NHibernate;
using RadialReview.Accessors;
using RadialReview.Crosscutting.Flags;
using RadialReview.Models;
using RadialReview.Models.Angular.Organization;
using RadialReview.Models.Angular.Users;
using RadialReview.Models.ClientSuccess;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
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
using System.Web.Script.Serialization;

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

		[Access(AccessLevel.RadialData)]
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

		public class HFCategory : ILongIdentifiable, IHistorical {

			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual long ParentId { get; set; }
			public virtual string Name { get; set; }
			public virtual string EmailTemplate { get; set; }
			public virtual long CreatorId { get; set; }

			public virtual long[] ForTeams { get { return (_ForTeams ?? "").Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLong()).ToArray(); } set { _ForTeams = string.Join("~", value); } }
			public virtual long[] ForUsers { get { return (_ForUsers ?? "").Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLong()).ToArray(); } set { _ForUsers = string.Join("~", value); } }
			public virtual string[] AdditionalTags { get { return (_AdditionalTags ?? "").Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries).ToArray(); } set { _AdditionalTags = string.Join("~", value.Where(x => !string.IsNullOrWhiteSpace(x))); } }

			[ScriptIgnore]
			public virtual string _ForTeams { get; set; }
			[ScriptIgnore]
			public virtual string _ForUsers { get; set; }
			[ScriptIgnore]
			public virtual string _AdditionalTags { get; set; }
			[ScriptIgnore]
			public virtual long? CategoryId { get; set; }
			[ScriptIgnore]
			public virtual string[] _CategoryTags { get; set; }

			public virtual string[] AllTags {
				get {
					var builder = AdditionalTags.Where(x => x != null).ToList();
					if (_CategoryTags != null)
						builder.AddRange(_CategoryTags.Where(x => x != null).Select(x => "cat:" + x));
					return builder.Where(x => x != null).ToArray();
				}
			}
			public virtual List<HFCategory> _Children { get; set; }

			public class Map : ClassMap<HFCategory> {
				public Map() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.ParentId);
					Map(x => x.Name);
					Map(x => x.EmailTemplate).Length(6000);
					Map(x => x.CreatorId);
					Map(x => x._ForTeams);
					Map(x => x._ForUsers);
					Map(x => x._AdditionalTags);
					Map(x => x.CategoryId);
				}
			}
		}

		private void Recurse(HFCategory parent, List<HFCategory> children, List<string> parentNames) {
			var parents = children.Where(x => x.ParentId == parent.Id).ToList();
			var output = new List<HFCategory>();
			var myCat = parentNames.ToList();
			myCat.Add(parent.Name);
			parent._CategoryTags = myCat.ToArray();
			var pns = parentNames.ToList();
			pns.Add(parent.Name);
			foreach (var p in parents) {
				output.Add(p);
				Recurse(p, children, pns);
			}
			parent._Children = output;
		}

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> HFGetCategories() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var cats = s.QueryOver<HFCategory>().Where(x => x.DeleteTime == null).List().ToList();
					var parent = new HFCategory() { Id = 0 };
					Recurse(parent, cats, new List<string>());
					return Json(parent._Children, JsonRequestBehavior.AllowGet);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> HFCategories() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var cats = s.QueryOver<HFCategory>().List().ToList();
					return View(cats);
				}
			}
		}

		[HttpPost]
		[ValidateInput(false)]
		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> HFAddCategory(long parent, string name, string template, string tags = null, long? categoryId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var c = new HFCategory() {
						Name = name,
						ParentId = parent,
						EmailTemplate = template,
						CreatorId = GetUser().Id,
						CategoryId = categoryId,
						_AdditionalTags = tags
					};
					s.Save(c);
					tx.Commit();
					s.Flush();
					return Json(c);
				}
			}
		}
		[HttpPost]
		[ValidateInput(false)]
		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> HFEditCategory(long id, bool? delete = null, long? parent = null, string name = null, string template = null, string tags = null, long? categoryId = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var c = s.Get<HFCategory>(id);
					if (delete != null)
						c.DeleteOrUndelete(s, delete.Value);
					c.ParentId = parent ?? c.ParentId;
					c.Name = name ?? c.Name;
					c.EmailTemplate = template ?? c.EmailTemplate;
					c._AdditionalTags = tags ?? c._AdditionalTags;
					c.CategoryId = categoryId ?? c.CategoryId;

					s.Update(c);
					tx.Commit();
					s.Flush();
					return Json(c);
				}
			}
		}

		[HttpPost]
		[ValidateInput(false)]
		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> HFDeleteCategory(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var c = s.Get<HFCategory>(id);
					c.DeleteTime = DateTime.UtcNow;
					s.Update(c);
					tx.Commit();
					s.Flush();
				}
			}
			return Json(true);
		}


		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> HFUserInfo(string search) {

			var results = SearchAccessor.AdminSearchAllUsers(GetUser(), search).Where(x => x.ResultType == RGMType.User).ToList();
			//var found = results.SingleOrDefault(x => x.Email == search);
			//if (found == null)
			//	return Json(ResultObject.CreateError("no user"), JsonRequestBehavior.AllowGet);

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(results.Select(x => x.OrganizationId).Distinct().ToList()).List().ToDictionary(x => x.Id, x => x);
					var users = s.QueryOver<UserOrganizationModel>().WhereRestrictionOn(x => x.Id).IsIn(results.Select(x => x.Id).Distinct().ToList()).List().ToDictionary(x => x.Id, x => x);

					var output = results.Select(x => new {
						deleted = orgs[x.OrganizationId].DeleteTime != null || users[x.Id].DeleteTime != null,
						org = new AngularOrganizationUnsafe(orgs[x.OrganizationId]),
						user = AngularUser.CreateUser(users[x.Id]),
						position = users[x.Id].Cache.Positions,
						search = x,
					}).ToList();
					return Json(ResultObject.Create(output), JsonRequestBehavior.AllowGet);
				}
			}
		}
	}
}
