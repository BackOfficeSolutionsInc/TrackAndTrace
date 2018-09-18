using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ApiDesign {
	public static class Const {
		public static class API {
			public const int V1 = 1;
			public const int V2 = 2;

			public static readonly IEnumerable<int> Versions = new[] { V1, V2 };
		}
	}
}