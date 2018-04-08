using System.IO;
using System.Text;
using CsvHelper;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Extensions;
using RadialReview.Models.Payments;
using RadialReview.Models.L10;
using NHibernate.Criterion;
using RadialReview.Models.Accountability;
using System.Threading;

namespace RadialReview.Controllers {
	public class OrganizationController : BaseController {
		//
		// GET: /Organization/
		[Access(AccessLevel.Any)]
		public ActionResult Index() {
			return View();
		}


		public class OrgStats {
			public long OrgId { get; set; }
			public string OrgName { get; set; }
			public string Username { get; set; }
			public DateTime? LastLogin { get; set; }
			public string Position { get; set; }
			public DateTime? OrgCreateTime { get; set; }
			public DateTime? TrialEnd { get; set; }
			public DateTime? LastMeeting { get; set; }
			public DateTime? CreditCardExp { get; set; }

			public AccountType Status { get; set; }
			public bool L10Enabled { get; internal set; }
			public bool EvalEnabled { get; internal set; }
			public bool PeopleEnabled { get; internal set; }
		}

		private static string[] supportNames = new[] { "Clay Upton", "Kathy Mayfield", "Maggie Marques", "Mike Miller", "Isaiah Nolte" };

		private static List<OrgStats> GenerateStats() {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).Future();
					var listQ = s.QueryOver<UserLookup>().Where(x => x.DeleteTime == null && !x.IsRadialAdmin)
						.WhereRestrictionOn(x => x.Name).Not.IsIn(supportNames)
						.OrderBy(x => x.LastLogin).Desc
						.SelectList(a =>
							a.Select(x => x.OrganizationId)
							 .Select(x => x.Name)
							 .Select(x => x.Positions)
							 .Select(x => x.LastLogin)
						)/*.Take(1)*/
						.Future<object[]>();

					var tokensQ = s.QueryOver<PaymentSpringsToken>().Where(x => x.Active == true && x.DeleteTime == null).Future();

					var meetingLastLUQ = s.QueryOver<L10Meeting>().Where(x => x.DeleteTime == null && x.CompleteTime != null)
						.OrderBy(x => x.CompleteTime).Desc.Future();

					var tokens = tokensQ.ToDictionary(x => x.OrganizationId, x => x);
					var meetingLastLU = meetingLastLUQ.ToList().GroupBy(x => x.OrganizationId).Select(x => x.First())
						.ToDictionary(x => x.OrganizationId, x => x.CompleteTime);
					//.Select(
					//    Projections.Distinct(Projections.Property("OrganizationId")),
					//    Projections.Property("CompleteTime")
					//).List<object[]>().ToDictionary(x => (long)x[0], x => (DateTime?)x[1]);

					var list = listQ.Select(x => new {
						OrganizationId = (long)x[0],
						Name = (string)x[1],
						Positions = (string)x[2],
						LastLogin = (DateTime?)x[3],
					}).ToList();

					var stats = orgs.Select(x => {
						var user = list.Where(y => y.OrganizationId == x.Id).OrderByDescending(y => y.LastLogin).FirstOrDefault();
						DateTime? trialEnd = DateTime.MinValue;
						try {
							trialEnd = tokens.ContainsKey(x.Id) ? (DateTime?)null : x.NotNull(u => u.PaymentPlan.NotNull(z => z.FreeUntil));
						} catch (Exception) {
						}

						return new OrgStats() {
							OrgId = x.NotNull(y => y.Id),
							OrgName = x.NotNull(y => y.GetName()),
							L10Enabled = x.NotNull(y => y.Settings.EnableL10),
							EvalEnabled = x.NotNull(y => y.Settings.EnableReview),
							PeopleEnabled = x.NotNull(y => y.Settings.EnablePeople),
							Username = user.NotNull(y => y.Name),
							Position = user.NotNull(y => y.Positions),
							LastLogin = user.NotNull(y => y.LastLogin),
							OrgCreateTime = x.NotNull(u => u.CreationTime),
							Status = x.NotNull(y => y.AccountType),
							LastMeeting = meetingLastLU.GetOrDefault(x.NotNull(y => y.Id), null),
							TrialEnd = trialEnd,
							CreditCardExp = !tokens.ContainsKey(x.Id) || tokens[x.Id].TokenType != PaymentSpringTokenType.CreditCard ? (DateTime?)null : new DateTime(tokens[x.Id].YearExpire, tokens[x.Id].MonthExpire, 1)
						};
					}).OrderByDescending(x => x.OrgId).ToList();

