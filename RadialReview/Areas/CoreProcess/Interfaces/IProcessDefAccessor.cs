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
        string Deploy(UserOrganizationModel caller, string key, List<object> files);
        IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key);
        IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller); // get all
        IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId); // get by id
        long Create(UserOrganizationModel caller, string processName);
        bool Edit(UserOrganizationModel caller, string localId, string processName);
        Stream CreateBpmnFile(string processName, string localId);
        void UploadCamundaFile(Stream stream, string path);
        Stream GetCamundaFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
        bool CreateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        bool UpdateTask(UserOrganizationModel caller, string localId, TaskViewModel model);
        List<TaskViewModel> GetAllTask(UserOrganizationModel caller, string localId);
        bool ModifiyBpmnFile(UserOrganizationModel caller, string localId, string oldOrder, string newOrder);
    }
}
