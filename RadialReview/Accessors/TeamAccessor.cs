using RadialReview.Exceptions;
using RadialReview.Models;
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
        public OrganizationTeamModel GetTeam(UserOrganizationModel caller,long teamId)
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

        public List<TeamDurationModel> GetUsersTeams(UserOrganizationModel caller,long forUserId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var forUser=s.Get<UserOrganizationModel>(forUserId);
                    PermissionsUtility.Create(s, caller).ViewOrganization(forUser.Organization.Id);

                    return forUser.Teams.ToList();
                }
            }
        }

        public OrganizationTeamModel EditTeam(UserOrganizationModel caller, long teamId,
                                                                                        String name=null,
                                                                                        bool? onlyManagerCanEdit=null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditTeam(teamId);
                    caller = s.Get<UserOrganizationModel>(caller.Id);

                    var team = s.Get<OrganizationTeamModel>(teamId) ;

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
                        
                    if (name!=null)
                    {
                        team.Name = name;
                    }

                    if (onlyManagerCanEdit != null && onlyManagerCanEdit.Value!=team.OnlyManagersEdit) 
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
                    var uOrg = s.Get<UserOrganizationModel>(userOrgId);

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