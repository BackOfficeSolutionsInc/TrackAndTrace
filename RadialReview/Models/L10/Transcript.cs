using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.L10
{
	public class Transcript : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual string Text { get; set; }

		public virtual long? RecurrenceId { get; set; }
		public virtual long? MeetingId { get; set; }
		public virtual long UserId { get; set; }

		public virtual UserOrganizationModel _User { get; set; }

		public class MMap : ClassMap<Transcript>
		{
			public MMap()
			{
				Id(x => x.Id);
				Map(x => x.Text);
				Map(x => x.UserId);
				Map(x => x.MeetingId);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.RecurrenceId);
			}
		}
	}
}