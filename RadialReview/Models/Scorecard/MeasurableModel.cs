﻿using System;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scorecard
{
	public class MeasurableModel :ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual long? FromTemplateItemId { get; set; }
		public virtual string Title { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual long AdminUserId { get; set; }
		public virtual UserOrganizationModel AdminUser { get; set; }
		public virtual LessGreater GoalDirection { get; set; }
		public virtual decimal Goal { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual DateTime NextGeneration { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual DayOfWeek DueDate { get; set; }
		public virtual TimeSpan DueTime { get; set; }

		public virtual UnitType UnitType { get; set; }


		public MeasurableModel(){
		}

		public MeasurableModel(OrganizationModel forOrganization)
		{
			var now = DateTime.UtcNow;
			CreateTime = now;
			NextGeneration =now- TimeSpan.FromDays(7);
			DueDate = DayOfWeek.Friday;

			DueTime = forOrganization.ConvertToUTC(TimeSpan.FromHours(12));
				
			//	TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(-1*forOrganization.Settings.TimeZoneOffsetMinutes));
			Organization = forOrganization;
			OrganizationId = forOrganization.Id;
		}

		public class MeasurableMap : ClassMap<MeasurableModel>
		{
			public MeasurableMap()
			{
				//Table("measurablemodel");
				Id(x => x.Id);
				Map(x => x.Title);
				Map(x => x.NextGeneration);
				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();
				Map(x => x.AdminUserId).Column("AdminUserId");
				References(x => x.AdminUser).Column("AdminUserId").LazyLoad().ReadOnly();
				Map(x => x.Goal);
				Map(x => x.GoalDirection);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.UnitType);

				Map(x => x.DueDate);
				Map(x => x.DueTime);

				Map(x => x.FromTemplateItemId);
			} 
		}
		
		public virtual string ToSymbolString(){
			return GoalDirection.ToSymbol()+" "+Goal.ToString("0.#####");
		}
	}
}