using RadialReview.Models.Angular.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Areas.CoreProcess.Models.MapModel;

namespace RadialReview.Models.Angular.CoreProcess {
    public class AngularCoreProcess : BaseAngular{
        public AngularCoreProcess(long id) : base(id){
        }
        public AngularCoreProcess() {
        }

        public string Name { get; set; }

        public static AngularCoreProcess Create(ProcessDef_Camunda x) {
            return new AngularCoreProcess(x.LocalId) {
                Name=x.ProcessDefKey,                
            };
        }
    }
}