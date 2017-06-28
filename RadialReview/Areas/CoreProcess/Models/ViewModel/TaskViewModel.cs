using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
	public class TaskViewModel {
		public TaskViewModel() {
			id = Guid.NewGuid();
		}

		public Guid id { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public ProcessViewModel process { get; set; }
	}
}
