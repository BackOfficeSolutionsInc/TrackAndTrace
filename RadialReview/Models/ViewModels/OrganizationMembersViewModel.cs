using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Conventions.AcceptanceCriteria;
using Mandrill;
using RadialReview.Accessors;
using RadialReview.Models.UserModels;
using Mandrill.Models;

namespace RadialReview.Models.ViewModels
{
    public class OrgMembersViewModel
    {
        public OrganizationModel Organization { get; set; }
        public List<OrgMemberViewModel> Users { get; set; }

		public bool ManagingOrganization { get; set; }

		public bool CanUpgradeUsers { get; set; }
		

        public OrgMembersViewModel(UserOrganizationModel caller,IEnumerable<UserLookup> members,OrganizationModel organization,bool hasAdminDelete,bool canUpgradeUsers,List<UserRole> roles)
        {
	        ManagingOrganization = caller.ManagingOrganization;
            Organization = organization;
			Users = members.ToListAlive().Select(x => new OrgMemberViewModel(x, hasAdminDelete,roles.Where(y=>y.UserId == x.UserId))).ToList();
			CanUpgradeUsers = canUpgradeUsers;
        }

		public List<MessageAccessor.ManageMembersMessage> Messages { get; set; } 
    }

    public class OrgMemberViewModel
    {
        public long Id { get;set; }
        public String Name { get; set; }
        public String Email { get; set; }
        public bool Verified { get; set; }
        public bool Manager { get; set; }
        public bool EmailSent { get; set; }
		public WebHookEventType? EmailEvent { get; set; }
        public int NumIndividualResponsibilities { get; set; }
        public int NumTotalResponsibilities { get; set; }
        //public int NumTeams { get; set; }
        //public int NumPositions { get; set; }
        public Boolean Managing { get; set; }
		public bool EvalOnly { get; set; }

		public List<UserRole> Roles { get; set; }

        public List<String> PositionTitles { get; set; }
        public List<String> TeamsTitles { get; set; }
        public List<string> ManagersTitles { get; set; }
        public bool Admin { get; set; }

	    public bool IsClient { get; set; }

        public OrgMemberViewModel(UserOrganizationModel userOrg,bool forceCanDelete,IEnumerable<UserRole> roles)
        {
            Id = userOrg.Id;
            Name = userOrg.GetName();
            Email = userOrg.GetEmail();
            Manager = userOrg.IsManager();
            Verified = userOrg.User != null;
            TeamsTitles = userOrg.Teams.ToListAlive().Select(x => x.Team.Name).ToList();
            PositionTitles = userOrg.Positions.ToListAlive().Select(x=>x.Position.CustomName).ToList();
            ManagersTitles = userOrg.ManagedBy.ToListAlive().Select(x => x.Manager.GetName()).ToList();
            NumIndividualResponsibilities = userOrg.Responsibilities.ToListAlive().Count();

	        EmailEvent = userOrg.TempUser.EmailStatus;
			EvalOnly = userOrg.EvalOnly;

			Roles = roles.ToList();

	        IsClient = userOrg.IsClient;
            EmailSent=true;
            if (userOrg.TempUser != null && userOrg.TempUser.LastSent == null)
                EmailSent = false;
            Admin = userOrg.ManagingOrganization;

			Managing = userOrg.GetPersonallyManaging();
			ForceCanDelete = forceCanDelete;

            //PositionTitle = userOrg.Positions.ToListAlive().FirstOrDefault().NotNull(x => x.Position.CustomName);

           /* NumTotalResponsibilities =
                userOrg.Responsibilities.ToListAlive().Count() +
                userOrg.Positions.ToListAlive().Sum(x => x.Position.Responsibilities.Count()) +
                userOrg.Teams.ToListAlive().Sum(x => x.Team.Responsibilities.Count());*/

	        throw new Exception("rocks roles measurables gone");
	        //NumRocks	   = userOrg.NumRocks;
	        //NumRoles	   = userOrg.NumRoles;
	        //NumMeasurables = userOrg.NumMeasurables;
        }

		public OrgMemberViewModel(UserLookup u, bool forceCanDelete, IEnumerable<UserRole> roles)
		{
			Id = u.UserId;
			Name = u.Name;
			Email = u.Email;
			Manager = u.IsManager;
			Verified = u.HasJoined;
			TeamsTitles = u.Teams.Split(',').Select(x=>x.Trim()).Where(x=>!String.IsNullOrWhiteSpace(x)).ToList();
			PositionTitles = u.Positions.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			ManagersTitles = u.Managers.Split(',').Select(x => x.Trim()).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
			EmailSent = u.HasSentInvite;
			Admin = u.IsAdmin;
			IsClient = u.IsClient;
			Managing = u._PersonallyManaging;

			EvalOnly = u.EvalOnly;

			EmailEvent = u.EmailStatus;
			//PositionTitle = userOrg.Positions.ToListAlive().FirstOrDefault().NotNull(x => x.Position.CustomName);

			/* NumTotalResponsibilities =
				 userOrg.Responsibilities.ToListAlive().Count() +
				 userOrg.Positions.ToListAlive().Sum(x => x.Position.Responsibilities.Count()) +
				 userOrg.Teams.ToListAlive().Sum(x => x.Team.Responsibilities.Count());*/

			NumRocks = u.NumRocks;
			NumRoles = u.NumRoles;
			NumMeasurables = u.NumMeasurables;
			ForceCanDelete = forceCanDelete;

			Roles = roles.ToList();

		}



		public int NumRocks { get; set; }

		public int NumRoles { get; set; }
		public int NumMeasurables { get; set; }

		public bool ForceCanDelete { get; set; }
	}
}