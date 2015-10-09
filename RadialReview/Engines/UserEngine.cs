using System.Web.Security;
using RadialReview.Accessors;
using RadialReview.Models;
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

					foundUser.SetPersonallyManaging(DeepSubordianteAccessor.ManagesUser(s,perms, caller.Id, id));

					var responsibilities = new List<String>();

					var r = _ResponsibilitiesAccessor.GetResponsibilityGroup(s, perms, id);
					var teams = _TeamAccessor.GetUsersTeams(caller, id);
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
					var rocks = RockAccessor.GetAllRocks(s, perms, id);
					var measurables = ScorecardAccessor.GetUserMeasurables(s, perms, id, true);
					//foundUser.PopulatePersonallyManaging(caller, caller.AllSubordinates);

					var model = new UserOrganizationDetails()
					{
						User=foundUser,
						Responsibilities = responsibilities,
						Roles = roles,
						Rocks = rocks,
						Measurables = measurables,
						ManagingOrganization = caller.ManagingOrganization,
						/*Editable =	caller.ManagingOrganization || foundUser.GetPersonallyManaging() || 
									(foundUser.Organization.Settings.ManagersCanEditSelf && foundUser.ManagerAtOrganization) ||
									(foundUser.Organization.Settings.EmployeesCanEditSelf)*/
					};

					return model;
				}
			}
         
        }

    }
}