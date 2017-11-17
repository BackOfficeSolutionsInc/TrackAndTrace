using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Areas.CoreProcess.Interfaces
{
    public interface ITaskAccessor
    {
        IEnumerable<ITask> GetAllTasks(UserOrganizationModel caller);
        ITask GetTaskById(UserOrganizationModel caller, string taskId);
        ITask GetTaskByProcessDefId(UserOrganizationModel caller, string processDefId);
        bool CompleteTask(UserOrganizationModel caller, string taskId);

        //IEnumerable<ITask> GetTaskList(UserOrganizationModel caller);
    }
}
