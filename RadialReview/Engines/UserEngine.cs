using System.Web.Security;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Scorecard;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Utilities;

namespace RadialReview.Engines
{
    public class UserEngine
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
		private static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
		private static PositionAccessor _PositionAccessor = new PositionAccessor();
		private static RockAccessor _RockAccessor = new RockAccessor();
		private static RoleAccessor _RoleAccessor = new RoleAccessor();
        private static TeamAccessor _TeamAccessor = new TeamAccessor();
        private static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
        protected static UserAccessor _UserAccessor = new UserAccessor();

        public UserOrganizationDetails GetUserDetails(UserOrganizationModel caller,long id)
        {
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var perms = PermissionsUtility.Create(s, caller);
					var foundUser = UserAccessor.GetUserOrganization(s,perms, id, false, false);

					foundUser.SetPersonallyManaging(DeepAccessor.Users.ManagesUser(s,perms, caller.Id, id));

					var responsibilities = new List<String>();

					var r = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, id);
					var teams = TeamAccessor.GetUsersTeams(s.ToQueryProvider(true),perms, id);
					var userResponsibility = ((UserOrganizationModel)r).Hydrate(s).Position().SetTeams(teams).Execute();

					responsibilities.AddRange(userResponsibility.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
					foreach (var rgId in userResponsibility.Positions.ToListAlive().Select(x => x.Position.Id))
					{
						var positionResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, rgId);
						responsibilities.AddRange(positionResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
					}
					foreach (var teamId in userResponsibility.Teams.ToListAlive().Select(x => x.Team.Id))
					{
						var teamResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, teamId);
						responsibilities.AddRange(teamResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
					}

					var roles = RoleAccessor.GetRoles(s, perms, id);
					var model = new UserOrganizationDetails()
					{
						SelfId = caller.Id,
						User=foundUser,
						Responsibilities = responsibilities,
						Roles = roles,
						ManagingOrganization = caller.ManagingOrganization,
						/*Editable =	caller.ManagingOrganization || foundUser.GetPersonallyManaging() || 
									(foundUser.Organization.Settings.ManagersCanEditSelf && foundUser.ManagerAtOrganization) ||
									(foundUser.Organization.Settings.EmployeesCanEditSelf)*/
					};
					
					if (perms.IsPermitted(x => x.CanViewUserRocks(id))){
						model.Rocks = RockAccessor.GetAllRocks(s, perms, id);
						model.CanViewRocks = true;
					}

					if (perms.IsPermitted(x => x.CanViewUserMeasurables(id))){
						model.Measurables = ScorecardAccessor.GetUserMeasurables(s, perms, id, true, false, false);
						model.CanViewMeasurables = true;
					}
					//foundUser.PopulatePersonallyManaging(caller, caller.AllSubordinates);

				

					return model;
				}
			}
         
        }

    }
}