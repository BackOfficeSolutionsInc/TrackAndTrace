using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
	public class ProcessViewModel {
		public long Id { get; set; }
		public string Name { get; set; }
		public string Description { get; set; }
		public List<TaskViewModel> taskList { get; set; }

	}
}
