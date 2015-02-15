using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scorecard
{
	public class ScoreModel : ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DateEntered { get; set; }
		public virtual DateTime DateDue { get; set; }
		public virtual DateTime ForWeek { get; set; }
		public virtual long MeasurableId { get; set; }
		public virtual MeasurableModel Measurable { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual decimal? Measured { get; set; }

		public virtual DateTime? DeleteTime { get; set; }

		public ScoreModel(){
			
		}

		public class ScoreMap : ClassMap<ScoreModel>
		{
			public ScoreMap()
			{
				Id(x => x.Id);
				Map(x => x.DateEntered);
				Map(x => x.DateDue);
				Map(x => x.ForWeek);
				Map(x => x.Measured);
				Map(x => x.OrganizationId);
				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();

				Map(x => x.MeasurableId).Column("MeasureableId");
				References(x => x.Measurable).Column("MeasureableId").Not.LazyLoad().ReadOnly();
				Map(x => x.DeleteTime);
			}
		}

	}
}