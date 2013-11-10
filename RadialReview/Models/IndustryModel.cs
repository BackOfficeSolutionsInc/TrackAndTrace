using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class IndustryModel : IOrigin
    {
        public virtual long Id { get; protected set; }
        public virtual String Name { get; set; }
        public virtual List<IndustryModel> Subindustries { get; set; }
        public virtual List<IndustryModel> Superindustries { get; set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual OriginType QuestionOwnerType()
        {
            return OriginType.Industry;
        }

        public virtual String OriginCustomName
        {
            get
            {
                return DisplayNameStrings.industry + "(" + Name + ")";
            }
        }

        public IndustryModel()
        {
            Subindustries = new List<IndustryModel>();
            Superindustries = new List<IndustryModel>();
            CustomQuestions = new List<QuestionModel>();
        }

    }

    public class IndustryModelMap : ClassMap<IndustryModel>
    {
        public IndustryModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);

            HasMany(x => x.CustomQuestions)
                .KeyColumn("IndustryQuestion_Id")
                .Inverse();
            
            HasManyToMany(x => x.Superindustries)
                .Table("IndustryHierarchy")
                .ParentKeyColumn("subindustry")
                .ChildKeyColumn("superindustry")
                .Cascade.SaveUpdate();
            HasManyToMany(x => x.Subindustries)
                .Table("IndustryHierarchy")
                .ParentKeyColumn("superindustry")
                .ChildKeyColumn("subindustry")
                .Cascade.SaveUpdate();
        }
    }
}