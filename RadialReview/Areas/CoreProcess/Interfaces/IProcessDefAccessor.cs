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
        Task<bool> Deploy(UserOrganizationModel caller, string localId);
        IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key);
        IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller); // get all
        IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId); // get by id
        List<ProcessInstanceViewModel> GetProcessInstanceList(string localId);
        Task<long> Create(UserOrganizationModel caller, string processName);
        Task<bool> Edit(UserOrganizationModel caller, string localId, string processName);
        Task<bool> Delete(UserOrganizationModel caller, long processId);
        Stream CreateBpmnFile(string processName, string localId);
        System.Threading.Tasks.Task UploadFileToServer(Stream stream, string path);
        Task<Stream> GetFileFromServer(string keyName);
        System.Threading.Tasks.Task DeleteFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
        Task<TaskViewModel> CreateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        Task<TaskViewModel> UpdateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        Task<bool> DeleteTask(UserOrganizationModel caller, string taskId, string localId);
        Task<List<TaskViewModel>> GetAllTask(UserOrganizationModel caller, string localId);
        Task<bool> ModifiyBpmnFile(UserOrganizationModel caller, string localId, int oldOrder, int newOrder);
        bool ProcessSuspend(UserOrganizationModel caller, string processInsId, bool isSuspend);
		ProcessDef_Camunda ProcessStart(UserOrganizationModel caller, long processId);
        List<TaskViewModel> GetTaskListByProcessDefId(UserOrganizationModel caller, List<string> processDefId);
    }
}
