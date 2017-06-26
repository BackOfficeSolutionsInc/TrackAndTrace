using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Areas.CoreProcess.Models.MapModel;
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
        bool Create(UserOrganizationModel caller, string processName);
        bool Edit(UserOrganizationModel caller, string processDefId);
        Stream CreateBmpnFile(string processName);
        void UploadCamundaFile(Stream stream, string path);
        void GetCamundaFileFromServer(string keyName);
        IEnumerable<ProcessDef_Camunda> GetList(UserOrganizationModel caller);
        ProcessDef_Camunda GetById(UserOrganizationModel caller, long processId);
    }
}
