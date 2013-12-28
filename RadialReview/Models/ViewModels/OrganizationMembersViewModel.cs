using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class OrgMembersViewModel
    {
        public List<OrgMemberViewModel> Users { get; set; }

        public OrgMembersViewModel(IEnumerable<UserOrganizationModel> members)
        {
            Users = members.Select(x => new OrgMemberViewModel(x)).ToList();
        }
    }

    public class OrgMemberViewModel
    {
        public long Id { get;set; }
        public String Name { get; set; }
        public String Email { get; set; }
        public bool Verified { get; set; }
        public bool Manager { get; set; }
        public bool EmailSent { get; set; }
        public int NumIndividualResponsibilities { get; set; }
        public int NumTotalResponsibilities { get; set; }
        //public int NumTeams { get; set; }
        //public int NumPositions { get; set; }
        public Boolean Managing { get; set; }

        public List<String> PositionTitles { get; set; }
        public List<String> TeamsTitles { get; set; }
        public List<string> ManagersTitles { get; set; }

        public OrgMemberViewModel(UserOrganizationModel userOrg)
        {
            Id = userOrg.Id;
            Name = userOrg.GetName();
            Email = userOrg.EmailAtOrganization;
            Manager = userOrg.IsManager();
            Verified = userOrg.User != null;
            TeamsTitles = userOrg.Teams.ToListAlive().Select(x => x.Team.Name).ToList();
            PositionTitles = userOrg.Positions.ToListAlive().Select(x=>x.Position.CustomName).ToList();
            ManagersTitles = userOrg.ManagedBy.ToListAlive().Select(x => x.Manager.GetName()).ToList();
            NumIndividualResponsibilities = userOrg.Responsibilities.ToListAlive().Count();
            EmailSent=true;
            if (userOrg.TempUser != null && userOrg.TempUser.LastSent == null)
                EmailSent = false;

            Managing = userOrg.GetPersonallyManaging();

            //PositionTitle = userOrg.Positions.ToListAlive().FirstOrDefault().NotNull(x => x.Position.CustomName);

            NumTotalResponsibilities =
                userOrg.Responsibilities.ToListAlive().Count() +
                userOrg.Positions.ToListAlive().Sum(x => x.Position.Responsibilities.Count()) +
                userOrg.Teams.ToListAlive().Sum(x => x.Team.Responsibilities.Count());

        }

    }
}