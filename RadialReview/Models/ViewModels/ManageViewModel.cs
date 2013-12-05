using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.ViewModels
{
    public class DisplayUser
    {
        public String Name { get; set; }
        public long UserOrganizationId { get; set; }

    }

    public class ManageViewModel
    {
        public List<UserOrganizationModel> ManagedUsers { get; set; }
        public List<UserOrganizationModel> AllSubordinates { get; set; }
        public List<GroupModel> ManagingGroups { get; set; }
        public List<UserOrganizationModel> PendingUsers { get; set; }
        public OrganizationModel Organization { get; set; }
        public List<QuestionCategoryModel> Categories { get; set; }

        public List<ReviewsViewModel> ManagingReviews {get;set;}

        public QuestionsViewModel OrganizationQuestions { get; set; }

        public ManageViewModel(UserOrganizationModel orgUser)
        {
            ManagedUsers = orgUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate).Where(x =>  x.IsAttached()).ToListAlive();
            PendingUsers = orgUser.ManagingUsers.ToListAlive().Select(x => x.Subordinate).Where(x => !x.IsAttached()).ToListAlive();
            ManagingGroups = orgUser.ManagingGroups.ToListAlive();
            Organization = orgUser.Organization;
            /*PendingUsers = orgUser.CreatedNexuses
                .Alive()
                .Where(x => x.DateExecuted==null && x.ActionCode == NexusActions.JoinOrganizationUnderManager)
                .Select(x => new PendingUsers() { EmailAddress = x.GetArgs()[1],Date=x.DateCreated,Id=long.Parse(x.GetArgs()[2]) })
                .ToList();*/
            Categories = orgUser.Organization.QuestionCategories.ToListAlive();
            OrganizationQuestions = new QuestionsViewModel(Organization.Id, OriginType.Organization, Organization.Id, Organization.CustomQuestions);
            AllSubordinates = orgUser.AllSubordinates;

            //TODO calculate completion
            ManagingReviews = orgUser.CreatedReviews.Select(x=>new ReviewsViewModel(x)).ToList();
        }

    }
}