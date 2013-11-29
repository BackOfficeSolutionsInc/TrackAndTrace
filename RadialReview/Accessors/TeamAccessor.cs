using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Responsibilities;
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

        public Boolean AddMember(UserOrganizationModel user, long teamId, long userOrgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, user).EditTeam(teamId).EditUserOrganization(userOrgId);
                    var team = s.Get<OrganizationTeamModel>(teamId);
                    var uOrg = s.Get<UserOrganizationModel>(userOrgId);

                    team.Members.Add(new TeamMemberModel() { UserOrganization = uOrg });

                    s.Update(team);
                    tx.Commit();
                    s.Flush();
                    return true;
                }
            }
        }

        public Boolean RemoveMember(UserOrganizationModel caller, long teamId,long userOrgId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    PermissionsUtility.Create(s, caller).EditTeam(teamId).EditUserOrganization(userOrgId);
                    var team = s.Get<OrganizationTeamModel>(teamId);

                    team.Members.FirstOrDefault(x => x.UserOrganization.Id == userOrgId).DeleteTime = DateTime.UtcNow;

                    s.Update(team);
                    tx.Commit();
                    s.Flush();
                    return true;
                }
            }
        }
    }
}