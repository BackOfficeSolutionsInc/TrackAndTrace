﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Askables;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10
{
	public class L10Recurrence : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual String Name { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public virtual IList<L10Recurrence_Attendee> _DefaultAttendees { get; set; }
		public virtual IList<L10Recurrence_Measurable> _DefaultMeasurables { get; set; }
		public virtual IList<L10Recurrence_Rocks> _DefaultRocks { get; set; }

		public virtual List<L10Note> _MeetingNotes { get; set; }
		public virtual long? MeetingInProgress { get; set; }

		public class L10RecurrenceMap : ClassMap<L10Recurrence>
		{
			public L10RecurrenceMap()
			{
				Id(x => x.Id);
				Map(x => x.Name).Length(10000);
				Map(x => x.CreateTime);
				Map(x => x.MeetingInProgress);
				Map(x => x.DeleteTime);

				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();

				//HasMany(x => x.DefaultAttendees).KeyColumn("L10RecurrenceId");
				//HasMany(x => x.DefaultMeasurables).KeyColumn("L10RecurrenceId");
			}
		}

		public class L10Recurrence_Rocks : ILongIdentifiable, IDeletable, IOneToMany
		{
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual RockModel ForRock { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }
			public L10Recurrence_Rocks()
			{
				CreateTime = DateTime.UtcNow;
			}

			public class L10Recurrence_RocksMap : ClassMap<L10Recurrence_Rocks>
			{
				public L10Recurrence_RocksMap()
				{
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					References(x => x.L10Recurrence).Column("L10RecurrenceId");//.LazyLoad().ReadOnly().Not.Nullable().Cascade.SaveUpdate();
					References(x => x.ForRock).Column("RockId");//.LazyLoad().ReadOnly().Not.Nullable().Cascade.SaveUpdate();
				}
			}

			public virtual object UniqueKey()
			{
				return Tuple.Create(ForRock.Id, L10Recurrence.Id, DeleteTime);
			}
		}


		public class L10Recurrence_Attendee : ILongIdentifiable, IDeletable, IOneToMany
		{
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			//public virtual long L10RecurrenceId { get; set; }
			//public virtual long UserId { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }

			public L10Recurrence_Attendee()
			{
				CreateTime = DateTime.UtcNow;
			}
			public class L10Recurrence_AttendeeMap : ClassMap<L10Recurrence_Attendee>
			{
				public L10Recurrence_AttendeeMap()
				{
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					//Map(x => x.L10RecurrenceId).Column("L10RecurrenceId");
					References(x => x.L10Recurrence).Column("L10RecurrenceId");//.LazyLoad().ReadOnly().Not.Nullable().Cascade.SaveUpdate();
					//Map(x => x.UserId).Column("UserId");
					References(x => x.User).Column("UserId");//.LazyLoad().ReadOnly().Not.Nullable().Cascade.SaveUpdate();
				}
			}
			public virtual object UniqueKey()
			{
				return Tuple.Create(User.Id, L10Recurrence.Id, DeleteTime);
			}
		}
		public class L10Recurrence_Measurable : ILongIdentifiable, IDeletable, IOneToMany
		{
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			//public virtual long L10RecurrenceId { get; set; }
			//public virtual long MeasurableId { get; set; }
			public virtual MeasurableModel Measurable { get; set; }
			public virtual L10Recurrence L10Recurrence { get; set; }

			public L10Recurrence_Measurable()
			{
				CreateTime = DateTime.UtcNow;
			}
			public class L10Recurrence_MeasurableMap : ClassMap<L10Recurrence_Measurable>
			{
				public L10Recurrence_MeasurableMap()
				{
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					//Map(x => x.L10RecurrenceId).Column("L10RecurrenceId");
					References(x => x.L10Recurrence, "L10RecurrenceId");

					//Map(x => x.MeasurableId).Column("MeasurableId");
					References(x => x.Measurable, "MeasurableId");
				}
			}
			public virtual object UniqueKey()
			{
				return Tuple.Create(Measurable.Id, L10Recurrence.Id, DeleteTime);
			}
		}


	}
}