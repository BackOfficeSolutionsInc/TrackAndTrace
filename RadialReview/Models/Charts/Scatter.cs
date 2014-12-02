using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Models.Charts
{
	public class Scatter
	{
		public class ScatterPoint
		{
			public decimal cx { get; set; }
			public decimal cy { get; set; }
			public DateTime date { get; set; }
			public string title { get; set; }
			public string subtitle { get; set; }
			public string xAxis { get; set; }
			public string yAxis { get; set; }
			public string imageUrl { get; set; }
			public string @class { get; set; }
			public string id { get; set; }
		}

		public Checktree FilterTree { get; set; }
		public List<ScatterPoint> OrderedPoints { get; set; }
		public List<ScatterPoint> Points { get; set; }

		public string xAxis { get; set; }
		public string yAxis { get; set; }

		public decimal xMin { get; set; }
		public decimal xMax { get; set; }
		public decimal yMin { get; set; }
		public decimal yMax { get; set; }
		public string title { get; set; }

		public Scatter()
		{
			xMin = -100;
			yMin = -100;
			xMax = 100;
			yMax = 100;
			xAxis = "x";
			yAxis = "y";

		}
	}
}