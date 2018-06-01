using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Payments {
	public class PaymentCredit : ILongIdentifiable, IHistorical {
		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual string Message { get; set; }
		public virtual decimal OriginalAmount { get; set; }
		public virtual decimal AmountRemaining { get; set; }
		public virtual long OrgId { get; set; }

		public virtual long CreatedBy { get; set; }


		public PaymentCredit() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<PaymentCredit> {
			public Map() {
				Id(x => x.Id);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
				Map(x => x.Message);
				Map(x => x.OriginalAmount);
				Map(x => x.AmountRemaining);
				Map(x => x.CreatedBy);
				Map(x => x.OrgId);
			}
		}
	}
}