using RadialReview.Areas.CoreProcess.CamundaComm;
using RadialReview.Areas.CoreProcess.Interfaces;
using RadialReview.Areas.CoreProcess.Models.Interfaces;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Areas.CoreProcess.Accessors {
    public class TaskAccessor : ITaskAccessor {

        ICommClass commClass = CommFactory.Get();
        public bool CompleteTask(UserOrganizationModel caller, string taskId) {
			throw new NotImplementedException();
		}
		public IEnumerable<ITask> GetAllTasks(UserOrganizationModel caller)
        {
            //return commClass.GetTaskList();
            throw new NotImplementedException();
        }
		public ITask GetTaskById(UserOrganizationModel caller, string taskId) {
			throw new NotImplementedException();
		}
		public ITask GetTaskByProcessDefId(UserOrganizationModel caller, string processDefId) {
			throw new NotImplementedException();
		}
	}
}