﻿using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class TeamAccessor : BaseAccessor
    {
        /*
        private OrganizationTeamModel Populate(ISession session, OrganizationTeamModel team)
        {
            switch (team.Type)
            {
                case TeamType.Standard:{
                        return team;
                    }
                case TeamType.AllMembers:{
                        team.Name = team.Organization.GetName(); return team;
                    }
                case TeamType.Managers:{
                        team.Name = "Managers at "+team.Organization.GetName(); return team;
                    }
                default: throw new NotImplementedException("Team Type Unknown");
            }
        }*/


        public List<OrganizationTeamModel> GetOrganizationTeams(UserOrganizationModel caller, long organizationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewOrganization(organizationId);
                    var teams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == organizationId).List().ToList();
                    //teams.ForEach(x => Populate(s, x));
                    return teams;
                }
            }
        }

        public OrganizationTeamModel GetTeam(UserOrganizationModel caller, long teamId)
        {
            if (teamId == 0)
                return new OrganizationTeamModel() { };
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewTeam(teamId);
                    var team = s.Get<OrganizationTeamModel>(teamId);
                    //team.Members = s.QueryOver<TeamMemberModel>().Where(x => x.TeamId == teamId).List().ToList();
                    return team;
                }
            }
        }

        public List<TeamDurationModel> GetTeamMembers(UserOrganizationModel caller, long teamId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).ViewTeam(teamId);
                    var team = s.Get<OrganizationTeamModel>(teamId);

                    switch (team.Type)
                    {
                        case TeamType.Standard:
                            {
                                var teamMembers = s.QueryOver<TeamDurationModel>().Where(x => x.Team.Id == teamId).List().ToList();
                                return teamMembers;
                            }
                        case TeamType.AllMembers:
                            {
                                var users = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == team.Organization.Id).List().ToList();
                                return users.Select(x => new TeamDurationModel() { Id = -2, Start = x.AttachTime, Team = team, User = x, DeleteTime = x.DeleteTime ?? x.DetachTime }).ToList();
                            }
                        case TeamType.Managers:
                            {
                                var managers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == team.Organization.Id && (x.ManagerAtOrganization || x.ManagingOrganization)).List().ToList();
                                return managers.Select(x => new TeamDurationModel() { Id = -2, Start = x.AttachTime, Team = team, User = x, DeleteTime = x.DeleteTime ?? x.DetachTime }).ToList();
                            }
                        default: throw new NotImplementedException("Team Type unknown");
                    }
                }
            }
        }

        public List<TeamDurationModel> GetUsersTeams(UserOrganizationModel caller, long forUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUser = s.Get<UserOrganizationModel>(forUserId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(forUser.Organization.Id);
                    var teams = forUser.Teams.ToList();

                    if (forUser.IsManager()){
                        var managerTeam=s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.Managers).SingleOrDefault();
                        //Populate(s,managerTeam);
                        teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = managerTeam, User = forUser });
                    }
                    var allMembersTeam = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == forUser.Organization.Id && x.Type == TeamType.AllMembers).SingleOrDefault();
                    //Populate(s,allMembersTeam);
                    teams.Add(new TeamDurationModel() { Start = forUser.AttachTime, Id = -2, Team = allMembersTeam, User = forUser });

                    //teams.ForEach(x => Populate(s, x.Team));

                    return teams;
                }
            }
        }

        public OrganizationTeamModel EditTeam(UserOrganizationModel caller, long teamId, String name = null,
                                                                                        bool? onlyManagerCanEdit = null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditTeam(teamId);
                    caller = s.Get<UserOrganizationModel>(caller.Id);

                    var team = s.Get<OrganizationTeamModel>(teamId);

                    if (teamId == 0)
                    {
                        if (name == null || onlyManagerCanEdit == null)
                            throw new PermissionsException();

                        team = new OrganizationTeamModel()
                        {
                            CreatedBy = caller.Id,
                            Organization = caller.Organization,
                            OnlyManagersEdit = onlyManagerCanEdit.Value
                        };
                    }

                    if (name != null)
                    {
                        team.Name = name;
                    }

                    if (onlyManagerCanEdit != null && onlyManagerCanEdit.Value != team.OnlyManagersEdit)
                    {
                        if (!caller.IsManager())
                            throw new PermissionsException();
                        team.OnlyManagersEdit = onlyManagerCanEdit.Value;
                    }


                    s.SaveOrUpdate(team);

                    tx.Commit();
                    s.Flush();
                    return team;
                }
            }
        }

        public Boolean AddMember(UserOrganizationModel caller, long teamId, long userOrgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditTeam(teamId).EditUserOrganization(userOrgId);
                    var team = s.Get<OrganizationTeamModel>(teamId);

                    if (team.Type != TeamType.Standard)
                        throw new PermissionsException("You cannot add members to an auto-generated team.");


                    var uOrg = s.Get<UserOrganizationModel>(userOrgId);

                    var existing = s.QueryOver<TeamDurationModel>().Where(x => x.User.Id == userOrgId && x.Team.Id == teamId && x.DeleteTime == null).SingleOrDefault();
                    if (existing != null)
                        throw new PermissionsException("The user is already a member of this team.");

                    //team.Members.Add(new TeamMemberModel() { UserOrganization = uOrg });

                    var teamDuration = new TeamDurationModel(uOrg, team, caller.Id);

                    uOrg.Teams.Add(teamDuration);
                    s.Update(uOrg);

                    tx.Commit();
                    s.Flush();
                    return true;
                }
            }
        }

        public Boolean RemoveMember(UserOrganizationModel caller, long teamDurationId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var teamDuration = s.Get<TeamDurationModel>(teamDurationId);
                    if (teamDuration.Team.Type != TeamType.Standard)
                        throw new PermissionsException("You cannot remove members from an auto-generated team.");

                    if (teamDuration.DeleteTime != null)
                        throw new PermissionsException();

                    PermissionsUtility.Create(s, caller).EditTeam(teamDuration.Team.Id);
                    //var team = s.Get<OrganizationTeamModel>(teamId);
                    //team.Members.FirstOrDefault(x => x.UserOrganization.Id == userOrgId).DeleteTime = DateTime.UtcNow;

                    teamDuration.DeleteTime = DateTime.UtcNow;
                    teamDuration.DeletedBy = caller.Id;

                    s.Update(teamDuration);
                    tx.Commit();
                    s.Flush();
                    return true;
                }
            }
        }
    }
}