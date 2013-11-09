using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models
{
    public class IndustryModel : ICustomQuestions
    {
        public long Id { get; protected set; }
        public IList<QuestionModel> CustomQuestions { get; set; }
        public OriginType QuestionOwner { get { return OriginType.Industry; } }
        public String Name { get; set; }
        public List<IndustryModel> Subindustries { get; set; }
        public List<IndustryModel> Superindustries { get; set; }

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