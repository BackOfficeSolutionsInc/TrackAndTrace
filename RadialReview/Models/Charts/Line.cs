using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Script.Serialization;

namespace RadialReview.Models.Charts {
	public class Line {
		public long start { get; set; }
		public long end { get; set; }
		[ScriptIgnore]
		public List<LineChart> charts { get; set; }

		public long? marginTop { get; set; }
		public long? marginRight { get; set; }
		public long? marginBottom { get; set; }
		public long? marginLeft { get; set; }

		//public List<List<LinePoint>> values {
		//    get {
		//        return charts.Select(x => x.values).ToList();
		//    }
		//}
		//public List<string> names { get { return charts.Select(x => x.name).ToList(); } }
		//public List<List<double>> values { get; set; }
		//public List<string> displayNames { get; set; }
		//public List<string> colors { get; set; }
		//public string scale { get; set; }

		public List<LinePoint> values { get; set; }

		public Line() {
			charts = new List<LineChart>();
		}

		public class LineChart {
			public bool rounding { get; set; }
			public string color { get; set; }
			public string axis { get; set; }
			public string name { get; set; }
			public string displayName { get; set; }
			public List<LinePoint> values { get; set; }

			public LineChart() {
				values = new List<LinePoint>();
			}
		}

		public class LinePoint {
			public long time { get; set; }
			public decimal? value { get; set; }

		}

	}
}
