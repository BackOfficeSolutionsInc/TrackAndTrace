using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.People.Angular {

	public class AngularPeopleAnalyzer : BaseAngular {

		public AngularPeopleAnalyzer() { }
		public AngularPeopleAnalyzer(long id) : base(id) {
		}

		public IEnumerable<AngularPeopleAnalyzerRow> Rows { get; set; }
		public IEnumerable<PeopleAnalyzerValue> Values { get; set; }

	}

	public class AngularPeopleAnalyzerRow : BaseAngular {
		public AngularPeopleAnalyzerRow() { }
		public AngularPeopleAnalyzerRow(AngularUser user) : base(user.Id) {
			User = user;
		}
		public AngularUser User { get; set; }
	}

	public class AngularPeopleAnalyzerResponse : BaseAngular {

		public AngularPeopleAnalyzerRow() { }
		public AngularPeopleAnalyzerRow(string id) : base(id) {
			User = user;
		}

	}


	public class AngularPeopleAnalyzerRow : BaseAngular {
		public AngularPeopleAnalyzerRow() { }
		public AngularPeopleAnalyzerRow(long id) : base(id) {
		}
		
		public Dictionary<long,string> Value { get; set; }

		public string Name { get; set; }
		public string Get { get; set; }
		public string Want { get; set; }
		public string Capacity { get; set; }

	}

	public class PeopleAnalyzerValue {
		public long ValueId { get; set; }
		public string Value { get; set; }
		//public string Rating { get; set; }
	}
}