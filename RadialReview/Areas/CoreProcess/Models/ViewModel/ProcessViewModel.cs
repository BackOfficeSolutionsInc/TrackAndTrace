using RadialReview.Areas.CoreProcess.Models.MapModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
	public class ProcessViewModel {
		public ProcessViewModel() {
		}
		public ProcessViewModel(ProcessDef_Camunda item, int instanceCount) {
			Id = item.Id;
			Name = item.ProcessDefKey;
			//process.IsStarted = status;
			if (item.CamundaId == null) {
				Count = 0;
				status = "<div style='color:red'><i class='fa fa-2x fa-times-circle'></i></ div>";
			} else {
				//process.Count = "1";				
				Count = instanceCount;
				status = "<div style='color:green'><i class='fa fa-2x fa-check-circle'></i></ div>";
			}

			LocalID = item.LocalId;
			Id = item.Id;
			//Name = name;
			//LocalID = localID;
			//Description = description;
			//this.status = status;
			//IsStarted = isStarted;
			//Action = action;
			//Count = count;
		}

		public long Id { get; set; }

		[Required(ErrorMessage = "field is required")]
		public string Name { get; set; }
		public string LocalID { get; set; }
		public string Description { get; set; }
		public string status { get; set; }
		public bool IsStarted { get; set; }
		public string Action { get; set; }
		//public string CamundaId { get; set; }
		public int Count { get; set; }

		public List<TaskViewModel> taskList { get; set; }

	}
}
