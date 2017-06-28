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
        bool Edit(UserOrganizationModel caller, string processDefId);
        Stream CreateBpmnFile(string processName);
        void UploadCamundaFile(Stream stream, string path);
        Stream GetCamundaFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
        bool CreateTask(UserOrganizationModel caller, string processDefId, TaskViewModel model);
    }
}