					return stats;

				}
			}
		}


		[Access(AccessLevel.RadialData)]
		public ActionResult Stats(string type = null) {
			var stats = GenerateStats();
			type = (type ?? "").ToLower();

			if (type != "csv")
				return View(stats);
			else {
				var csv = new Csv();
				csv.Title = "OrgId";
				foreach (var o in stats) {
					csv.Add("" + o.OrgId, "OrgName", o.OrgName);
					csv.Add("" + o.OrgId, "User", o.Username);
					csv.Add("" + o.OrgId, "Position", o.Position);
					csv.Add("" + o.OrgId, "CreateTime", o.OrgCreateTime.NotNull(x => x.Value.ToString()));
					csv.Add("" + o.OrgId, "Status", "" + o.Status);
					csv.Add("" + o.OrgId, "LastLogin", o.LastLogin.NotNull(x => x.Value.ToString()));
					csv.Add("" + o.OrgId, "TrialEnd", o.TrialEnd.ToString());
					csv.Add("" + o.OrgId, "LastMeeting", o.LastMeeting.NotNull(x => x.Value.ToString()));
					csv.Add("" + o.OrgId, "CreditCardExp", o.CreditCardExp.ToString());
					csv.Add("" + o.OrgId, "L10_Enabled", o.L10Enabled.ToString());
					csv.Add("" + o.OrgId, "Eval_Enabled", o.EvalEnabled.ToString());
					csv.Add("" + o.OrgId, "People_Enabled", o.PeopleEnabled.ToString());
				}

				return File(csv.ToBytes(), "text/csv", DateTime.UtcNow.ToJavascriptMilliseconds() + "_OrgStats.csv");
			}
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Invites() {
			var members = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails);

			var temps = members.Where(x => x.HasJoined == false).Select(x => UserAccessor.GetUserOrganization(GetUser(), x.UserId, true, false, PermissionType.EditEmployeeDetails).TempUser).Where(x => x != null).ToList();

			return View(temps);
		}

		//[Access(AccessLevel.Radial)]
		//public ActionResult StatsExport() {
		//	var stats = GenerateStats();
		//}

		[Access(AccessLevel.Radial)]
		public ActionResult Which(long? id = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					if (id == null) {
						var list = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).Fetch(x => x.PaymentPlan).Eager.List().ToList();
						foreach (var i in list) {
							var a = i.PaymentPlan.Id;
						}

						return View("WhichList", list);
					}

					Thread.SetData(Thread.GetNamedDataSlot("AllowSpecialOrgs"), true);

					var perms = PermissionsUtility.Create(s, GetUser());//.ManagingOrganization(id.Value);

					var org = s.Get<OrganizationModel>(id);
					var b = org.PaymentPlan.FreeUntil;

					ViewBag.Members = OrganizationAccessor.GetOrganizationMembersLookup(s, perms, id.Value, false);
					var meetings = s.QueryOver<L10Meeting>()
						.Fetch(x => x.L10Recurrence).Lazy
						.Where(x => x.OrganizationId == id && x.CreateTime > DateTime.UtcNow.AddDays(-90))
						.List().ToList();

					var attendees = s.QueryOver<L10Meeting.L10Meeting_Attendee>().WhereRestrictionOn(x => x.L10Meeting.Id).IsIn(meetings.Select(x => x.Id).ToList()).Select(x => x.L10Meeting.Id).List<long>().ToList();

					var attendeeslookup = attendees.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

					foreach (var m in meetings)
						m._MeetingAttendees = Enumerable.Range(0, attendeeslookup.GetOrDefault(m.Id, 0)).Select(x => (L10Meeting.L10Meeting_Attendee)null).ToList();

					ViewBag.Meetings = meetings;
					return View(org);

				}
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult SetAccountType(long id, AccountType type) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);

					if (org.AccountType == AccountType.Cancelled && type != AccountType.Cancelled) {
						org.DeleteTime = null;
					}
					if (type == AccountType.Cancelled && org.AccountType != AccountType.Cancelled) {
						org.DeleteTime = DateTime.UtcNow;
					}
					org.AccountType = type;
					s.Update(org);

					tx.Commit();
					s.Flush();

					return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
				}
			}

		}

		[Access(AccessLevel.Radial)]
		public JsonResult Delete(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, GetUser()).ManagingOrganization(id);

					var org = s.Get<OrganizationModel>(id);
					org.DeleteTime = DateTime.UtcNow;

					tx.Commit();
					s.Flush();

					return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
				}
			}
		}

		private void PrepareCreateOrgViewBag() {
			var implementers = ApplicationAccessor.GetCoaches(GetUser()).OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Id);
			implementers.Insert(0, new SelectListItem() { Text = "<select coach>", Value = "-1" });
			ViewBag.Implementers = implementers;

			var support = ApplicationAccessor.GetSupportMembers(GetUser()).OrderBy(x => x.User.GetName());
			var myIds = new long[0];
			try {
				myIds = GetUser().User.UserOrganizationIds;
			} catch (Exception e) {
			}
			ViewBag.MySupportId = support.FirstOrDefault(x => myIds.Any(y => y == x.UserOrgId)).NotNull(x => x.Id);
			ViewBag.SupportTeam = support.ToSelectList(x => x.User.GetName(), x => x.Id);

			var campaigns = ApplicationAccessor.GetCampaigns(GetUser(), true).OrderBy(x => x.Name).ToSelectList(x => x.Name, x => x.Name).ToList();
			campaigns.Insert(0, new SelectListItem() { Text = "n/a", Value = null });
			ViewBag.Campaigns = campaigns;
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Create(int? count) {
			var user = GetUserModel();

			PrepareCreateOrgViewBag();

			return View(new OrgCreationData() {
				AssignedTo = ViewBag.MySupportId,
				CoachId = -1
			});
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public async Task<ActionResult> Create(OrgCreationData data) {// String name, bool enableL10, bool enableReview, bool enableAC, string contactFN, string contactLN, string contactEmail,long implementer, ) {
			var user = GetUserModel();
			string planType = "professional";

			if (data.AssignedTo == null)
				ModelState.AddModelError("AssignedTo", "Organization must be assigned to someone in customer support.");
			if (string.IsNullOrWhiteSpace(data.ContactEmail))
				ModelState.AddModelError("ContactEmail", "Please select an email for the primary contact");
			if (string.IsNullOrWhiteSpace(data.ContactFN))
				ModelState.AddModelError("ContactFN", "Please select a first name for the primary contact");
			if (string.IsNullOrWhiteSpace(data.ContactLN))
				ModelState.AddModelError("ContactLN", "Please select a last name for the primary contact");

			if (ModelState.IsValid) {
				var paymentPlanType = PaymentAccessor.GetPlanType(planType);
				var result = await _OrganizationAccessor.CreateOrganization(user, paymentPlanType, DateTime.UtcNow, data);
				var uOrg = result.NewUser;

				if (data.AccountType == AccountType.Implementer || data.AccountType == AccountType.Coach) {
					ApplicationAccessor.EditCoach(GetUser(), new Models.Application.Coach() {
						CoachType = data.AccountType == AccountType.Implementer ? CoachType.CertifiedOrProfessionalEOSi : CoachType.BusinessCoach,
						Name = data.ContactFN + " " + data.ContactLN,
						Email = data.ContactEmail,
						UserOrgId = uOrg.Id,
					});
				}

				return RedirectToAction("SetRole", "Account", new { id = uOrg.Id });
			}

			PrepareCreateOrgViewBag();
			return View(data);
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Products() {
			_PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));
			return View(GetUser().Organization);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Products(OrganizationModel model) {
			if (ModelState.IsValid) {
				await _OrganizationAccessor.UpdateProducts(GetUser(), model.Settings.EnableReview, model.Settings.EnableL10, model.Settings.EnableSurvey, model.Settings.EnablePeople, model.Settings.EnableCoreProcess, model.Settings.Branding);
				return RedirectToAction("Index", "Manage");
			}
			return View(GetUser().Organization);
		}

		[Access(AccessLevel.Any)]
		public async Task<ActionResult> Join(String id = null) {
			if (String.IsNullOrWhiteSpace(id))
				throw new PermissionsException("Id cannot be empty.");


			var nexus = _NexusAccessor.Get(id);
			if (nexus.DateExecuted != null)
				throw new RedirectException(ExceptionStrings.AlreadyMember);
			var user = GetUserModel();

			new Cache().Invalidate(AlertHub.REGISTERED_KEY + user.UserName);

			var orgId = int.Parse(nexus.GetArgs()[0]);
			var placeholderUserId = long.Parse(nexus.GetArgs()[2]);
			if (user == null)
				return RedirectToAction("Login", "Account", new { returnUrl = "Organization/Join/" + id });
			try {
				var userOrg = GetUser(placeholderUserId);
				if (!user.IsRadialAdmin) {
					throw new RedirectException(ExceptionStrings.AlreadyMember);
				} else {
					throw new OrganizationIdException();
				}
			} catch (OrganizationIdException) {
				//We want to hit this exception.
				new Cache().Invalidate(CacheKeys.ORGANIZATION_ID);
				//Session["OrganizationId"] = null;
				var org = await OrganizationAccessor.JoinOrganization(user, nexus.ByUserId, placeholderUserId);
				_NexusAccessor.Execute(nexus);
				return RedirectToAction("Index", "Home", new { message = String.Format(MessageStrings.SuccessfullyJoinedOrganization, org.Organization.Name) });
			}
		}


		/*
		[Access(AccessLevel.UserOrganization)]
		public ActionResult ManageList()
		{
		}*/

		/*
		public ActionResult Manage(int? organizationId)
		{
			if (organizationId == null)
			{
				var userOrgs = GetUserOrganizations();
				var managing = userOrgs.Where(x => x.IsManager());
				var count = managing.Count();
				if (count == 0)
					throw new PermissionsException();
				else if (count == 1)
					return RedirectToAction("Manage", new { organizationId = managing.First().Organization.Id });
				else
					return View("ManageList", managing.Select(x => x.Organization).ToList());
			}
			else
			{
				var userOrg = GetOneUserOrganization(organizationId.Value)
					.Hydrate()
					.ManagingGroups(questions:true)
					.ManagingUsers(subordinates:true)
					.Organization(questions:true)
					.Reviews(answers:true)
					.Nexus()
					.Execute();

				if (userOrg == null)
					throw new PermissionsException();

				if (!userOrg.IsManager())
					throw new PermissionsException();
                
				return View(new ManageViewModel(userOrg));
			}
		}*/

		[Access(AccessLevel.Any)]
		public ActionResult Begin(int? count = null) {
			ViewBag.Count = count;
			int[] roundUp = new int[] { 10, 15, 25, 50, 100, 500 };
			double[] prices = new double[] { 0, 199, 499, 999, 1999, 3999, Double.MaxValue };

			if (count != null) {
				ViewBag.Price = prices[0];
				for (int i = 0; i < roundUp.Length; i++) {
					if (count > roundUp[i]) {
						ViewBag.Price = prices[i + 1];
					}
				}
			}
			return View();
		}

		[Access(AccessLevel.Any)]
		public ActionResult Redirect(int organizationId, string returnUrl) {
			if (returnUrl.Contains("?"))
				return RedirectToLocal(returnUrl + "&setUserOrganizationId=" + organizationId);
			else
				return RedirectToLocal(returnUrl + "?setUserOrganizationId=" + organizationId);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Tree(string type = "cartesian") {
			if (type.ToLower() == "radial") {
				return View("RadialTree", GetUser().Organization.Id);
			} else if (type.ToLower() == "forcedirected") {
				return View("ForceDirected", GetUser().Organization.Id);
			} else {
				return View("Tree", GetUser().Organization.Id);
			}
		}

		[Access(AccessLevel.Manager)]
		public ActionResult ResendJoin(long id) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);

			if (found.TempUser == null)
				throw new PermissionsException("User is already a part of the organization");

			if (found.IsPlaceholder) {
				if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id)))
					return Content("You're not permitted to upgrade placeholders to paid accounts.");
			}

			return PartialView(found.TempUser);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult ResendJoinEmailManual(long id) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);

			if (found.TempUser == null)
				throw new PermissionsException("User is already a part of the organization");

			if (found.IsPlaceholder) {
				if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id)))
					return Content("You're not permitted to upgrade placeholders to paid accounts.");
			}

			//var id = found.TempUser.Guid;
			var url = "Account/Register?returnUrl=%2FOrganization%2FJoin%2F" + found.TempUser.Guid;
			url = Config.BaseUrl(GetUser().Organization) + url;
			var productName = Config.ProductName(GetUser().Organization);
			found.TempUser.EmailTemplate = String.Format(EmailStrings.JoinOrganizationUnderManager_Body, new String[] { found.TempUser.FirstName, GetUser().Organization.Name.Translate(), url, url, productName, found.TempUser.Guid.ToUpper() });


			return PartialView(found.TempUser);
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<JsonResult> ResendJoin(long id, TempUserModel model, long TempId, bool resendEmail) {
			var found = UserAccessor.GetUserOrganization(GetUser(), id, true, false, PermissionType.EditEmployeeDetails);
			if (found.TempUser == null)
				throw new PermissionsException("User is already a part of the organization");


			if (found.IsPlaceholder) {
				if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.CanUpgradeUsersAtOrganization(found.Organization.Id)))
					return Json(ResultObject.CreateError("You're not permitted to upgrade placeholders to paid accounts."));
			}

			if (found.IsPlaceholder) {
				await UserAccessor.RemoveRole(GetUser(), found.Id, UserRoleType.PlaceholderOnly);
			}


			_UserAccessor.UpdateTempUser(GetUser(), id, model.FirstName, model.LastName, model.Email, DateTime.UtcNow);
			model.Id = TempId;
			if (resendEmail) {
				var result = await Emailer.SendEmail(JoinOrganizationAccessor.CreateJoinEmailToGuid(GetUser(), model));
				var prefix = "Resent";
				if (model.LastSent == null)
					prefix = "Sent";
				var o = result.ToResults(prefix + " invite to " + model.Name() + ".");
				o.Object = model;

				return Json(o);
			} else {
				return Json(ResultObject.SilentSuccess(model));
			}
		}


		#region Uploads
		public class UploadVM {
			public long OrganizationId { get; set; }
			public HttpPostedFileBase File { get; set; }
			public Guid Guid { get; set; }

		}

		[Access(AccessLevel.Manager)]
		[HttpGet]
		public ActionResult Upload() {
			var orgId = GetUser().Organization.Id;

			return View(new UploadVM() {
				OrganizationId = orgId,
			});
		}

		public static Dictionary<Guid, String> CSVs = new Dictionary<Guid, String>();

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public ActionResult Upload(UploadVM model) {
			var file = model.File;

			if (file != null && file.ContentLength > 0) {
				var guid = Guid.NewGuid();
				CSVs[guid] = file.InputStream.ReadToEnd();
				return RedirectToAction("Fields", new { id = guid.ToString() });
			}
			ViewBag.Message = "An error has occurred.";
			return RedirectToAction("Upload");
		}

		public class FieldsVM {
			public Guid Guid { get; set; }
			public string[] Fields { get; set; }
			public String FirstNameColumn { get; set; }
			public String LastNameColumn { get; set; }
			public String EmailColumn { get; set; }
			public String PositionColumn { get; set; }
			public String ManagerFirstNameColumn { get; set; }
			public String ManagerLastNameColumn { get; set; }

		}

		[Access(AccessLevel.Manager)]
		public ActionResult Fields(string id = null) {
			try {
				var guid = Guid.Parse(id);

				var data = CSVs[guid];
				using (var sr = new StreamReader(data.ToStream())) {
					var csv = new CsvReader(sr);
					while (csv.Read()) { }

					var headers = csv.FieldHeaders.Select((x, i) => {
						if (String.IsNullOrWhiteSpace(x))
							return "(Column " + i.ToLetter() + ")";
						return x;
					}).ToList();

					var model = new FieldsVM() {
						Guid = guid,
						Fields = headers.ToArray(),
					};

					return View(model);
				}

			} catch (Exception) {
				ViewBag.Message = "An error has occurred.";
				return RedirectToAction("Upload");
			}
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public async Task<ActionResult> Fields(FieldsVM model) {
			var nexus = new NexusController();

#pragma warning disable CS0618 // Type or member is obsolete
			var existingPositions = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);
#pragma warning restore CS0618 // Type or member is obsolete
			var existingUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
			var pos = _PositionAccessor.AllPositions().First();

			var data = CSVs[model.Guid];

			var managerLookup = new Dictionary<long, string[]>();

			var errors = new CounterSet<String>();

			using (var sr = new StreamReader(data.ToStream())) {
				var csv = new CsvReader(sr);
				while (csv.Read()) {
					var email = csv.GetField(model.EmailColumn).Trim();
					var firstName = csv.GetField(model.FirstNameColumn).Trim();
					var lastName = csv.GetField(model.LastNameColumn).Trim();
					var position = csv.GetField(model.PositionColumn).Trim();
					var managerFirst = csv.GetField(model.ManagerFirstNameColumn).Trim();
					var managerLast = csv.GetField(model.ManagerLastNameColumn).Trim();

					if ((new[] { email, firstName, lastName, position, managerFirst, managerLast }.All(string.IsNullOrWhiteSpace))) {
						//Empty row
						continue;
					}

					var positionFound = existingPositions.FirstOrDefault(x => x.CustomName == position);

					if (positionFound == null && !String.IsNullOrWhiteSpace(position)) {
						var newPosition =await _OrganizationAccessor.EditOrganizationPosition(GetUser(), 0, GetUser().Organization.Id, /*pos.Id,*/ position);
						existingPositions.Add(newPosition);
						positionFound = newPosition;
					}

					var vm = new CreateUserOrganizationViewModel() {
						Email = email,
						FirstName = firstName,
						LastName = lastName,
						OrgId = GetUser().Organization.Id,
						ManagerNodeId = null,
						Position = new UserPositionViewModel() {
							CustomPosition = null,
							PositionId = positionFound != null ? positionFound.Id : -2
						},
					};
					try {
						var user = (await _UserAccessor.CreateUser(GetUser(), vm)).Item2;

						existingUsers.Add(user);
						managerLookup.Add(user.Id, new[] { managerFirst, managerLast });
					} catch (PermissionsException e) {
						errors.Add(e.Message);
					} catch (Exception) {
						errors.Add("An error has occurred.");
					}
				}
			}


			AccountabilityAccessor._FinishUploadAccountabilityChart(GetUser(), existingUsers, managerLookup, errors);

			ViewBag.Message = String.Join("\n", errors.Select(x => x.Key));
			return RedirectToAction("Members", "Manage");

		}
		#endregion





	}
}