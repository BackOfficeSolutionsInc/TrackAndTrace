using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using System.Threading.Tasks;

namespace RadialReview.Hooks {
    public class Tasks_RealTime : ITaskHook {
        public Task ClaimTask(ISession s) {
            throw new NotImplementedException();
        }

        public Task CompleteTask(ISession s) {
            throw new NotImplementedException();
        }

        public Task SyncTask(ISession s) {
            throw new NotImplementedException();
        }

        public Task UnclaimTask(ISession s) {
            throw new NotImplementedException();
        }
    }
}