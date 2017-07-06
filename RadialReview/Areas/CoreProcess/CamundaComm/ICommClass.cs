using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.ProcessInstance;
using CamundaCSharpClient.Model.Task;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public interface ICommClass {
		IProcessDef GetProcessDefByKey(string key);
		processInstanceModel ProcessStart(string id);
		NoContentStatus ProcessSuspend(string id, bool isSuspend);
		IEnumerable<TaskModel> GetTaskList(string processDefId);
		IEnumerable<TaskModel> GetTaskList(List<string> processDefId);
		int GetProcessInstanceCount(string processDefId);
		IEnumerable<IProcessInstance> GetProcessInstanceList(string processDefId);
	}
}