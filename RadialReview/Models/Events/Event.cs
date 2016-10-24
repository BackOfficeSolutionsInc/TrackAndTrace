using FluentNHibernate.Mapping;
using RadialReview.Models.Components;
using RadialReview.Models.Interfaces;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Events {

	public class AccountEvent : ILongIdentifiable, IHistorical{

		public virtual long Id { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }
		public virtual EventType Type { get; set; }
		public virtual long? OrgId { get; set; }

		public virtual long? TriggeredBy { get; set; }

		public virtual ForModel ForModel { get; set; }

		public virtual decimal? Argument1 { get; set; }
		public virtual string Message { get; set; }

		public virtual bool Addressed { get; set; }
		public virtual long? AssignedTo { get; set; }
		public virtual DateTime? AssignTime { get; set; }
		public virtual DateTime? CloseTime { get; set; }
		public virtual bool ShouldAddress { get; set; }

		public AccountEvent() {
			CreateTime = DateTime.UtcNow;
		}

		public class Map : ClassMap<AccountEvent> {
			public Map() {
				Id(x => x.Id);

				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);

				Map(x => x.TriggeredBy);
				
				Map(x => x.Type).CustomType<EventType>();
				Map(x => x.OrgId);
				Map(x => x.Message);
				Map(x => x.Argument1);

				Map(x => x.Addressed);
				Map(x => x.ShouldAddress);
				Map(x => x.AssignedTo);
				Map(x => x.AssignTime);
				Map(x => x.CloseTime);

				Component(x => x.ForModel);
			}
		}
	}
}