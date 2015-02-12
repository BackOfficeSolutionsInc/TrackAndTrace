using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;

namespace RadialReview.Models.Charts {
	public class Line
	{
		public long start { get; set; }
		public long end { get; set; }
		public List<LineChart> charts { get; set; }

		public long? marginTop { get; set; }
		public long? marginRight { get; set; }
		public long? marginBottom { get; set; }
		public long? marginLeft { get; set; }


		public Line()
		{
			charts=new List<LineChart>();
		}

		public class LineChart
		{
			public bool rounding { get; set; }
			public string color { get; set; }
			public string axis { get; set; }
			public string name { get; set; }
			public string displayName { get; set; }
			public List<LinePoint> points { get; set; }
		}

		public class LinePoint
		{
			public long time { get; set; }
			public decimal? value { get; set; }

		}

	}
}