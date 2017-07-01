using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {
	public class Coach : IHistorical, ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual String Name { get; set; }
		public virtual long? UserOrgId { get; set; }
		public virtual string Email { get; set; }
		
		public virtual CoachType CoachType { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public Coach() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<Coach> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Name);
				Map(x => x.CoachType);
				Map(x => x.UserOrgId);
				Map(x => x.Email);
			}
		}
	}
}