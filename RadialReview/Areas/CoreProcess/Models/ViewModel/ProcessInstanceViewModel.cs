using CamundaCSharpClient.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Process {
    public class ProcessInstanceViewModel : CamundaBase
    {
        public string Id { get; set; }

        public string DefinitionId { get; set; }

        public string BusinessKey { get; set; }

        public object CaseInstanceId { get; set; }

        public bool Ended { get; set; }

        public bool Suspended { get; set; }

        public object Links { get; set; }
    }
}
