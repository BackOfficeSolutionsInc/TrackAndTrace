
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using FluentNHibernate.Mapping;
using NHibernate.Proxy;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class UserOrganizationModel : ResponsibilityGroupModel, IOrigin, IDeletable
    {
        public virtual TempUserModel TempUser { get; set; }
        public virtual String EmailAtOrganization { get; set; }
        public virtual Boolean ManagerAtOrganization { get; set; }
        public virtual Boolean ManagingOrganization { get; set; }
        public virtual Boolean IsRadialAdmin { get; set; }
        //public virtual String Title { get; set; }
        public virtual DateTime AttachTime { get; set; }
        public virtual DateTime? DetachTime { get; set; }
        public virtual UserModel User { get; set; }
        public virtual IList<ManagerDuration> ManagingUsers { get; set; }
        public virtual IList<ManagerDuration> ManagedBy { get; set; }
        public virtual IList<GroupModel> Groups { get; set; }
        public virtual IList<GroupModel> ManagingGroups { get; set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual IList<NexusModel> CreatedNexuses { get; set; }
        public virtual IList<QuestionModel> CreatedQuestions { get; set; }
        public virtual IList<ReviewModel> Reviews { get; set; }
        public virtual List<ReviewsModel> CreatedReviews { get; set; }
        public virtual IList<PositionDurationModel> Positions { get; set; }
        public virtual IList<TeamDurationModel> Teams { get; set; }
        public virtual DateTime? DeleteTime { get; set; }

        public virtual String JobDescription { get; set; }

        public virtual OriginType GetOriginType()
        {
            return OriginType.User;
        }

        public virtual String GetSpecificNameForOrigin()
        {
            return this.GetName();
        }

        #region Helpers
        public virtual Dictionary<String, List<String>> Properties { get; set; }
        public virtual Boolean IsAttached()
        {
            return User != null;
        }
        public virtual List<UserOrganizationModel> AllSubordinates { get; set; }
        /*
        public virtual List<OriginType> EditableQuestionOrigins
        {
            get
            {
                var origins = new List<OriginType>();
                if (IsManager)
                    origins.Add(OriginType.User);
                if (ManagerAtOrganization)
                    origins.Add(OriginType.Group);
                if (IsManagerCanEditOrganization)
                    origins.Add(OriginType.Organization);
                return origins;
            }
        }*/
        #endregion

        public UserOrganizationModel()
            : base()
        {
            ManagedBy = new List<ManagerDuration>();
            ManagingUsers = new List<ManagerDuration>();
            Groups = new List<GroupModel>();
            ManagingGroups = new List<GroupModel>();
            CustomQuestions = new List<QuestionModel>();
            CreatedNexuses = new List<NexusModel>();
            CreatedQuestions = new List<QuestionModel>();
            AttachTime = DateTime.UtcNow;
            //AllSubordinates = new List<UserOrganizationModel>();
            Properties = new Dictionary<string, List<String>>();
            //CreatedReviews = new List<ReviewsModel>();
            Reviews = new List<ReviewModel>();
            Positions = new List<PositionDurationModel>();
            Teams = new List<TeamDurationModel>();
            TempUser = null;
        }

        public override string ToString()
        {
            return Organization.NotNull(x => x.Name) + " - " + this.GetNameAndTitle();
        }


        public virtual List<IOrigin> OwnsOrigins()
        {
            var owns = new List<IOrigin>();
            owns.AddRange(ManagingUsers.Cast<IOrigin>());
            owns.AddRange(ManagingGroups.Cast<IOrigin>());
            owns.AddRange(CreatedQuestions.Cast<IOrigin>());
            return owns;
        }

        public virtual List<IOrigin> OwnedByOrigins()
        {
            var ownedBy = new List<IOrigin>();
            ownedBy.AddRange(ManagedBy.Cast<IOrigin>());
            ownedBy.Add(Organization);
            return ownedBy;
        }

        public override string GetName()
        {
            if (this.User != null)
                return this.User.Name();

            if (TempUser != null)
                return this.TempUser.Name();

            return this.EmailAtOrganization;
        }

        public virtual string GetTitles(int numToShow = int.MaxValue, long callerUserId = -1)
        {
            if (this.Positions == null)
                return "";

            var count = this.Positions.Count();

            String titles = null;
            var actualPositions = Positions.ToListAlive().Select(x => x.Position.CustomName).ToList();
            if (callerUserId == Id)
                actualPositions.Insert(0, "You");

            titles = String.Join(", ", actualPositions.Take(numToShow));
            if (actualPositions.Count > numToShow)
                titles += ",...";

            return titles;
        }

        public override string GetGroupType()
        {
            return DisplayNameStrings.user;
        }

    }

    public class UserOrganizationModelMap : SubclassMap<UserOrganizationModel>
    {
        public UserOrganizationModelMap()
        {
            //Map(x => x.Title);

            Map(x => x.IsRadialAdmin);
            Map(x => x.ManagingOrganization);
            Map(x => x.ManagerAtOrganization);
            Map(x => x.AttachTime);
            Map(x => x.DetachTime);
            Map(x => x.DeleteTime);
            Map(x => x.EmailAtOrganization);

            Map(x => x.JobDescription).Length(65000);

            References(x => x.TempUser).Not.LazyLoad().Cascade.All();

            //Reviews
            HasMany(x => x.Reviews)
                .Cascade.SaveUpdate();
            /*HasMany(x => x.Responsibilities)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();*/
            //HasMany(x => x.CreatedReviews).Cascade.SaveUpdate();

            HasMany(x => x.Positions)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();

            References(x => x.User)
                .Not.LazyLoad()
                .Cascade.SaveUpdate();
            /*References(x => x.Organization)
                .Column("Organization_Id")
                .Cascade.SaveUpdate();*/
            HasMany(x => x.Teams)
                .KeyColumn("User_id")
                .Cascade.SaveUpdate();

            HasMany(x => x.ManagedBy)
                .KeyColumn("SubordinateId")
                .Cascade.SaveUpdate();

            HasMany(x => x.ManagingUsers)
                .KeyColumn("ManagerId")
                .Cascade.SaveUpdate();


            /*HasManyToMany(x => x.ManagedBy)
                .Table("ManagedMembers")
                .ParentKeyColumn("Subordinate")
                .ChildKeyColumn("Manager")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.ManagingUsers)
                .Table("ManagedMembers")
                .ParentKeyColumn("Manager")
                .ChildKeyColumn("Subordinate")
                .Cascade.SaveUpdate();*/


            HasManyToMany(x => x.Groups)
                .Table("GroupMembers")
                .Inverse();
            HasManyToMany(x => x.ManagingGroups)
                .Table("GroupManagement")
                .Inverse();
            HasMany(x => x.CustomQuestions)
                .KeyColumn("UserQuestion_Id")
                .Inverse();
            HasMany(x => x.CreatedQuestions)
                .KeyColumn("CreatedQuestionsId")
                .Inverse();
            HasManyToMany(x => x.CreatedNexuses)
                .Cascade.SaveUpdate()
                .Table("UserOrganizationNexuses");


        }
    }
}