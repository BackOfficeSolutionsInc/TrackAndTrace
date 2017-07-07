using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.ProcessInstance;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public interface ICommClass {
		IProcessDef GetProcessDefByKey(string key);
        processInstanceModel ProcessStart(string id);
        NoContentStatus ProcessSuspend(string id, bool isSuspend);
        IEnumerable<ITask> GetTaskList();
        int GetProcessInstanceCount(string processDefId);
        IEnumerable<IProcessInstance> GetProcessInstanceList(string processDefId);


    }
}