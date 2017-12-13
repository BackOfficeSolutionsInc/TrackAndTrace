using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using RadialReview.Utilities;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using RadialReview.Utilities.Synchronize;
using RadialReview.Hooks.Realtime;
using RadialReview.Hubs;
using Microsoft.AspNet.SignalR;
using RadialReview.Models.Scorecard;

namespace RadialReview.Hooks.Meeting {
	public class AuditLogHooks : ITodoHook, IRockHook, IHeadlineHook, IIssueHook, IMeetingRockHook, IMeetingMeasurableHook, IMeasurableHook {
		public bool CanRunRemotely() {
			return true;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Database;
		}

		public async Task CreateTodo(ISession s, TodoModel todo) {
			if (todo.ForRecurrenceId > 0) {
				Audit.L10Log(s, todo.CreatedBy, todo.ForRecurrenceId.Value, "CreateTodo", ForModel.Create(todo), todo.NotNull(x => x.Message));
			}
		}

		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {
			if (todo.ForRecurrenceId > 0) {
				var updatesText = new List<string>();
				if (updates.MessageChanged) {
					updatesText.Add("Message: " + todo.Message);
				}
				if (updates.DueDateChanged) {
					updatesText.Add("Due-Date: " + todo.DueDate.ToShortDateString());
				}
				if (updates.AccountableUserChanged) {
					updatesText.Add("Accountable: " + todo.AccountableUser.GetName());
				}
				if (updates.CompletionChanged) {
					if (todo.CompleteTime != null) {
						updatesText.Add("Marked Complete");
					} else if (todo.CompleteTime == null) {
						updatesText.Add("Marked Incomplete");
					}
				}
				var updatedText = "Updated To-Do \"" + todo.Message + "\" \n " + String.Join("\n", updatesText);
				Audit.L10Log(s, caller, todo.ForRecurrenceId.Value, "UpdateTodo", ForModel.Create(todo), updatedText);
			}
		}

		public async Task UpdateRock(ISession s, UserOrganizationModel caller, RockModel rock, IRockHookUpdates updates) {
			if (updates.StatusChanged) {
				var recurIds = s.QueryOver<L10Recurrence.L10Recurrence_Rocks>().Where(x => x.ForRock.Id == rock.Id && x.DeleteTime == null).Select(x => x.L10Recurrence.Id).List<long>().ToList();
				if (recurIds.Any()) {
					foreach (var recurrenceId in recurIds) {
						var currentMeeting = L10Accessor._GetCurrentL10Meeting_Unsafe(s, recurrenceId, true, false, false);
						if (currentMeeting != null) {
							var meetingRocks = s.QueryOver<L10Meeting.L10Meeting_Rock>().Where(x => x.L10Meeting.Id == currentMeeting.Id && x.ForRock.Id == rock.Id && x.DeleteTime == null).List().ToList();
							foreach (var r in meetingRocks) {
								Audit.L10Log(s, caller, recurrenceId, "UpdateRockCompletion", ForModel.Create(r), "\"" + r.ForRock.Rock + "\" set to \"" + rock.Completion + "\"");

							}
						}
					}
				}
			}
		}

		public async Task CreateHeadline(ISession s, PeopleHeadline headline) {
			var recurrenceId = headline.RecurrenceId;
			if (recurrenceId > 0) {
				Audit.L10Log(s, s.Get<UserOrganizationModel>(headline.CreatedBy), headline.RecurrenceId, "CreateHeadline", ForModel.Create(headline), headline.NotNull(x => x.Message));
			}
		}

		public async Task CreateIssue(ISession s, IssueModel.IssueModel_Recurrence issue) {
			var recurrenceId = issue.Recurrence.Id;
			if (recurrenceId > 0) {
				Audit.L10Log(s, issue.CreatedBy, recurrenceId, "CreateIssue", ForModel.Create(issue.Issue), issue.Issue.NotNull(x => x.Message));
			}
		}

