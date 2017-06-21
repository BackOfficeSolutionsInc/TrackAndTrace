using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Models.Interfaces
{
    public interface ITask
    {
        string GetId();
        string GetName();
        string GetDescription();
        string GetDue();
        string GetOwner();
        string GetprocessDefinitionId();
        string GetProcessInstanceId();
        string GetAssignee();

    }
}
