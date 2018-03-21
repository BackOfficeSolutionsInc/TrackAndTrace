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
using RadialReview.Models.Askables;
using RadialReview.Models.Issues;
using RadialReview.Models.Scorecard;

namespace RadialReview.Hooks.Meeting {
	public class DepristineHooks : ITodoHook, IMeetingRockHook, IIssueHook, IMeetingMeasurableHook {
		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
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

		public async Task CreateIssue(ISession s, IssueModel.IssueModel_Recurrence issue) {
			await _Deprestine(s, issue.CreatedBy, issue.Recurrence.Id);
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			var r = s.Get<L10Recurrence>(recurRock.L10Recurrence.Id);
			await _Deprestine(s, s.Get<UserOrganizationModel>(r.CreatedById), r.Id);
		}

		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			await _Deprestine(s, caller, recurMeasurable.L10Recurrence.Id);
		}


		#region No-ops
		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {
			//Do nothing
		}

		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId) {
			//Do nothing
		}

		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			//Do nothing
		}

		public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issue, IIssueHookUpdates updates) {
			//Do nothing
		}
		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			//Do nothing
		}

		#endregion
	}
}