using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
	public class ProcessViewModel {
		public long Id { get; set; }
        
        [Required(ErrorMessage = "field is required")]
        public string Name { get; set; }
        public string LocalID { get; set; }
        public string Description { get; set; }
        public string status { get; set; }
        public bool IsStarted { get; set; }
        public string Action { get; set; }
        public string CamundaId { get; set; }
        public string Count { get; set; }
        
        public List<TaskViewModel> taskList { get; set; }

	}
}
