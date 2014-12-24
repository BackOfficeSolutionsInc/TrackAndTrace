using System.Management.Instrumentation;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Periods
{
	public class PeriodModel : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime StartTime { get; set; }
		public virtual DateTime EndTime { get; set; }
		public virtual String Name { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public class PeriodMap : ClassMap<PeriodModel>
		{
			public PeriodMap()
			{
				Id(x => x.Id);
				Map(x => x.DeleteTime);
				Map(x => x.StartTime);
				Map(x => x.EndTime); 
				Map(x => x.Name);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
			}
		}
	}
}