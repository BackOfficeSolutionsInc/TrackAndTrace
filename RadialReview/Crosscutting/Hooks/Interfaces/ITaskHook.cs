using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
	public interface ITaskHook : IHook {
		Task ClaimTask(ISession s, string taskId, long userId);
		Task UnclaimTask(ISession s, string taskId);
		Task CompleteTask(ISession s, string taskId, long userId);
	}
}
