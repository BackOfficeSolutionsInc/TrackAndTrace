using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.VideoConference {
	public class JoinedVideo : ILongIdentifiable, IHistorical{
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long UserId { get; set; }
		public virtual long VideoProvider { get; set; }
		public virtual long? RecurrenceId { get; set; }
		public virtual long? MeetingId { get; set; }

		public JoinedVideo() {
			CreateTime = DateTime.UtcNow;
		}
		public class Map : ClassMap<JoinedVideo> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.UserId);
				Map(x => x.VideoProvider);
				Map(x => x.RecurrenceId);
				Map(x => x.MeetingId);
			}
		}
	}
}