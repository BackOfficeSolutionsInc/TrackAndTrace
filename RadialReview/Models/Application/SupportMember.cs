using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Application {
	public class SupportMember : IHistorical, ILongIdentifiable {
		public virtual long Id { get; set; }
		public virtual long UserOrgId { get; set; }

		public virtual UserOrganizationModel User { get; set; }
		
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public SupportMember() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<SupportMember> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.UserOrgId).Column("UserOrgId");
				References(x => x.User).Column("UserOrgId").Not.LazyLoad().ReadOnly();
			}
		}
	}
}