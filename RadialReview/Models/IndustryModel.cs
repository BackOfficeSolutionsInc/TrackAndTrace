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
        public virtual LocalizedStringModel Name { get; set; }
        public virtual IList<IndustryModel> Subindustries { get; set; }
        public virtual IList<IndustryModel> Superindustries { get; set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }

        public virtual ApplicationWideModel Application { get; set; }

        public virtual OriginType GetOriginType()
        {
            return OriginType.Industry;
        }

        public virtual String GetSpecificNameForOrigin()
        {
            return DisplayNameStrings.industry + "(" + Name.Translate() + ")";
        }

        public IndustryModel()
        {
            Subindustries = new List<IndustryModel>();
            Superindustries = new List<IndustryModel>();
            CustomQuestions = new List<QuestionModel>();
        }

        public virtual List<IOrigin> OwnsOrigins()
        {
            var owns=new List<IOrigin>(Subindustries);
            return owns;
        }

        public virtual List<IOrigin> OwnedByOrigins()
        {
            var ownedBy = new List<IOrigin>(Superindustries);
            ownedBy.Add(Application);
            return ownedBy;
        }
    }

    public class IndustryModelMap : ClassMap<IndustryModel>
    {
        public IndustryModelMap()
        {
            Id(x => x.Id);
            References(x => x.Name).Not.LazyLoad();

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