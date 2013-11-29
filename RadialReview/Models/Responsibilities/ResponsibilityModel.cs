using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Responsibilities
{
    public class ResponsibilityModel : ILongIdentifiable, IDeletable
    {
        public virtual long Id { get; set; }
        public virtual long ForOrganizationId { get;set; }
        public virtual long ForResponsibilityGroup { get; set; }
        public virtual String Responsibility { get; set; }
        public virtual QuestionCategoryModel Category {get;set;}

        public virtual DateTime? DeleteTime { get; set; }
    }

    public class ResponsibilityModelMap : ClassMap<ResponsibilityModel>
    {
        public ResponsibilityModelMap()
        {
            Id(x => x.Id);
            Map(x => x.DeleteTime);
            Map(x => x.Responsibility);
            Map(x => x.ForOrganizationId);
            Map(x => x.ForResponsibilityGroup);
            References(x => x.Category).Not.LazyLoad();
        }
    }
}