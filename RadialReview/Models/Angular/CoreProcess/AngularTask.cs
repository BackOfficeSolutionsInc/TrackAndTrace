using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.CoreProcess.Models.Process;

namespace RadialReview.Models.Angular.CoreProcess {
    public class AngularTask : BaseStringAngular {
        public AngularTask(string id) : base(id){
        }
        public AngularTask() {
        }

        public string Name { get; set; }
        public long? Assignee { get; set; }
        public bool? Assigned { get; set; }
        public DateTime? CompleteTime { get; set; }
        public bool? Complete { get;set;}

        public static AngularTask Create(TaskViewModel x) {
            return new AngularTask(x.Id) {
                Name= x.name,
                Assigned= x.Assignee!=null,
                Assignee = x.Assignee,
                Complete = false,
            };
        }
    }
}