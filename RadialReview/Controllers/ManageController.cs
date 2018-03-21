using Microsoft.Ajax.Utilities;
using RadialReview.Accessors;
using RadialReview.Engines;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Permissions;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities;

namespace RadialReview.Controllers {
	public class ManageController : BaseController {

		private void UpdateViewbag() {
			ViewBag.ManagingPayments = PermissionsAccessor.CanEdit(GetUser(), PermItem.ResourceType.UpdatePaymentForOrganization, GetUser().Organization.Id);
			ViewBag.ShowAddClient = GetUser().Organization.Settings.AllowAddClient;
		}


		//
		// GET: /Manage/
		[Access(AccessLevel.Manager)]
		public ActionResult Index() {
			//Main page
			var page = "Members";//(string)new Cache().Get(CacheKeys.MANAGE_PAGE) ?? "Members";
			return RedirectToAction(page);
			/*
			switch (page)
			{
				case "Positions":  Positions();
				case "Teams": return Teams();
				case "Members": return Members();
				case "Reviews": return Reviews();
				default: return Positions();
			}*/
		}

		public class ManageUserModel {
			public UserOrganizationDetails Details { get; set; }
		}

		[Access(AccessLevel.Manager)]
		public ActionResult UserDetails(long id) {
			if (id == 0)
				id = GetUser().Id;

			//var caller = GetUser().Hydrate().ManagingUsers(true).Execute();
			var details = _UserEngine.GetUserDetails(GetUser(), id);
			//DeepSubordianteAccessor.ManagesUser(GetUser(), GetUser().Id, id);
			//details.User.PopulatePersonallyManaging(GetUser(), caller.AllSubordinates);

			details.ForceEditable = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditQuestionForUser(id));

			var model = new ManageUserModel() {
				Details = details
			};

			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Positions() {
			UpdateViewbag();
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Positions", LifeTime.Request/*Session*/);
#pragma warning disable CS0618 // Type or member is obsolete
			var orgPos = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id).ToListAlive();
#pragma warning restore CS0618 // Type or member is obsolete

			var positions = orgPos.Select(x =>
				new OrgPosViewModel(x, _PositionAccessor.GetUsersWithPosition(GetUser(), x.Id).Count())
			).ToList();

			var caller = GetUser().Hydrate().EditPositions().Execute();

			var model = new OrgPositionsViewModel() { Positions = positions, CanEdit = caller.GetEditPosition() };
			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Teams() {
			UpdateViewbag();
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Teams", LifeTime.Request/*Session*/);
			var orgTeams = TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id);
			var teams = orgTeams.Select(x => new OrganizationTeamViewModel { Team = x, Members = 0, TemplateId = x.TemplateId }).ToList();

			for (int i = 0; i < orgTeams.Count(); i++) {
				try {
					teams[i].Team = teams[i].Team.HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
					teams[i].Members = TeamAccessor.GetTeamMembers(GetUser(), teams[i].Team.Id, false).ToListAlive().Count();
				} catch (Exception e) {
					log.Error(e);
				}

			}
			var model = new OrganizationTeamsViewModel() { Teams = teams };
			return View(model);
		}
		public class DataVM {
			public List<SelectListItem> Periods { get; set; }
		}
		[Access(AccessLevel.Manager)]
		public ActionResult Data() {
			UpdateViewbag();
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Data", LifeTime.Request/*Session*/);
			var model = new DataVM() {
				Periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToSelectList(y => y.Name, y => y.Id)
			};

			return View(model);
		}

		[Access(AccessLevel.Manager)]
		[OutputCache(NoStore = true, Duration = 0)]
		public ActionResult Members() {
			UpdateViewbag();
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Members", LifeTime.Request/*Session*/);
			//var user = GetUser().Hydrate().ManagingUsers(true).Execute();

			var members = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails);

			var hasAdminDelete = _PermissionsAccessor.AnyTrue(GetUser(), PermissionType.DeleteEmployees, x => x.ManagingOrganization);

			var messages = MessageAccessor.GetManageMembers_Messages(GetUser(), GetUser().Organization.Id);

