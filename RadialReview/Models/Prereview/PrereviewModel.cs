using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Prereview
{
    public class PrereviewModel : IDeletable
    {
		public virtual ReviewsModel _ReviewContainer { get; set; }

		public virtual long Id { get; set; }
        //who customizes this review?
        public virtual long ManagerId { get; set; }
        public virtual DateTime PrereviewDue { get; set; }
        public virtual long ReviewContainerId { get; set; }
        public virtual bool Started { get; set; }
        public virtual DateTime? Executed { get; set; }
        public virtual DateTime? DeleteTime { get; set; }
    }

    public class PrereviewModelMap : ClassMap<PrereviewModel>
    {
        public PrereviewModelMap()
        {
            Id(x => x.Id);
            Map(x => x.Started);
            Map(x => x.PrereviewDue);
            Map(x => x.ReviewContainerId);
            Map(x => x.ManagerId);
            Map(x => x.Executed);
            Map(x => x.DeleteTime);
        }
    }
}