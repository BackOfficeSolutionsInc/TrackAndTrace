using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.AccountabilityGroupModels
{
    public class AccountabilitiesModel
    {
        public virtual long Id { get; set; }
        public virtual long ForOrganizationId { get;set; }
        public virtual long ForAccountabilityGroup { get; set; }
        public virtual String Accountability { get; set; }
        public virtual QuestionCategoryModel Category {get;set;}        
    }

    public class AccountabilitiesModelMap : ClassMap<AccountabilitiesModel>
    {
        public AccountabilitiesModelMap()
        {
            Id(X => X.Id);
            Map(x => x.Accountability);
            Map(x => x.ForOrganizationId);
            Map(x => x.ForAccountabilityGroup);
            References(x => x.Category).Not.LazyLoad();
        }
    }
}