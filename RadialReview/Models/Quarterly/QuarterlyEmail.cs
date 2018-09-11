using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Quarterly {
	public class QuarterlyEmail : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual string Email { get; set; }
		public virtual long RecurrenceId { get; set; }
		public virtual long SenderId { get; set; }
		public virtual long OrgId { get; set; }
		public virtual DateTime ScheduledTime { get; set; }
		public virtual DateTime? SentTime { get; set; }

		public QuarterlyEmail() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<QuarterlyEmail> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Email);
				Map(x => x.RecurrenceId);
				Map(x => x.ScheduledTime);
				Map(x => x.SentTime);
				Map(x => x.SenderId);
				Map(x => x.OrgId);
			}
		}
	}
}