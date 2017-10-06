using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.L10;

namespace RadialReview.Hooks.Meeting {
	public class DepristineHooks : ITodoHook {
		public bool CanRunRemotely() {
			return true;
		}

		private async Task _Deprestine(ISession s, UserOrganizationModel caller, long recurrenceId) {
			var r = s.Get<L10Recurrence>(recurrenceId);
			await L10Accessor.Depristine_Unsafe(s, caller, r);
			s.Update(r);
		}

		public async Task CreateTodo(ISession s, TodoModel todo) {
			if (todo.ForRecurrenceId != null && todo.ForRecurrenceId > 0) {
				await _Deprestine(s, todo.CreatedBy, todo.ForRecurrenceId.Value);
			}
		}

		public async Task UpdateTodo(ISession s, TodoModel todo, ITodoHookUpdates updates) {
			//Do nothing
		}
	}
}