using System.Diagnostics;
using System.Threading.Tasks;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.L10;

namespace RadialReview.Models.Todo
{
    public enum TodoType {
        Recurrence = 0,
        Personal = 1,
    }


	[DebuggerDisplay("Message = {Message}, Owner={AccountableUser}")]
	public class TodoModel : IDeletable, ILongIdentifiable, IIssue
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime DueDate { get; set; }
        /// <summary>
        /// When did the todo get completed?
        /// </summary>
		public virtual DateTime? CompleteTime { get; set; }
        /// <summary>
        /// When did the todo get closed out during a meeting
        /// </summary>
        public virtual DateTime? CloseTime { get; set; }
		public virtual String Message { get; set; }
		public virtual String Details { get; set; }
		public virtual long CreatedById { get; set; }
        public virtual UserOrganizationModel CreatedBy { get; set; }
        public virtual long? CreatedDuringMeetingId { get; set; }
        public virtual long? CompleteDuringMeetingId { get; set; }
        public virtual L10Meeting CreatedDuringMeeting { get; set; }
        //public virtual L10Meeting CompleteDuringMeeting { get; set; }
		public virtual long? ForRecurrenceId { get; set; }
		public virtual L10Recurrence ForRecurrence { get; set; }
		public virtual long AccountableUserId { get; set; }
		public virtual UserOrganizationModel AccountableUser { get; set; }
		public virtual long Ordering { get; set; }

		public virtual String ForModel { get; set; }
		public virtual long ForModelId { get; set; } // -2 if from text message.
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public virtual String PadId { get; set; }

        public virtual TodoType TodoType { get; set; }
        public virtual long? ClearedInMeeting { get; set; }


		public virtual async Task<string> GetPadId(bool editable) {
			return editable ? PadId : _ReadOnlyPadId;
		}		
		public virtual string _ReadOnlyPadId { get; set; }
		public virtual async Task<string> ResolveReadOnlyPadId() {
			_ReadOnlyPadId = _ReadOnlyPadId ?? await PadAccessor.GetReadonlyPad(PadId);
			return _ReadOnlyPadId;
		}

		public TodoModel()
		{
			CreateTime = DateTime.UtcNow;
			DueDate = CreateTime.AddDays(7);
            Ordering = -CreateTime.Ticks;
            PadId = Guid.NewGuid().ToString();
            TodoType = TodoType.Recurrence;
		}

		public class IssueMap : ClassMap<TodoModel>
		{
			public IssueMap()
			{
				Id(x => x.Id);
                Map(x => x.Ordering);

                Map(x => x.TodoType).CustomType<TodoType>();

                Map(x => x.CloseTime);
                Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.CompleteTime);
				Map(x => x.DueDate);
                Map(x => x.ClearedInMeeting);

				Map(x => x.ForModel);
				Map(x => x.ForModelId);

				Map(x => x.PadId);

				Map(x => x.Message).Length(10000);
				Map(x => x.Details).Length(10000);

				Map(x => x.CreatedById).Column("CreatedById");
				References(x => x.CreatedBy).Column("CreatedById").LazyLoad().ReadOnly();

				Map(x => x.AccountableUserId).Column("AccountableUserId");
				References(x => x.AccountableUser).Column("AccountableUserId").LazyLoad().ReadOnly();

				Map(x => x.CreatedDuringMeetingId).Column("CreatedDuringMeetingId");
                References(x => x.CreatedDuringMeeting).Column("CreatedDuringMeetingId").Nullable().LazyLoad().ReadOnly();

                Map(x => x.CompleteDuringMeetingId).Column("CompleteDuringMeetingId");
                //References(x => x.CompleteDuringMeeting).Column("CompleteDuringMeetingId").Nullable().LazyLoad().ReadOnly();

				Map(x => x.ForRecurrenceId).Column("ForRecurrenceId");
				References(x => x.ForRecurrence).Column("ForRecurrenceId").Nullable().LazyLoad().ReadOnly();

				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();



				//Map(x => x.MeetingRecurrenceId).Column("RecurrenceId");
				//References(x => x.MeetingRecurrence).Column("RecurrenceId").LazyLoad().ReadOnly();

			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task<string> GetIssueMessage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (CompleteTime == null)
                return "Incomplete: '" + Message + "'";//"Todo was not completed";
            return "'" + Message + "'";//"Todo was completed";

            //if (CreatedDuringMeeting != null){
            //    if (CreatedDuringMeeting.StartTime.HasValue && !CreatedDuringMeeting.CompleteTime.HasValue && CreateTime > CreatedDuringMeeting.StartTime.Value && CompleteTime == null){
            //        //Created during meeting
            //        return "";
            //    }
            //    if (CreatedDuringMeeting.StartTime.HasValue && CreateTime < CreatedDuringMeeting.StartTime.Value && CompleteTime == null){
            //        //Incomplete from previous meeting
            //        return "Incomplete: '" + Message + "'";//"Todo was not completed";
            //        //return "Todo was not completed";
            //    }
            //}
            //else{
            //    if (CompleteTime == null)
            //        return "Incomplete: '"+Message+"'";//"Todo was not completed";
            //    else
            //        return "";
            //}
            //return "";
		}

		public virtual async Task<string> GetIssueDetails()
		{
			var week = CreateTime.ToString("d");
			var accountable = AccountableUser.GetName();
		
			var footer = "Week: " + week + "\nOwner: " + accountable;

			var padDetails =await PadAccessor.GetText(PadId);

			return "MESSAGE: " + Message + "\nDETAILS: " + padDetails + "\n\n" + footer;

		}
	}
}