
using FluentNHibernate.Automapping;
using FluentNHibernate.Automapping.Alterations;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class UserOrganizationModel : ICustomQuestions, IDeletable
    {
        public virtual long Id { get; protected set; }
        public virtual Boolean ManagerAtOrganization { get; set; }
        public virtual Boolean ManagingOrganization { get; set; }
        public virtual Boolean IsRadialAdmin { get; set; }
        public virtual String Title { get; set; }
        public virtual DateTime AttachTime { get; set; }
        public virtual DateTime? DetachTime { get; set; }
        public virtual UserModel User { get; set; }
        public virtual OrganizationModel Organization { get; set; }
        public virtual IList<UserOrganizationModel> ManagingUsers { get; set; }
        public virtual IList<UserOrganizationModel> ManagedBy { get; set; }
        public virtual IList<GroupModel> Groups { get; set; }
        public virtual IList<GroupModel> ManagingGroups { get; set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual IList<NexusModel> CreatedNexuses { get; set; }
        public virtual IList<QuestionModel> CreatedQuestions { get; set; }

        public virtual DateTime? DeleteTime { get; set; }        
        public virtual OriginType QuestionOwner { get { return OriginType.User; } }

        #region Helpers
            public virtual IList<UserOrganizationModel> GetManagingUsersAndSelf()
            {
                return ManagingUsers.Union(new List<UserOrganizationModel> { this }).ToList();
            }
            public virtual Boolean IsManager { get { return IsRadialAdmin || ManagerAtOrganization || ManagingOrganization; } }
            public virtual Boolean IsManagerCanEditOrganization { get { return IsRadialAdmin || ManagingOrganization || (ManagerAtOrganization && Organization.ManagersCanEdit); } }
            

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
            }

        #endregion

        public UserOrganizationModel()
        {
            ManagedBy = new List<UserOrganizationModel>();
            ManagingUsers = new List<UserOrganizationModel>();
            Groups = new List<GroupModel>();
            ManagingGroups = new List<GroupModel>();
            CustomQuestions = new List<QuestionModel>();
            CreatedNexuses = new List<NexusModel>();
            AttachTime = DateTime.UtcNow;
            CreatedQuestions = new List<QuestionModel>();
        }
    }

    public class UserOrganizationModelMap: ClassMap<UserOrganizationModel>
    {
        public UserOrganizationModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Title);

            Map(x => x.IsRadialAdmin);
            Map(x => x.ManagingOrganization);
            Map(x => x.ManagerAtOrganization);
            Map(x => x.AttachTime);
            Map(x => x.DetachTime);
            Map(x => x.DeleteTime);

            References(x => x.User)
                .Cascade.SaveUpdate();
            References(x => x.Organization)
                .Column("Organization_Id")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.ManagedBy)
                .Table("ManagedByMembers")
                .ParentKeyColumn("Subordinate")
                .ChildKeyColumn("Manager")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.ManagingUsers)
                .Table("ManagingMembers")
                .ParentKeyColumn("Manager")
                .ChildKeyColumn("Subordinate")
                .Cascade.SaveUpdate();
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