using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;

namespace RadialReview.Models.L10
{
	public class L10MeetingAttendee
	{
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual UserOrganizationModel User { get; set; }
		public virtual long L10MeetingId { get; set; }
		public virtual L10Meeting L10Meeting { get; set; }
		public virtual int? Rating { get; set; }

		public class L10MeetingAttendeeMap : ClassMap<L10MeetingAttendee>
		{
			public L10MeetingAttendeeMap()
			{
				Id(x => x.Id);
				Map(x => x.UserId).Column("UserId");
				References(x => x.User).Column("UserId").Not.LazyLoad().ReadOnly();
				Map(x => x.L10MeetingId).Column("L10MeetingId");
				References(x => x.L10Meeting).Column("L10MeetingId").LazyLoad().ReadOnly();
				Map(x => x.Rating);


			}
		}
	}
}