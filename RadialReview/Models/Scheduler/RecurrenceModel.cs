using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Scheduler
{
	public class RecurrenceModel : ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual DateTime StartDate { get; set; }
		public virtual DateTime EndDate { get; set; }
		public virtual TimeSpan StartTime { get; set; }
		public virtual TimeSpan EndTime { get; set; }
		public virtual RepeatsType Repeats { get; set; }
		public virtual int RepeatEvery { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual String Name { get; set; }
		public virtual String Description { get; set; }

		#region Weekly
			public virtual bool WeeklySunday { get; set; }
			public virtual bool WeeklyMonday { get; set; }
			public virtual bool WeeklyTuesday { get; set; }
			public virtual bool WeeklyWednesday { get; set; }
			public virtual bool WeeklyThursday { get; set; }
			public virtual bool WeeklyFriday { get; set; }
			public virtual bool WeeklySaturday { get; set; }
		#endregion

		#region Monthly
			public virtual bool MonthlyLast { get; set; }
			public virtual bool MonthlyFirst { get; set; }
			public virtual DayOfWeek MonthlyFirstLastDayOfWeek { get; set; }
			//Take start date day of month as calculation
			public virtual bool MonthlyDayOfMonth { get; set; }
			//Take start date day of week as calculation
			public virtual bool MonthlyDayOfWeek { get; set; }
		#endregion

		public class RecurrenceMap : ClassMap<RecurrenceModel>
		{
			public RecurrenceMap()
			{
				Id(x => x.Id);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.StartDate);
				Map(x => x.EndDate);
				Map(x => x.StartTime);
				Map(x => x.EndTime);
				Map(x => x.Repeats);
				Map(x => x.RepeatEvery);
				Map(x => x.Name);
				Map(x => x.Description);
				Map(x => x.DeleteTime);

				//Weekly
				Map(x => x.WeeklySunday);
				Map(x => x.WeeklyMonday);
				Map(x => x.WeeklyTuesday);
				Map(x => x.WeeklyWednesday);
				Map(x => x.WeeklyThursday);
				Map(x => x.WeeklyFriday);
				Map(x => x.WeeklySaturday);

				//Monthly
				Map(x => x.MonthlyLast);
				Map(x => x.MonthlyFirst);
				Map(x => x.MonthlyFirstLastDayOfWeek);
				Map(x => x.MonthlyDayOfMonth);
				Map(x => x.MonthlyDayOfWeek);
			}
		}

	}
}