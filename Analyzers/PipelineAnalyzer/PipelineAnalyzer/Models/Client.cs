using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PipelineAnalyzer.Models {

	public enum Status {
		Demo,
		Trial,
		Walkthrough,
		Paying,
		Lost,
		Closed,



	}

	public class Client {
		public string Name { get; set; }
		public DateTime CreateTime { get; set; }
	}
}
