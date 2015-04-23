using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;

namespace RadialReview.Models.Todo
{
	public class TodoModel : IDeletable, ILongIdentifiable, IIssue
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime DueDate { get; set; }
		public virtual DateTime? CompleteTime { get; set; }
		public virtual String Message { get; set; }
		public virtual String Details { get; set; }
		public virtual long CreatedById { get; set; }
		public virtual UserOrganizationModel CreatedBy { get; set; }
		public virtual long? CreatedDuringMeetingId { get; set; }
		public virtual L10Meeting CreatedDuringMeeting { get; set; }
		public virtual long? ForRecurrenceId { get; set; }
		public virtual L10Recurrence ForRecurrence { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual int Ordering { get; set; }

		public virtual String ForModel { get; set; }
		public virtual long ForModelId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public TodoModel()
		{
			CreateTime = DateTime.UtcNow;
			DueDate = CreateTime.AddDays(7);
		}

		public class IssueMap : ClassMap<TodoModel>
		{
			public IssueMap()
			{
				Id(x => x.Id);
				Map(x => x.Ordering);
				
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CompleteTime);
				Map(x => x.DueDate);

				Map(x => x.ForModel);
				Map(x => x.ForModelId);

				Map(x => x.Message).Length(10000);
				Map(x => x.Details).Length(10000);

				Map(x => x.CreatedById).Column("CreatedById");
				References(x => x.CreatedBy).Column("CreatedById").LazyLoad().ReadOnly();

				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();

				Map(x => x.CreatedDuringMeetingId).Column("CreatedDuringMeetingId");
				References(x => x.CreatedDuringMeeting).Column("CreatedDuringMeetingId").Nullable().LazyLoad().ReadOnly();

				Map(x => x.ForRecurrenceId).Column("ForRecurrenceId");
				References(x => x.ForRecurrence).Column("ForRecurrenceId").Nullable().LazyLoad().ReadOnly();

				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();



				//Map(x => x.MeetingRecurrenceId).Column("RecurrenceId");
				//References(x => x.MeetingRecurrence).Column("RecurrenceId").LazyLoad().ReadOnly();

			}
		}

		public virtual string GetIssueMessage()
		{
			if (CreatedDuringMeeting != null){
				if (CreatedDuringMeeting.StartTime.HasValue && !CreatedDuringMeeting.CompleteTime.HasValue && CreateTime > CreatedDuringMeeting.StartTime.Value && CompleteTime == null){
					//Created during meeting
					return "";
				}
				if (CreatedDuringMeeting.StartTime.HasValue && CreateTime < CreatedDuringMeeting.StartTime.Value && CompleteTime == null){
					//Incomplete from previous meeting
					return "Todo was not completed";
				}
			}
			else{
				if (CompleteTime == null)
					return "Todo was not completed";
				else
					return "";

			}
			return "";
		}

		public virtual string GetIssueDetails()
		{
			var week = CreateTime.ToString("d");
			var accountable = AccountableUser.GetName();
		
			var footer = "Week of " + week + "\nOwner: " + accountable;


			return "MESSAGE: " + Message + "\nDETAILS: " + Details + "\n\n" + footer;

		}
	}
}