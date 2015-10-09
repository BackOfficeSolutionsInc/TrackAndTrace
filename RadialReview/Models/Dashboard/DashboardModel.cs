using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Dashboard
{
	public class Dashboard : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual string Title { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual UserModel ForUser { get; set; }
		public virtual bool PrimaryDashboard { get; set; }

		public Dashboard(){
			CreateTime = DateTime.UtcNow;
		}

		

	}
	public class DashboardMap : ClassMap<Dashboard>
	{
		public DashboardMap()
		{
			Id(x => x.Id);
			Map(x => x.Title);
			Map(x => x.PrimaryDashboard);
			Map(x => x.CreateTime);
			Map(x => x.DeleteTime);
			References(x => x.ForUser).Nullable();
		}
	}
}