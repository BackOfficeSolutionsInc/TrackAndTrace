using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.Synchronize
{
	public class Sync : ILongIdentifiable, IHistorical
	{
		public virtual long Id { get; set; }
		public virtual long UserId { get; set; }
		public virtual long Timestamp { get; set; }
		public virtual String Action { get; set; }
		public virtual DateTime CreateTime { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public Sync(){
			CreateTime = DateTime.UtcNow;
		}

		public class SyncMap : ClassMap<Sync>
		{
			public SyncMap()
			{
				Id(x => x.Id);
				Map(x => x.Timestamp);
				Map(x => x.UserId);
				Map(x => x.Action);
				Map(x => x.CreateTime);
				Map(x => x.DeleteTime);
			}
		}
	}
}