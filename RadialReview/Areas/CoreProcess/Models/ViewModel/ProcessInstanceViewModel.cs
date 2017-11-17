using CamundaCSharpClient.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RadialReview.Areas.CoreProcess.Models.MapModel;

namespace RadialReview.Areas.CoreProcess.Models.Process {
    public class ProcessInstanceViewModel : CamundaBase
    {		
		public string Id { get; set; }
        public long DefinitionId { get; set; }
        public string BusinessKey { get; set; }
        public object CaseInstanceId { get; set; }
        public bool Ended { get; set; }
        public bool Suspended { get; set; }
        public object Links { get; set; }

		public DateTime? CreateTime { get; set; }
		public DateTime? CompleteTime { get; set; }


		//public string suspend { get; set; }
		public List<string> Process { get; set; }

        public static ProcessInstanceViewModel Create(ProcessInstance_Camunda x) {
            return new ProcessInstanceViewModel() {
                Id = x.CamundaProcessInstanceId,
                DefinitionId = x.LocalProcessInstanceId,
                Suspended = x.Suspended,
                CreateTime = x.CreateTime,
                CompleteTime = x.CompleteTime
            };
        }
    }
}
