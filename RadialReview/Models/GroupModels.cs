using FluentNHibernate.Mapping;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace RadialReview.Models
{
    public class GroupModel : ICustomQuestions, IDeletable
    {
        [Key]
        public virtual long Id { get; set; }
        [Display(Name="groupName",ResourceType=typeof(DisplayNameStrings))]
        public virtual String GroupName { get; set; }

        public virtual IList<UserOrganizationModel> GroupUsers { get; set; }

        public virtual IList<UserOrganizationModel> Managers { get; set; }

        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public OriginType QuestionOwner { get { return OriginType.Group; } }
        public GroupModel()
        {
            GroupUsers = new List<UserOrganizationModel>();
            Managers = new List<UserOrganizationModel>();
            CustomQuestions = new List<QuestionModel>();
        }


        public virtual DateTime? DeleteTime { get; set; }
    }

    public class GroupModelMap : ClassMap<GroupModel>
    {
        public GroupModelMap()
        {
            Id(x => x.Id);
            Map(x => x.GroupName);
            Map(x => x.DeleteTime);
            HasManyToMany(x => x.GroupUsers)
                .Table("GroupMembers")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.Managers)
                .Table("GroupManagement")
                .Cascade.SaveUpdate();
            HasMany(x => x.CustomQuestions)
                .KeyColumn("GroupQuestion_Id")
                .Inverse();
        }
    }


}
