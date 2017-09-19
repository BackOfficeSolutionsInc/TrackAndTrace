using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RadialReview.Utilities.Hooks {
    public interface ITaskHook : IHook {
        Task ClaimTask(ISession s);
        Task UnclaimTask(ISession s);
        Task SyncTask(ISession s);
        Task CompleteTask(ISession s);
    }
}
