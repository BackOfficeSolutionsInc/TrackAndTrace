using CamundaCSharpClient.Model;
using CamundaCSharpClient.Model.ProcessInstance;
using CamundaCSharpClient.Model.Task;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public interface ICommClass {
        Task<IProcessDef> GetProcessDefByKey(string key);

        Task<processInstanceModel> ProcessStart(string id);

        Task<NoContentStatus> ProcessSuspend(string id, bool isSuspend);

        Task<IEnumerable<TaskModel>> GetTaskList(string processDefId);

        Task<IEnumerable<TaskModel>> GetTaskList(List<string> processDefId);

        Task<int> GetProcessInstanceCount(string processDefId);

        Task<IEnumerable<IProcessInstance>> GetProcessInstanceList(string processDefId);
        Task<IEnumerable<TaskModel>> GetTaskByCandidateGroup(string candidateGroup);
        Task<IEnumerable<TaskModel>> GetTaskListByInstanceId(string InstanceId);

    }
}