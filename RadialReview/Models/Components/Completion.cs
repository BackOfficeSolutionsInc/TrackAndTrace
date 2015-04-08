using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentNHibernate.Mapping;

namespace RadialReview.Models.Components
{
	public class Completion
	{
		public virtual int NumRequiredComplete { get; set; }
		public virtual int NumOptionalComplete { get; set; }
		public virtual int NumRequired { get; set; }
		public virtual int NumOptional { get; set; }

		public class CompletionMap : ComponentMap<Completion>
		{
			public CompletionMap()
			{
				Map(x => x.NumRequired);
				Map(x => x.NumOptional);
				Map(x => x.NumRequiredComplete);
				Map(x => x.NumOptionalComplete);
			}
		}

	}
}