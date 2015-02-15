using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
/*
namespace RadialReview.Models.L10
{
	public abstract class L10MeetingItem : ILongIdentifiable
	{
		public virtual long Id { get; set; }
		public virtual L10Meeting L10Meeting { get; set; }
		public virtual String Text { get; set; }

		public class L10Meeting_ConnectionMap : ClassMap<L10MeetingItem>
		{
			public L10Meeting_ConnectionMap()
			{
				Id(x => x.Id);
				Map(x => x.Text).Length(50000);
				References(x => x.L10Meeting).Column("L10MeetingId");
			}
		}
	}
}*/