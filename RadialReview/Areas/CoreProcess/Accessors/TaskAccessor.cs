using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Accessors {
    public class TaskAccessor : ITaskAccessor
    {
        public IEnumerable<ITask> GetAllTasks(UserOrganizationModel caller)
        {
            CommClass commClass = new CommClass();
            return commClass.GetTaskList();
        }
    }
}