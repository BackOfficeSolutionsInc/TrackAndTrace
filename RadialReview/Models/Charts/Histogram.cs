using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TypeLite;

namespace RadialReview.Models.Charts
{
	public class Histogram
	{
		public class Bin
		{
			public decimal threshold { get; set; }
		}

		public Bin[] bins { get; set; }

		public decimal[] values { get; set; }


	}
}