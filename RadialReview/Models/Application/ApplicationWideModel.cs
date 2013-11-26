
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
    public class ApplicationWideModel : IOrigin
    {
        public virtual long Id { get; protected set; }
        public virtual IList<QuestionModel> CustomQuestions { get; set; }
        public virtual OriginType GetOriginType()
        {
            return OriginType.Application;
        }
        public virtual String GetSpecificNameForOrigin() { 
                return ProductStrings.ProductName;
        }
        public virtual IList<OrganizationModel> Organizations { get; set; }

        public virtual List<IOrigin> OwnsOrigins()
        {
            return Organizations.Cast<IOrigin>().ToList();
        }

        public virtual List<IOrigin> OwnedByOrigins()
        {
            return new List<IOrigin>();
        }

        public virtual IList<PaymentPlanModel> PaymentPlans { get; set; }

        public ApplicationWideModel()
        {
            Organizations = new List<OrganizationModel>();
            CustomQuestions = new List<QuestionModel>();
        }

        public ApplicationWideModel(long id) :this()
        {
            Id = id;
        }

    }

    public class ApplicationWideModelMap : ClassMap<ApplicationWideModel>
    {
        public ApplicationWideModelMap()
        {
            Id(x => x.Id);
            HasMany(x => x.CustomQuestions)
                .KeyColumn("ApplicationQuestion_Id")
                .Inverse();

            HasMany(x => x.Organizations)
                .KeyColumn("ApplicationOrganizations")
                .Inverse();
        }
    }
}