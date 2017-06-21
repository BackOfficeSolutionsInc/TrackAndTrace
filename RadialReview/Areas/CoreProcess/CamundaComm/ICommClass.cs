using RadialReview.Areas.CoreProcess.Models.Interfaces;

namespace RadialReview.Areas.CoreProcess.CamundaComm {
	public interface ICommClass {
		IProcessDef GetProcessDefByKey(string key);
	}
}