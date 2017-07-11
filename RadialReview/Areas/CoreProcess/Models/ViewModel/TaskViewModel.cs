using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
	public class TaskViewModel {
		
		public string Id { get; set; }
        [Required(ErrorMessage = "field is required")]
        public string name { get; set; }
		public string description { get; set; }
		public ProcessViewModel process { get; set; }

        public long[] SelectedMemberId { get; set; }

        public string SelectedMemberName { get; set; }
        public string SelectedIds { get; set; }

        public List<CandidateGroupViewModel> CandidateList { get; set; }


    }

   public class CandidateGroupViewModel
    {
        public long Id { get; set; }
        public string Name { get; set; }
    }
}
