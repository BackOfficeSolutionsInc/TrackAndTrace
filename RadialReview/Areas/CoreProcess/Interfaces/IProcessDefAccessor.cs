using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Interfaces
{
    public interface IProcessDefAccessor
    {
        string Deploy(UserOrganizationModel caller, string key);
        IProcessDef GetProcessDefByKey(UserOrganizationModel caller, string key);
        IEnumerable<IProcessDef> GetAllProcessDef(UserOrganizationModel caller); // get all
        IProcessDef GetProcessDefById(UserOrganizationModel caller, string processDefId); // get by id
    }
}
