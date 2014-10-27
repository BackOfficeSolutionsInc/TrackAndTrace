using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.UserModels {
	public class RoleModel : ILongIdentifiable, IDeletable{
		public virtual long Id { get; set; }
		public virtual long ForUserId { get; set; }
		public virtual String Role { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public class RMMap : ClassMap<RoleModel>
		{
			public RMMap()
			{
				Id(x => x.Id);
				Map(x => x.ForUserId);
				Map(x => x.Role);
				Map(x => x.DeleteTime);
			}
		}
	}
}