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

namespace RadialReview.Controllers
{
	public class ManageController : BaseController
	{
		//
		// GET: /Manage/
		[Access(AccessLevel.Manager)]
		public ActionResult Index()
		{
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

		public class ManageUserModel
		{
			public UserOrganizationDetails Details { get; set; }
		}

		[Access(AccessLevel.Manager)]
		public ActionResult UserDetails(long id)
		{
			if (id == 0)
				id = GetUser().Id;

			//var caller = GetUser().Hydrate().ManagingUsers(true).Execute();
			var details = _UserEngine.GetUserDetails(GetUser(), id);
			//DeepSubordianteAccessor.ManagesUser(GetUser(), GetUser().Id, id);
			//details.User.PopulatePersonallyManaging(GetUser(), caller.AllSubordinates);

			details.ForceEditable = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditQuestionForUser(id));

			var model = new ManageUserModel(){
				Details = details
			};

			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Positions()
		{
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Positions", LifeTime.Session);
			var orgPos = _OrganizationAccessor.GetOrganizationPositions(GetUser(), GetUser().Organization.Id);

			var positions = orgPos.Select(x =>
				new OrgPosViewModel(x, _PositionAccessor.GetUsersWithPosition(GetUser(), x.Id).Count())
			).ToList();

			var caller = GetUser().Hydrate().EditPositions().Execute();

			var model = new OrgPositionsViewModel() { Positions = positions, CanEdit = caller.GetEditPosition() };
			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Teams()
		{

			new Cache().Push(CacheKeys.MANAGE_PAGE, "Teams", LifeTime.Session);
			var orgTeams = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id);
			var teams = orgTeams.Select(x => new OrganizationTeamViewModel { Team = x, Members = 0,TemplateId = x.TemplateId}).ToList();

			for (int i = 0; i < orgTeams.Count(); i++)
			{
				try
				{
					teams[i].Team = teams[i].Team.HydrateResponsibilityGroup().PersonallyManaging(GetUser()).Execute();
					teams[i].Members = _TeamAccessor.GetTeamMembers(GetUser(), teams[i].Team.Id, false).ToListAlive().Count();
				}
				catch (Exception e)
				{
					log.Error(e);
				}

			}
			var model = new OrganizationTeamsViewModel() { Teams = teams };
			return View(model);
		}

		[Access(AccessLevel.Manager)]
		[OutputCache(NoStore = true,Duration = 0)]
		public ActionResult Members()
		{
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Members", LifeTime.Session);
			//var user = GetUser().Hydrate().ManagingUsers(true).Execute();

			var members = _OrganizationAccessor.GetOrganizationMembersLookup(GetUser(), GetUser().Organization.Id, true, PermissionType.EditEmployeeDetails);
			//var members = 

			for (int i = 0; i < members.Count(); i++)
			{
				var u = members[i];
				//u.Teams = u.Teams.OrderBy(x => x.Team.Type).ToList();
				//var teams = _TeamAccessor.GetUsersTeams(GetUser(), u.Id);
				//members[i] = members[i].Hydrate().SetTeams(teams).PersonallyManaging(GetUser()).Managers().Execute();
			}
			var model = new OrgMembersViewModel(GetUser(), members, GetUser().Organization);
			return View(model);
		}

		public class ReorganizeVM
		{
			public List<UserOrganizationModel> AllUsers { get; set; }
			public List<ManagerDuration> AllManagerLinks { get; set; }


		}

		[Access(AccessLevel.Manager)]
		public ActionResult Reorganize()
		{
			new Cache().Push(CacheKeys.MANAGE_PAGE, "Reorganize", LifeTime.Session);
			var orgId = GetUser().Organization.Id;
			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), orgId, false, false);
			var allManagers = _OrganizationAccessor.GetOrganizationManagerLinks(GetUser(), orgId).ToListAlive();

			var depth = _DeepSubordianteAccessor.GetOrganizationMap(GetUser(), orgId);

			allUsers.ForEach(x => x.SetLevel(depth.Count(y => y.SubordinateId == x.Id)));

			var model = new ReorganizeVM()
			{
				AllManagerLinks = allManagers,
				AllUsers = allUsers,
			};

			return View(model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Organization()
		{
			var user = GetUser().Hydrate().Organization().Execute();
			_PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));

			var companyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)
				//.Select(x => x.CompanyValue)
				.ToList();
			var companyRocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			var companyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			var model = new OrganizationViewModel(){
				Id = user.Organization.Id,
				CompanyValues = companyValues,
				CompanyRocks = companyRocks,
				CompanyQuestions = companyQuestions,
			};
			return View(model);
		}


		[Access(AccessLevel.Manager)]
		public ActionResult Advanced()
		{
			var user = GetUser().Hydrate().Organization().Execute();

			_PermissionsAccessor.Permitted(GetUser(), x => x.ManagingOrganization(GetUser().Organization.Id));

		
			var model = new OrganizationViewModel()
			{
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
                Cards = _PaymentAccessor.GetCards(GetUser(),GetUser().Organization.Id),

				OnlySeeRockAndScorecardBelowYou = user.Organization.Settings.OnlySeeRocksAndScorecardBelowYou,

				PaymentPlan = _PaymentAccessor.GetPlan(GetUser(),GetUser().Organization.Id)

			};

			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult Organization(OrganizationViewModel model)
		{
			model.CompanyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)//.Select(x => x.CompanyValue)
				.ToList();
			model.CompanyRocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			model.CompanyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public ActionResult Advanced(OrganizationViewModel model)
		{
			_OrganizationAccessor.Edit(
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
				model.WeekStart);
			ViewBag.Success = "Successfully Saved.";

			model.CompanyValues = _OrganizationAccessor.GetCompanyValues(GetUser(), GetUser().Organization.Id)
				//.Select(x => x.CompanyValue)
				.ToList();

			model.CompanyRocks = _OrganizationAccessor.GetCompanyRocks(GetUser(), GetUser().Organization.Id).ToList();
			model.Cards = _PaymentAccessor.GetCards(GetUser(), GetUser().Organization.Id);

			model.CompanyQuestions = OrganizationAccessor.GetQuestionsAboutCompany(GetUser(), GetUser().Organization.Id, null).ToList();

			return View(model);
		}
	}
}