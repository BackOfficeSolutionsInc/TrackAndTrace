﻿using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Engines
{
    public class UserEngine
    {
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        private static QuestionAccessor _QuestionAccessor = new QuestionAccessor();
        private static PositionAccessor _PositionAccessor = new PositionAccessor();
        private static TeamAccessor _TeamAccessor = new TeamAccessor();
        private static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();
        protected static UserAccessor _UserAccessor = new UserAccessor();

        public UserOrganizationDetails GetUserDetails(UserOrganizationModel caller,long id)
        {
             var foundUser = _UserAccessor.GetUserOrganization(caller, id, false, false);
            var responsibilities = new List<String>();

            var r = _ResponsibilitiesAccessor.GetResponsibilityGroup(caller, id);
            var teams = _TeamAccessor.GetUsersTeams(caller, id);
            var userResponsibility = ((UserOrganizationModel)r).Hydrate().Position().SetTeams(teams).Execute();

            responsibilities.AddRange(userResponsibility.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
            foreach (var rgId in userResponsibility.Positions.ToListAlive().Select(x => x.Position.Id))
            {
                var positionResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(caller, rgId);
                responsibilities.AddRange(positionResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
            }
            foreach (var teamId in userResponsibility.Teams.ToListAlive().Select(x => x.Team.Id))
            {
                var teamResp = _ResponsibilitiesAccessor.GetResponsibilityGroup(caller, teamId);
                responsibilities.AddRange(teamResp.Responsibilities.ToListAlive().Select(x => x.GetQuestion()));
            }


            var model = new UserOrganizationDetails()
            {
                User=foundUser,
                Responsibilities = responsibilities,
            };

            return model;
        }

    }
}