		public async Task UpdateIssue(ISession s, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence issueRecurrence, IIssueHookUpdates updates) {
			var updatesText = new List<string>();
			var recurrenceId = issueRecurrence.Recurrence.Id;
			var issueRecurrenceId = issueRecurrence.Id;

			if (updates.MessageChanged)
				updatesText.Add("Message: " + issueRecurrence.Issue.Message);
			if (updates.OwnerChanged)
				updatesText.Add("Owner: " + issueRecurrence.Owner.GetName());
			if (updates.PriorityChanged)
				updatesText.Add("Priority from " + updates.oldPriority + " to " + issueRecurrence.Priority);
			if (updates.RankChanged)
				updatesText.Add("Rank from " + updates.oldRank + " to " + issueRecurrence.Rank);

			if (updates.CompletionChanged) {
				if (issueRecurrence.CloseTime == null)
					updatesText.Add("Marked Closed");
				else if (issueRecurrence.CloseTime != null)
					updatesText.Add("Marked Open");
			}
			var updatedText = "Updated Issue \"" + issueRecurrence.Issue.Message + "\" \n " + String.Join("\n", updatesText);
			Audit.L10Log(s, caller, recurrenceId, "UpdateIssue", ForModel.Create(issueRecurrence), updatedText);
		}

		public async Task AttachRock(ISession s, UserOrganizationModel caller, RockModel rock, L10Recurrence.L10Recurrence_Rocks recurRock) {
			Audit.L10Log(s, caller, recurRock.L10Recurrence.Id, "CreateRock", ForModel.Create(recurRock), rock.Rock);
		}
		
		public async Task AttachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, L10Recurrence.L10Recurrence_Measurable recurMeasurable) {
			Audit.L10Log(s, caller, recurMeasurable.L10Recurrence.Id, "CreateMeasurable", ForModel.Create(measurable), measurable.Title);
		}
		
		public async Task DetachMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, long recurrenceId) {
			Audit.L10Log(s, caller, recurrenceId, "DeleteMeasurable", ForModel.Create(measurable), measurable.Title);
		}
        
		public async Task CreateMeasurable(ISession s, MeasurableModel m) {
			//throw new NotImplementedException();
		}
		
		public async Task UpdateMeasurable(ISession s, UserOrganizationModel caller, MeasurableModel measurable, List<ScoreModel> updatedScores, IMeasurableHookUpdates updates) {
			var updateText = new List<String>();

			if (updates.MessageChanged)
				updateText.Add("Title: " + measurable.Title);

			if (updates.ShowCumulativeChanged)
				updateText.Add("Cumulative: " + measurable.ShowCumulative);

			if (updates.CumulativeRangeChanged)
				updateText.Add("Cumulative Start: " + measurable.CumulativeRange);

			if (updates.GoalDirectionChanged)
				updateText.Add("Goal Direction: " + measurable.GoalDirection.ToSymbol());

			if (updates.GoalChanged)
				updateText.Add("Goal: " + measurable.Goal);

			if (updates.AlternateGoalChanged)
				updateText.Add("AltGoal: " + measurable.AlternateGoal);
			
			if (updates.AccountableUserChanged) 
				updateText.Add("Accountable: " + measurable.AccountableUser.GetName());
			
			if (updates.AdminUserChanged) 
				updateText.Add("Admin: " + measurable.AdminUser.GetName());			

			var updatedText = "Updated Measurable: \"" + measurable.Title + "\" \n " + String.Join("\n", updateText);

			var recurrenceIds = RealTimeHelpers.GetRecurrencesForMeasurable(s,measurable.Id);

			foreach (var recurrenceId in recurrenceIds) {
				Audit.L10Log(s, caller, recurrenceId, "UpdateArchiveMeasurable", ForModel.Create(measurable), updatedText);
			}
		}


		#region Noop
		public async Task CreateRock(ISession s, RockModel rock) {
			//noop
		}
		public async Task ArchiveRock(ISession s, RockModel rock, bool deleted) {
			//noop
		}
        public async Task UnArchiveRock(ISession s, RockModel rock, bool v)
        {
            //Nothing to do...
        }
        public async Task UpdateHeadline(ISession s, PeopleHeadline headline, IHeadlineHookUpdates updates) {
			//noop
		}
        public async Task ArchiveHeadline(ISession s, PeopleHeadline headline)
        {
            //noop
        }
        public async Task UnArchiveHeadline(ISession s, PeopleHeadline headline)
        {
            //Nothing to do...
        }
		public async Task DetachRock(ISession s, RockModel rock, long recurrenceId) {
			//Noop
		}
		public async Task UpdateVtoRock(ISession s, L10Recurrence.L10Recurrence_Rocks recurRock) {
			//Noop
		}

		public async Task DeleteMeasurable(ISession s, MeasurableModel measurable) {          
			//Noop
		}


		#endregion
	}
}