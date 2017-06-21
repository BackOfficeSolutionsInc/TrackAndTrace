using RadialReview.Areas.CoreProcess.Models.Interfaces;
using System.Collections;
using System.Collections.Generic;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public interface ICommClass {
		IProcessDef GetProcessDefByKey(string key);
        IEnumerable<ITask> GetTaskList();

    }
}