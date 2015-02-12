using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10
{
	public class L10Meeting : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual long L10RecurrenceId { get; set; }
		public virtual L10Recurrence L10Recurrence { get; set; }
		public virtual IList<L10MeetingAttendee> MeetingAttendees { get; set; }
		/// <summary>
		/// Current meetings measurables. Needed in case meeting measurables change throughout time
		/// </summary>
		public virtual IList<MeasurableModel> MeetingMeasurables { get; set; }


		public class L10MeetingMap : ClassMap<L10Meeting>
		{
			public L10MeetingMap()
			{
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.L10RecurrenceId).Column("L10RecurrenceId"); ;
				References(x => x.L10Recurrence).Column("L10RecurrenceId").Not.LazyLoad().ReadOnly();

				HasMany(x => x.MeetingAttendees).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();
				HasMany(x => x.MeetingMeasurables).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();

			}
		}
	}
}