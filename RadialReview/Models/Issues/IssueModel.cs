using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Accessors;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Issues {
	public class IssueModel : ILongIdentifiable, IDeletable, ITodo {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		//public virtual DateTime? CloseTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Message { get; set; }
		public virtual string Description { get; set; }
		public virtual string PadId { get; set; }
		public virtual long CreatedById { get; set; }
		public virtual UserOrganizationModel CreatedBy { get; set; }
		public virtual long? CreatedDuringMeetingId { get; set; }
		public virtual L10Meeting CreatedDuringMeeting { get; set; }
		public virtual List<L10Recurrence> _MeetingRecurrences { get; set; }
		public virtual long? _Order { get; set; }
		public virtual int _Priority { get; set; }

		public virtual String ForModel { get; set; }
		public virtual long ForModelId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual int _Rank { get; set; }

		//public virtual long _RecurrenceIssueId { get; set; }
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public virtual async Task<string> GetTodoMessage()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return "";
		}

		public virtual async Task<string> GetTodoDetails() {

			var padDetails = await PadAccessor.GetText(PadId);

			var header = "RESOLVE ISSUE: " + Message;
			if (!String.IsNullOrWhiteSpace(padDetails)) {
				header += "\n\n" + padDetails;
			}
			return header;
		}


		public IssueModel() {
			CreateTime = DateTime.UtcNow;
			_Order = -CreateTime.Ticks;
			PadId = Guid.NewGuid().ToString();
		}

		public class IssueMap : ClassMap<IssueModel> {
			public IssueMap() {
				Id(x => x.Id);
				//Map(x => x.CloseTime);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.ForModel);
				Map(x => x.ForModelId);

				Map(x => x.PadId);

				Map(x => x.Message).Length(10000);
				Map(x => x.Description).Length(10000);
				Map(x => x.CreatedById).Column("CreatedById");
				References(x => x.CreatedBy).Column("CreatedById").LazyLoad().ReadOnly();
				Map(x => x.CreatedDuringMeetingId).Column("CreatedDuringMeetingId");
				References(x => x.CreatedDuringMeeting).Column("CreatedDuringMeetingId").Nullable().LazyLoad().ReadOnly();

				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();



				//Map(x => x.MeetingRecurrenceId).Column("RecurrenceId");
				//References(x => x.MeetingRecurrence).Column("RecurrenceId").LazyLoad().ReadOnly();

			}
		}


		public class IssueModel_Recurrence : ILongIdentifiable, IDeletable {
			public virtual long Id { get; set; }
			public virtual int Priority { get; set; }
			public virtual DateTime LastUpdate_Priority { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual DateTime? CloseTime { get; set; }

			//public virtual string BackupOwner { get; set; }

			public virtual UserOrganizationModel Owner { get; set; }
			public virtual UserOrganizationModel CreatedBy { get; set; }
			public virtual IssueModel_Recurrence CopiedFrom { get; set; }
			public virtual IssueModel Issue { get; set; }
			public virtual L10Recurrence Recurrence { get; set; }
			public virtual List<IssueModel.IssueModel_Recurrence> _ChildIssues { get; set; }
			public virtual IssueModel_Recurrence ParentRecurrenceIssue { get; set; }
			public virtual long? Ordering { get; set; }

			public virtual int Rank { get; set; }
			public virtual bool AwaitingSolve { get; set; }
			public virtual bool MarkedForClose { get; set; }

			public IssueModel_Recurrence() {
				CreateTime = DateTime.UtcNow;
				Ordering = -CreateTime.Ticks;
			}

			public class IssueModel_RecurrenceMap : ClassMap<IssueModel_Recurrence> {
				public IssueModel_RecurrenceMap() {
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					Map(x => x.LastUpdate_Priority);
					Map(x => x.CloseTime);
					Map(x => x.Priority);
					Map(x => x.Ordering);
					Map(x => x.Rank);

					//Map(x => x.BackupOwner);

					Map(x => x.AwaitingSolve);
					Map(x => x.MarkedForClose);


					References(x => x.CreatedBy).Column("CreatedById");
					References(x => x.CopiedFrom).Column("CopiedFromId").Nullable();
					References(x => x.Owner).Column("OwnerId").Nullable();

					References(x => x.Issue).Column("IssueId");
					References(x => x.Recurrence).Column("RecurrenceId");

					//Map(x => x.ParentIssueId).Column("ParentIssueId");
					References(x => x.ParentRecurrenceIssue).Column("ParentRecurrenceIssueId").Nullable();
				}
			}


		}

	}
}