			var canUpgrade = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanEdit(PermItem.ResourceType.UpgradeUsersForOrganization, GetUser().Organization.Id));

			for (int i = 0; i < members.Count(); i++) {
				var u = members[i];
				//u.Teams = u.Teams.OrderBy(x => x.Team.Type).ToList();
				//var teams = _TeamAccessor.GetUsersTeams(GetUser(), u.Id);
				//members[i] = members[i].Hydrate().SetTeams(teams).PersonallyManaging(GetUser()).Managers().Execute();
			}

			var roles = UserAccessor.GetUserRolesAtOrganization(GetUser(), GetUser().Organization.Id);

			var model = new OrgMembersViewModel(GetUser(), members, GetUser().Organization, hasAdminDelete, canUpgrade, roles);
			model.Messages = messages;
			return View(model);
		}

		public class ReorganizeVM {
			public List<UserOrganizationModel> AllUsers { get; set; }
			public List<ManagerDuration> AllManagerLinks { get; set; }
		}

		[Access(AccessLevel.Manager)]
		[Obsolete("Do not use", true)]
		public ActionResult Reorganize() {
			UpdateViewbag();
			throw new Exception("Do not use");
			//new Cache().Push(CacheKeys.MANAGE_PAGE, "Reorganize", LifeTime.Request/*Session*/);
			//var orgId = GetUser().Organization.Id;
			//var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);
			//var allManagers = _OrganizationAccessor.GetOrganizationManagerLinks(GetUser(), orgId).ToListAlive();

			//var depth = _DeepSubordianteAccessor.GetOrganizationMap(GetUser(), orgId);

			//allUsers.ForEach(x => x.SetLevel(depth.Count(y => y.SubordinateId == x.Id)));

			//var model = new ReorganizeVM()
			//{
			//	AllManagerLinks = allManagers,
			//	AllUsers = allUsers,
			//};

			//return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Organization() {
			UpdateViewbag();
			var user = GetUser().Hydrate().Organization().Execute();
			_PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));

			var companyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)
				//.Select(x => x.CompanyValue)
				.ToList();
			//var companyRocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			var companyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			var model = new OrganizationViewModel() {
				Id = user.Organization.Id,
				CompanyValues = companyValues,
				CompanyRocks = null,//companyRocks,
				CompanyQuestions = companyQuestions,
			};
			return View(model);
		}


		[Access(AccessLevel.Manager)]
		public ActionResult Advanced() {
			UpdateViewbag();
			OrganizationViewModel model = GetAdminDataModel(true, false);
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Payment() {
			UpdateViewbag();
			OrganizationViewModel model = GetAdminDataModel(false, true);
			return View(model);
		}

		private OrganizationViewModel GetAdminDataModel(bool testManageOrg, bool testManageFinance) {
			var user = GetUser().Hydrate().Organization().Execute();

			if (!testManageFinance && !testManageOrg)
				throw new PermissionsException();

			if (testManageOrg)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));
			if (testManageFinance)
				_PermissionsAccessor.Permitted(GetUser(), x => x.EditCompanyPayment(GetUser().Organization.Id));


			var model = new OrganizationViewModel() {
				Id = user.Organization.Id,
				ManagersCanEdit = user.Organization.ManagersCanEdit,
				OrganizationName = user.Organization.Name.Standard,
				StrictHierarchy = user.Organization.StrictHierarchy,
				ManagersCanEditPositions = user.Organization.ManagersCanEditPositions,
				ManagersCanRemoveUsers = user.Organization.ManagersCanRemoveUsers,

				SendEmailImmediately = user.Organization.SendEmailImmediately,
				ManagersCanEditSelf = user.Organization.Settings.ManagersCanEditSelf,
				EmployeesCanEditSelf = user.Organization.Settings.EmployeesCanEditSelf,
				ManagersCanCreateSurvey = user.Organization.Settings.ManagersCanCreateSurvey,
				EmployeesCanCreateSurvey = user.Organization.Settings.EmployeesCanCreateSurvey,

				RockName = user.Organization.Settings.RockName,
				TimeZone = user.Organization.Settings.TimeZoneId,
				WeekStart = user.Organization.Settings.WeekStart,
				Cards = PaymentAccessor.GetCards(GetUser(), GetUser().Organization.Id),

				OnlySeeRockAndScorecardBelowYou = user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou,

				PaymentPlan = PaymentAccessor.GetPlan(GetUser(), GetUser().Organization.Id),

				ScorecardPeriod = user.Organization.Settings.ScorecardPeriod,

				StartOfYearMonth = user.Organization.Settings.StartOfYearMonth,
				StartOfYearOffset = user.Organization.Settings.StartOfYearOffset,

				DateFormat = user.Organization.Settings.DateFormat,
				NumberFormat = user.Organization.Settings.NumberFormat,

				LimitFiveState = user.Organization.Settings.LimitFiveState,

				AllowAddClient = user.Organization.Settings.AllowAddClient,

				DefaultSendTodoTime = user.Organization.Settings.DefaultSendTodoTime,
				PossibleTodoTimes = TimingUtility.GetPossibleTimes(user.Organization.Settings.DefaultSendTodoTime),

				AccountabilityChartId = user.Organization.AccountabilityChartId

			};
			return model;
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult Organization(OrganizationViewModel model) {
			model.CompanyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)//.Select(x => x.CompanyValue)
				.ToList();
			model.CompanyRocks = null;//_OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			model.CompanyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult Advanced(OrganizationViewModel model) {
			OrganizationAccessor.Edit(
				GetUser(),
				model.Id,
				model.OrganizationName,
				model.ManagersCanEdit,
				model.StrictHierarchy,
				model.ManagersCanEditPositions,
				model.SendEmailImmediately,
				model.ManagersCanRemoveUsers,
				model.ManagersCanEditSelf,
				model.EmployeesCanEditSelf,
				model.ManagersCanCreateSurvey,
				model.EmployeesCanCreateSurvey,
				model.RockName,
				model.OnlySeeRockAndScorecardBelowYou,
				model.TimeZone,
				model.WeekStart,
				model.ScorecardPeriod,
				model.StartOfYearMonth,
				model.StartOfYearOffset,
				model.DateFormat,
				model.NumberFormat,
				model.LimitFiveState,
				model.DefaultSendTodoTime,
				model.AllowAddClient);
			ViewBag.Success = "Successfully Saved.";

			//model.CompanyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)
			//	//.Select(x => x.CompanyValue)
			//	.ToList();

			//model.CompanyRocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			//model.Cards = PaymentAccessor.GetCards(GetUser(), GetUser().Organization.Id);

			//model.CompanyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			//         model.AccountabilityChartId = user.Organization.AccountabilityChartId


			return Advanced();
		}
	}
}
