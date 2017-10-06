using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Utilities;

namespace RadialReview.Hooks.Meeting {
	public class AuditLogHooks : ITodoHook {
		public bool CanRunRemotely() {
			return true;
		}

		public async Task CreateTodo(ISession s, TodoModel todo) {
			if (todo.ForRecurrenceId > 0) {
				Audit.L10Log(s, todo.CreatedBy, todo.ForRecurrenceId.Value, "CreateTodo", ForModel.Create(todo), todo.NotNull(x => x.Message));
			}
		}

		public async Task UpdateTodo(ISession s, TodoModel todo, ITodoHookUpdates updates) {
			//Do nothing
		}
	}
}