using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CamundaCSharpClient.Model.ProcessDefinition
{
    public class ProcessDefinitionDeleteModel : CamundaBase
    {
        /// <summary>
        ///	true, if all process instances, historic process instances and jobs for this process definition should be deleted.
        /// </summary>
        public bool cascade { get; set; }
        /// <summary>
        /// true, if only the built-in ExecutionListeners should be notified with the end event.
        /// </summary>
        public bool skipCustomListeners { get; set; }
    }
}
