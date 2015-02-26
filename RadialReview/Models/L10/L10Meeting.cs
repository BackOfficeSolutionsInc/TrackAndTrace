using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;

namespace RadialReview.Models.L10
{
	public class L10Meeting : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual Guid HubId { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime? StartTime { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual long L10RecurrenceId { get; set; }
		public virtual L10Recurrence L10Recurrence { get; set; }
		public virtual IList<L10Meeting_Attendee> _MeetingAttendees { get; set; }
		public virtual long MeetingLeaderId { get; set; }
		public virtual UserOrganizationModel MeetingLeader { get; set; }
		/// <summary>
		/// Current meetings measurables. Needed in case meeting measurables change throughout time
		/// </summary>
		public virtual IList<L10Meeting_Measurable> _MeetingMeasurables { get; set; }
		public virtual IList<L10Meeting_Measurable> _MeetingConnections { get; set; }
		public virtual IList<L10Meeting_Log> _MeetingLogs { get; set; }
		public virtual IList<Tuple<string,double>> _MeetingLeaderPageDurations { get; set; }
		public virtual String _MeetingLeaderCurrentPage { get; set; }
		public virtual DateTime? _MeetingLeaderCurrentPageStartTime { get; set; }
		public virtual double? _MeetingLeaderCurrentPageBaseMinutes { get; set; }

		
		public L10Meeting()
		{
			_MeetingAttendees=new List<L10Meeting_Attendee>();
			_MeetingMeasurables=new List<L10Meeting_Measurable>();
			HubId = Guid.NewGuid();
		}

		public class L10MeetingMap : ClassMap<L10Meeting>
		{
			public L10MeetingMap()
			{
				Id(x => x.Id);
				Map(x => x.HubId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.StartTime);
				Map(x => x.CompleteTime);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.L10RecurrenceId).Column("L10RecurrenceId"); ;
				References(x => x.L10Recurrence).Column("L10RecurrenceId").Not.LazyLoad().ReadOnly();
				Map(x => x.MeetingLeaderId).Column("MeetingLeaderId"); ;
				References(x => x.MeetingLeader).Column("MeetingLeaderId").Not.LazyLoad().ReadOnly();

				//HasMany(x => x.MeetingAttendees).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();
				//HasMany(x => x.MeetingMeasurables).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();

			}
		}

		public class L10Meeting_Measurable : IDeletable, ILongIdentifiable
		{
			public virtual long Id { get; set; }
			//public virtual long UserId { get; set; }
			//public virtual long L10MeetingId { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual MeasurableModel Measurable { get; set; }

			public class L10Meeting_MeasurableMap : ClassMap<L10Meeting_Measurable>
			{
				public L10Meeting_MeasurableMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					References(x => x.Measurable).Column("MeasurableId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");
				}
			}
		}
		public class L10Meeting_Log : IDeletable, ILongIdentifiable
		{
			public virtual long Id { get; set; }
			//public virtual long UserId { get; set; }
			//public virtual long L10MeetingId { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual String Page { get; set; }
			public virtual DateTime StartTime { get; set; }
			public virtual DateTime? EndTime { get; set; }

			public class L10Meeting_LogMap : ClassMap<L10Meeting_Log>
			{
				public L10Meeting_LogMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.Page);//.Not.LazyLoad().ReadOnly();
					Map(x => x.StartTime);//.Not.LazyLoad().ReadOnly();
					Map(x => x.EndTime);//.Not.LazyLoad().ReadOnly();
					References(x => x.User).Column("UserId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");
				}
			}
		}

		

		public class L10Meeting_Connection : IDeletable, ILongIdentifiable
		{
			public virtual long Id { get; set; }
			//public virtual long UserId { get; set; }
			//public virtual long L10MeetingId { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual String ConnectionId { get; set; }
			public virtual UserOrganizationModel User { get; set; }

			public virtual int StartSelection { get; set; }
			public virtual int EndSelection { get; set; }
			public virtual string FocusId { get; set; }
			public virtual FocusType FocusType { get; set; }

			public L10Meeting_Connection()
			{
				CreateTime = DateTime.UtcNow;
			}

			public class L10Meeting_ConnectionMap : ClassMap<L10Meeting_Connection>
			{
				public L10Meeting_ConnectionMap()
				{
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.StartSelection);
					Map(x => x.EndSelection);
					Map(x => x.FocusId);
					Map(x => x.FocusType);
					Map(x => x.ConnectionId);
					References(x => x.User).Column("UserId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");


				}
			}
		}

		public class L10Meeting_Attendee : IDeletable, ILongIdentifiable
		{
			public virtual long Id { get; set; }
			public virtual long UserId { get; set; }
			public virtual UserOrganizationModel User { get; set; }
			public virtual long L10MeetingId { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual int? Rating { get; set; }
			public virtual DateTime? DeleteTime { get; set; }

			public class L10MeetingAttendeeMap : ClassMap<L10Meeting_Attendee>
			{
				public L10MeetingAttendeeMap()
				{
					Id(x => x.Id);
					Map(x => x.Rating);
					Map(x => x.DeleteTime);
					References(x => x.User).Column("UserId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");


				}
			}

		}

	}
}