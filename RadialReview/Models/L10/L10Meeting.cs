using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Mapping;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.VideoConference;

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
		public virtual long MeetingLeaderId { get; set; }
		public virtual UserOrganizationModel MeetingLeader { get; set; }

	//	public virtual VideoConferenceType SelectedVideoType { get; set; }
		public virtual long? SelectedVideoProviderId { get; set; }
		public virtual AbstractVCProvider SelectedVideoProvider { get; set; }
		//public virtual string VideoUrl { get; set; }

		/// <summary>
		/// Current meetings measurables. Needed in case meeting measurables change throughout time
		/// </summary>
		public virtual IList<L10Meeting_Measurable> _MeetingMeasurables { get; set; }
		public virtual IList<L10Meeting_Measurable> _MeetingConnections { get; set; }
		public virtual IList<L10Meeting_Attendee> _MeetingAttendees { get; set; }
		public virtual IList<L10Meeting_Rock> _MeetingRocks { get; set; }
		public virtual IList<L10Meeting_Log> _MeetingLogs { get; set; }
		public virtual IList<Tuple<string,double>> _MeetingLeaderPageDurations { get; set; }
		//public virtual IList<L10Note> _MeetingNotes { get; set; }

		public virtual String _MeetingLeaderCurrentPage { get; set; }
		//public virtual String _MeetingLeaderCurrentPageType { get; set; }
		public virtual DateTime? _MeetingLeaderCurrentPageStartTime { get; set; }
		public virtual double? _MeetingLeaderCurrentPageBaseMinutes { get; set; }
		
		public virtual Ratio TodoCompletion { get; set; }

		public virtual bool Preview { get; set; }


		public L10Meeting()
		{
			_MeetingAttendees = new List<L10Meeting_Attendee>();
			_MeetingMeasurables = new List<L10Meeting_Measurable>();
			_MeetingRocks = new List<L10Meeting_Rock>();
			HubId = Guid.NewGuid();
		}

		public class L10MeetingMap : ClassMap<L10Meeting>
		{
			public L10MeetingMap()
			{
				Id(x => x.Id);
				Map(x => x.HubId);
				Map(x => x.SelectedVideoProviderId).Column("SelectedVideoProviderId");
				References(x => x.SelectedVideoProvider).Column("SelectedVideoProviderId").LazyLoad().Nullable().ReadOnly();

				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.StartTime);
				Map(x => x.CompleteTime);
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.L10RecurrenceId).Column("L10RecurrenceId");
				References(x => x.L10Recurrence).Column("L10RecurrenceId").Not.LazyLoad().ReadOnly();
				Map(x => x.MeetingLeaderId).Column("MeetingLeaderId");
				References(x => x.MeetingLeader).Column("MeetingLeaderId").Not.LazyLoad().ReadOnly();

				Map(x => x.Preview);

                Component(x => x.TodoCompletion).ColumnPrefix("TodoCompletion_");

				//HasMany(x => x.MeetingAttendees).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();
				//HasMany(x => x.MeetingMeasurables).KeyColumn("L10MeetingId").Not.LazyLoad().Cascade.None();
			}
		}

		public class L10Meeting_Rock : ILongIdentifiable, IDeletable, IIssue, ITodo
		{
			public virtual long Id { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual DateTime? CompleteTime { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual RockState Completion { get; set; }
			public virtual L10Meeting L10Meeting { get; set; }
			public virtual RockModel ForRock { get; set; }
			public virtual L10Recurrence ForRecurrence { get; set; }

			public virtual bool VtoRock { get; set; }

			public L10Meeting_Rock()
			{
				CreateTime = DateTime.UtcNow;
				Completion=RockState.Indeterminate;
			}

			public class L10Meeting_RockMap : ClassMap<L10Meeting_Rock>
			{
				public L10Meeting_RockMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x.CompleteTime);
					Map(x => x.CreateTime);
					Map(x => x.Completion);
					Map(x => x.VtoRock);
					References(x => x.ForRock).Column("RockId");
					References(x => x.L10Meeting).Column("MeetingId");
					References(x => x.ForRecurrence).Column("RecurrenceId");
				}
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			public virtual async Task<string> GetTodoMessage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
                return "";
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			public virtual async Task<string> GetTodoDetails()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
				var week = L10Meeting.CreateTime.StartOfWeek(DayOfWeek.Sunday).ToString("d");
				var accountable = ForRock.AccountableUser.GetName();
                
                var footer = "'" + ForRock.Rock + "'\n\n"+"Week: " + week + "\nOwner: " + accountable;
				return footer;
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			public virtual async Task<string> GetIssueMessage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
                return ForRock.Rock;
				//var name = "'" + ForRock.Rock + "'";
				//switch(Completion){
				//	case RockState.Indeterminate:	return name;
				//	case RockState.AtRisk:			return name + " is marked 'Off Track'";
				//	case RockState.OnTrack:			return name + " is marked 'On Track'";
				//	case RockState.Complete:		return name + " is marked 'Done'";
				//	default:throw new ArgumentOutOfRangeException();
				//}
			}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
			public virtual async Task<string> GetIssueDetails()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
			{
                var marked = "";
                switch (Completion) {
                    case RockState.AtRisk: marked ="\nMarked: 'Off Track'"; break;
                    case RockState.OnTrack: marked = "\nMarked: 'On Track'"; break;
                    case RockState.Complete: marked = "\nMarked: 'Done'"; break;
                }

                var week = L10Meeting.CreateTime.StartOfWeek(DayOfWeek.Sunday).ToString("d");
				var accountable = ForRock.AccountableUser.GetName();
				var footer = "Week:" + week + "\nOwner: " + accountable;
                footer += marked;

                return footer;
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
			public virtual int _Ordering { get; set; }
			public virtual bool IsDivider { get; set; }
			public L10Meeting_Measurable()
			{

			}
			public class L10Meeting_MeasurableMap : ClassMap<L10Meeting_Measurable>
			{
				public L10Meeting_MeasurableMap()
				{
					Id(x => x.Id);
					Map(x => x.DeleteTime);
					Map(x => x._Ordering);
					Map(x => x.IsDivider);
					References(x => x.Measurable).Column("MeasurableId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");
				}
			}

			public virtual bool _WasModified { get; set; }
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
			public virtual decimal? Rating { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
            public virtual bool SeenTodoFireworks { get; set; }

			public class L10MeetingAttendeeMap : ClassMap<L10Meeting_Attendee>
			{
				public L10MeetingAttendeeMap()
				{
					Id(x => x.Id);
                    Map(x => x.Rating);
                    Map(x => x.DeleteTime);
                    Map(x => x.SeenTodoFireworks);
					References(x => x.User).Column("UserId");//.Not.LazyLoad().ReadOnly();
					References(x => x.L10Meeting).Column("L10MeetingId");//.LazyLoad().ReadOnly();
					//Map(x => x.UserId).Column("UserId");
					//Map(x => x.L10MeetingId).Column("L10MeetingId");
				}
			}
		}		
	}
}