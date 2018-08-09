using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.UserModels {
	public class ConsentModel {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual string UserId { get; set; }
		public virtual DateTime? ConsentTime { get; set; }
		public virtual DateTime? DenyTime { get; set; }

		public ConsentModel() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<ConsentModel> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.UserId).Index("idx__ConsentModel_UserId").Length(256);
				Map(x => x.ConsentTime);
				Map(x => x.DenyTime);
			}

		}
	}
}