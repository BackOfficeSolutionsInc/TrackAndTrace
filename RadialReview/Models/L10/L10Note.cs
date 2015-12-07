using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;
using RadialReview.Models.Interfaces;

namespace RadialReview.Models.L10
{
	public class L10Note: ILongIdentifiable, IDeletable
	{
		public virtual long Id { get; set; }
		public virtual DateTime? DeleteTime { get; set; }

		public virtual String Name { get; set; }
		public virtual L10Recurrence Recurrence { get; set; }
		public virtual String Contents { get; set; }
		public virtual String PadId { get; set; }

		public L10Note()
		{
			PadId = Guid.NewGuid().ToString();
		}

		public class L10NoteMap : ClassMap<L10Note>
		{
			public L10NoteMap()
			{
				Id(x => x.Id);
				Map(x => x.Contents).Length(10000);
				Map(x => x.Name);
				Map(x => x.PadId);
				Map(x => x.DeleteTime);
				References(x => x.Recurrence).Column("RecurrenceId");
			}
		}

	}
}