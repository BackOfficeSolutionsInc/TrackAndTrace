using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using RadialReview.Models.L10;

namespace RadialReview.Models.Issues
{
	public class IssueModel : ILongIdentifiable,IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? CloseTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Message { get; set; }
		public virtual string Description { get; set; }
		public virtual long CreatedById { get; set; }
		public virtual UserOrganizationModel CreatedBy { get; set; }
		public virtual long? CreatedDuringMeetingId { get; set; }
		public virtual L10Meeting CreatedDuringMeeting { get; set; }
		public virtual List<L10Recurrence> _MeetingRecurrences { get; set; }

		public virtual String ForModel { get; set; }
		public virtual long ForModelId { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }

		public IssueModel()
		{
			CreateTime = DateTime.UtcNow;
		}

		public class IssueMap : ClassMap<IssueModel>
		{
			public IssueMap()
			{
				Id(x => x.Id);
				Map(x => x.CloseTime);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				
				Map(x => x.ForModel);
				Map(x => x.ForModelId);

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


		public class IssueModel_Recurrence : ILongIdentifiable, IDeletable
		{
			public virtual long Id { get; set; }
			public virtual DateTime CreateTime { get; set; }
			public virtual DateTime? DeleteTime { get; set; }
			public virtual UserOrganizationModel CreatedBy { get; set; }
			public virtual IssueModel_Recurrence CopiedFrom { get; set; }
			public virtual IssueModel Issue { get; set; }
			public virtual L10Recurrence Recurrence { get; set; }

			public class IssueModel_RecurrenceMap : ClassMap<IssueModel_Recurrence>
			{
				public IssueModel_RecurrenceMap()
				{
					Id(x => x.Id);
					Map(x => x.CreateTime);
					Map(x => x.DeleteTime);
					References(x => x.CreatedBy).Column("CreatedById");
					References(x => x.CopiedFrom).Column("CopiedFromId").Nullable();
					References(x => x.Issue).Column("IssueId");
					References(x => x.Recurrence).Column("RecurrenceId");
				}
			}

		}

	}
}