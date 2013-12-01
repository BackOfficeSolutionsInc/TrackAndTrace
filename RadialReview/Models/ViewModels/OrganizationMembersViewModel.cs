﻿using System;
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
        public int NumIndividualResponsibilities { get; set; }
        public int NumTotalResponsibilities { get; set; }
        public int NumTeams { get; set; }
        public int NumPositions { get; set; }
        public String PositionTitle { get; set; }

        public OrgMemberViewModel(UserOrganizationModel userOrg)
        {
            Id = userOrg.Id;
            Name = userOrg.GetName();
            Email = userOrg.EmailAtOrganization;
            Verified = userOrg.User != null;
            NumTeams = userOrg.Teams.ToListAlive().Count();
            NumPositions = userOrg.Positions.ToListAlive().Count();
            NumIndividualResponsibilities = userOrg.Responsibilities.ToListAlive().Count();
            PositionTitle = userOrg.Positions.ToListAlive().FirstOrDefault().NotNull(x => x.Position.CustomName);

            NumTotalResponsibilities =
                userOrg.Responsibilities.ToListAlive().Count() +
                userOrg.Positions.ToListAlive().Sum(x => x.Position.Responsibilities.Count()) +
                userOrg.Teams.ToListAlive().Sum(x => x.Team.Responsibilities.Count());

        }
    }
}