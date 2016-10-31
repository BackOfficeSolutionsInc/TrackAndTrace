using FluentNHibernate.Mapping;
using RadialReview.Models.Enums;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Reviews {
	public class ValueBar : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual long OrganizationId { get; set; }
		public virtual long CompanyValueId { get; set; }
		public virtual PositiveNegativeNeutral Minimum { get; set; }

		public ValueBar() {
			CreateTime = DateTime.UtcNow;
			Minimum = PositiveNegativeNeutral.Neutral;
		}

		public class Map : ClassMap<ValueBar> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Minimum);

				Map(x => x.OrganizationId);
				Map(x => x.CompanyValueId);
			}
		}

	}
}