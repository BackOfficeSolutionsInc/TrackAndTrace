using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Accessors
{
    public class ProcessDefAccessor : IProcessDefAccessor
    {
        public string Deploy(UserOrganizationModel caller, string key, List<object> files)
        {
            // call Comm Layer
            CommClass commClass = new CommClass();
            var result = commClass.Deploy(key, files);

            return string.Empty;
        }

        public IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller)
        {
            throw new NotImplementedException();
        }

        public IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId)
        {
            throw new NotImplementedException();
        }

        public IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key)
        {
            throw new NotImplementedException();
        }
    }
}