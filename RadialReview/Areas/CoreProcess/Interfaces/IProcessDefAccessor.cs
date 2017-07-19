using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
using RadialReview.Areas.CoreProcess.Models.Process;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Interfaces
{
    public interface IProcessDefAccessor
    {
        Task<bool> Deploy(UserOrganizationModel caller, long localId);
        Task<IProcessDef> GetProcessDefByKey(UserOrganizationModel caller, string key);
        IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller); // get all
        IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId); // get by id
        List<ProcessInstanceViewModel> GetProcessInstanceList(UserOrganizationModel caller, long localId);
        Task<long> Create(UserOrganizationModel caller, string processName);
        Task<bool> Edit(UserOrganizationModel caller, long localId, string processName);
        Task<bool> Delete(UserOrganizationModel caller, long processId);
        Stream CreateBpmnFile(string processName, string bpmnId);
        System.Threading.Tasks.Task UploadFileToServer(Stream stream, string path);
        Task<Stream> GetFileFromServer(string keyName);
        System.Threading.Tasks.Task DeleteFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller, long orgId);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
        Task<TaskViewModel> CreateTask(UserOrganizationModel caller, long localId, TaskViewModel model);
        Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, long localId, TaskViewModel model);
        Task<bool> DeleteTask(UserOrganizationModel caller, string taskId, long localId);
        Task<List<TaskViewModel>> GetAllTask(UserOrganizationModel caller, long localId);
        Task<List<TaskViewModel>> GetAllTaskByTeamId(UserOrganizationModel caller, long teamId);
        Task<bool> ModifiyBpmnFile(UserOrganizationModel caller, long localId, int oldOrder, int newOrder);
        Task<bool> ProcessSuspend(UserOrganizationModel caller, long localId, bool isSuspend);
        Task<ProcessDef_Camunda> ProcessStart(UserOrganizationModel caller, long processId);
        Task<List<TaskViewModel>> GetTaskListByProcessDefId(UserOrganizationModel caller, List<string> processDefId);
    }
}
