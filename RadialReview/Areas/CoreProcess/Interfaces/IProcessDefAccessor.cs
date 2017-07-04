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
        bool Deploy(UserOrganizationModel caller, string localId);
        IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key);
        IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller); // get all
        IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId); // get by id
        long Create(UserOrganizationModel caller, string processName);
        bool Edit(UserOrganizationModel caller, string localId, string processName);
        bool Delete(UserOrganizationModel caller, long processId);
        Stream CreateBpmnFile(string processName, string localId);
        void UploadFileToServer(Stream stream, string path);
        Stream GetFileFromServer(string keyName);
        void DeleteFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
        TaskViewModel CreateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        TaskViewModel UpdateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        bool DeleteTask(UserOrganizationModel caller, string taskId, string localId);
        List<TaskViewModel> GetAllTask(UserOrganizationModel caller, string localId);
        bool ModifiyBpmnFile(UserOrganizationModel caller, string localId, int oldOrder, int newOrder);

    }
}
