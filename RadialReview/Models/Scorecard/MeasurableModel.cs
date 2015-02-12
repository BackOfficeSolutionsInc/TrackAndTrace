﻿using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scorecard
{
	public class MeasurableModel :ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual string Title { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual LessGreater GoalDirection { get; set; }
		public virtual decimal Goal { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual DateTime NextGeneration { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual DayOfWeek DueDate { get; set; }
		public virtual TimeSpan DueTime { get; set; }

		public class MeasurableMap : ClassMap<MeasurableModel>
		{
			public MeasurableMap()
			{
				Id(x => x.Id);
				Map(x => x.Title);
				Map(x => x.NextGeneration);
				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();
				Map(x => x.Goal);
				Map(x => x.GoalDirection);
				Map(x => x.OrganizationId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.DueDate);
				Map(x => x.DueTime);
			} 
		}
	}
}