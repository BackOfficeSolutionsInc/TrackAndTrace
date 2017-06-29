using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process
{
    public class TaskViewModel
    {
        public string Id { get; set; }
        [Required(ErrorMessage = "field is required")]
        public string name { get; set; }
        public string description { get; set; }
        public ProcessViewModel process { get; set; }
    }
}
