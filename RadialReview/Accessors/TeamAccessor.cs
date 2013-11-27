using RadialReview.Models;
using RadialReview.Models.AccountabilityGroupModels;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class TeamAccessor : BaseAccessor
    {
        public TeamModel GetTeam(UserOrganizationModel caller,long teamId)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var team = s.Get<TeamModel>(teamId);
                    PermissionsUtility.Create(s, caller).ViewTeam(team);
                    team.Members = s.QueryOver<TeamMemberModel>().Where(x => x.TeamId == teamId).List().ToList();
                    return team;
                }
            }
        }

        public TeamModel EditTeam(UserOrganizationModel caller, long teamId,String name=null)
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var team = s.Get<TeamModel>(teamId);
                    PermissionsUtility.Create(s, caller).EditTeam(team);
                    if (name!=null)
                    {
                        team.Name = name;
                    }
                    s.Update(team);

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
                    var team = s.Get<TeamModel>(teamId);
                    PermissionsUtility.Create(s, user).EditTeam(team).EditUserOrganization(userOrgId);
                    s.Save(new TeamMemberModel() { TeamId = teamId, UserOrganization = userOrgId });
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
                    var team = s.Get<TeamModel>(teamId);
                    PermissionsUtility.Create(s, caller).EditTeam(team).EditUserOrganization(userOrgId);

                    var member=s.QueryOver<TeamMemberModel>().Where(x => x.UserOrganization == userOrgId && x.TeamId == teamId).SingleOrDefault();
                    member.DeleteTime = DateTime.UtcNow;
                    s.Update(member);
                    tx.Commit();
                    s.Flush();
                    return true;
                }
            }
        }
    }
}