using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Models.Tasks;
using FluentNHibernate.Mapping;

namespace RadialReview.Models.Scheduler
{
	public class EventModel : ScheduledTask
	{
		public virtual long OrganizationId { get; set; }
		public virtual OrganizationModel Organization { get; set; }
		public virtual DateTime StartTime { get; set; }
		public virtual DateTime EndTime { get; set; }
		public virtual String Name { get; set; }
		public virtual String Description { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual RecurrenceModel Recurrence { get; set; }

		public class EventMap : SubclassMap<EventModel>
		{
			public EventMap()
			{
				Map(x => x.OrganizationId).Column("OrganizationId");
				References(x => x.Organization).Column("OrganizationId").LazyLoad().ReadOnly();
				Map(x => x.StartTime);
				Map(x => x.EndTime);
				Map(x => x.Name);
				Map(x => x.Description);
				Map(x => x.RecurrenceId).Column("RecurrenceId");
				References(x => x.Recurrence).Column("RecurrenceId").LazyLoad().ReadOnly();
			}
		}

	}
}