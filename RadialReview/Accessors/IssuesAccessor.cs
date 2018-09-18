using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using RadialReview.Models.Angular.Base;
using RadialReview.Utilities.DataTypes;
using System.Text;
using System.Web;
using RadialReview.Utilities.RealTime;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Models.Todo;
using SpreadsheetLight;
using static RadialReview.Accessors.IssuesAccessor;
using RadialReview.Utilities.Synchronize;
using RadialReview.Utilities.NHibernate;
using RadialReview.Models.VTO;
using RadialReview.Models.Angular.VTO;

namespace RadialReview.Accessors {

	public class IssueCreation {
		private string Message { get; set; }
		private string Details { get; set; }
		private long? OwnerId { get; set; }
		private long? CreatedDuringMeetingId { get; set; }

		private long RecurrenceId { get; set; }
		private DateTime? Now { get; set; }
		private string ForModelType { get; set; }
		private long ForModelId { get; set; }
		private bool _ensured { get; set; }
		private int Priority { get; set; }

		private IssueCreation(string message, string details, long? ownerId, long? createdDuringMeetingId, int priority, long recurrenceId, DateTime? now, string forModelType, long forModelId) {
			Message = message;
			Details = details;
			OwnerId = ownerId;
			CreatedDuringMeetingId = createdDuringMeetingId;
			RecurrenceId = recurrenceId;
			Now = now;
			ForModelType = forModelType;
			ForModelId = forModelId;
			Priority = priority;
		}

		public static IssueCreation CreateL10Issue(string message, string details, long? ownerId, long recurrenceId, long? createdDuringMeeting = null, int priority = 0, string modelType = "IssueModel", long modelId = -1, DateTime? now = null) {
			return new IssueCreation(message, details, ownerId, createdDuringMeeting, priority, recurrenceId, now, modelType, modelId);
		}


		public IssueOutput Generate(ISession s, PermissionsUtility perms) {
			UserOrganizationModel creator = perms.GetCaller();
			EnsurePermitted(perms, creator.Organization.Id);

			var duringMeeting = CreatedDuringMeetingId > 0 ? CreatedDuringMeetingId : null;
			Now = Now ?? DateTime.UtcNow;

			var issue = new IssueModel {
				CreatedById = OwnerId ?? creator.Id,
				CreatedBy = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
				CreatedDuringMeetingId = duringMeeting,
				CreatedDuringMeeting = duringMeeting.NotNull(x => s.Load<L10Meeting>(x)),
				CreateTime = Now.Value,
				Description = Details,
				ForModel = ForModelType,
				ForModelId = ForModelId,
				Message = Message,
				Organization = creator.Organization,
				OrganizationId = creator.Organization.Id,
				_Priority = Priority,
			};

			var issueRecur = new IssueModel.IssueModel_Recurrence() {
				CopiedFrom = null,
				Issue = issue,
				CreatedBy = issue.CreatedBy,
				CreateTime = issue.CreateTime,
				Recurrence = s.Load<L10Recurrence>(RecurrenceId),
				Owner = s.Load<UserOrganizationModel>(OwnerId ?? creator.Id),
				Priority = issue._Priority
			};

			return new IssueOutput {
				IssueModel = issue,
				IssueRecurrenceModel = issueRecur,
			};

		}

		private void EnsurePermitted(PermissionsUtility perms, long orgId) {
			_ensured = true;

			if (CreatedDuringMeetingId != null && CreatedDuringMeetingId > 0)
				perms.ViewL10Meeting(CreatedDuringMeetingId.Value);
			perms.ViewOrganization(orgId);
			if (OwnerId != null)
				perms.ViewUserOrganization(OwnerId.Value, false);

			perms.EditL10Recurrence(RecurrenceId);
		}

	}



	public class IssuesAccessor : BaseAccessor {



		public class IssueOutput {
			public IssueModel IssueModel { get; set; }
			public IssueModel.IssueModel_Recurrence IssueRecurrenceModel { get; set; }
		}

		public static async Task<IssueOutput> CreateIssue(ISession s, PermissionsUtility perms, IssueCreation issueCreator) {

			var io = issueCreator.Generate(s, perms);

            io.IssueModel.PadId = await PadAccessor.CreatePad(io.IssueModel.Description);
            
            s.Save(io.IssueModel);
			s.Save(io.IssueRecurrenceModel);
			
			var recurrenceId = io.IssueRecurrenceModel.Recurrence.Id;
			var r = s.Get<L10Recurrence>(recurrenceId);
			
			if (r.OrderIssueBy == "data-priority") {
				var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
					.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Priority > io.IssueModel._Priority && x.ParentRecurrenceIssue == null)
					.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
				var max = -1L;
				if (order.Any())
					max = order.Max() ?? -1;
				max += 1;
				io.IssueRecurrenceModel.Ordering = max;
				s.Update(io.IssueRecurrenceModel);
			}
			if (r.OrderIssueBy == "data-rank") {
				var order = s.QueryOver<IssueModel.IssueModel_Recurrence>()
					.Where(x => x.Recurrence.Id == recurrenceId && x.DeleteTime == null && x.CloseTime == null && x.Rank > io.IssueModel._Rank && x.ParentRecurrenceIssue == null)
					.Select(x => x.Ordering).List<long?>().Where(x => x != null).ToList();
				var max = -1L;
				if (order.Any())
					max = order.Max() ?? -1;
				max += 1;
				io.IssueRecurrenceModel.Ordering = max;
				s.Update(io.IssueRecurrenceModel);
			}

			// Trigger webhook events
			await HooksRegistry.Each<IIssueHook>((ses, x) => x.CreateIssue(ses, io.IssueRecurrenceModel));

			return io;

		}


		public static async Task<IssueOutput> CreateIssue(UserOrganizationModel caller, IssueCreation creation) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var o = await CreateIssue(s, perms, creation);
					tx.Commit();
					s.Flush();
					return o;
				}
			}
		}


		public enum IssueCompartment {
			ShortTerm = 1,
			LongTerm = 2,
		}

		public static async Task EditIssue(UserOrganizationModel caller, long issueRecurrenceId, string message = null, bool? complete = null,
			long? owner = null, int? priority = null, int? rank = null, bool? awaitingSolve = null, DateTime? now = null, IssueCompartment? compartment = null) {
			
			await SyncUtil.EnsureStrictlyAfter(caller, SyncAction.UpdateIssueMessage(issueRecurrenceId), async s => {
				var perms = PermissionsUtility.Create(s, caller);
				await EditIssue(s, perms, issueRecurrenceId, message, complete, owner, priority, rank, awaitingSolve, now, compartment);
			});			
		}
		/// <summary>
		/// SyncAction.UpdateIssueMessage(issue.Issue.Id)
		/// </summary>
		/// <param name="s"></param>
		/// <param name="perms"></param>
		/// <param name="issueRecurrenceId"></param>
		/// <param name="message"></param>
		/// <param name="complete"></param>
		/// <param name="owner"></param>
		/// <param name="priority"></param>
		/// <param name="rank"></param>
		/// <param name="awaitingSolve"></param>
		/// <param name="now"></param>
		/// <returns></returns>
		public static async Task EditIssue(IOrderedSession s, PermissionsUtility perms, long issueRecurrenceId, string message = null,
			bool? complete = null, long? owner = null, int? priority = null, int? rank = null, /*bool? delete=null,*/ bool? awaitingSolve = null,
			DateTime? now = null, IssueCompartment? status = null) {
			now = Math2.Min(DateTime.UtcNow.AddSeconds(3), now ?? DateTime.UtcNow);

			var issue = s.Get<IssueModel.IssueModel_Recurrence>(issueRecurrenceId);
			if (issue == null)
				throw new PermissionsException("Issue does not exist.");

			var recurrenceId = issue.Recurrence.Id;
			if (recurrenceId == 0)
				throw new PermissionsException("Meeting does not exist.");

			perms.EditL10Recurrence(recurrenceId);

			var updates = new IIssueHookUpdates();
			
			if (message != null && message != issue.Issue.Message) {
				issue.Issue.Message = message;
				updates.MessageChanged = true;
			}
			if (owner != null && (issue.Owner == null || owner != issue.Owner.Id) && owner > 0) {
				var any = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == issue.Recurrence.Id && x.User.Id == owner).Take(1).List().ToList();
				if (!any.Any())
					throw new PermissionsException("Specified Owner cannot see meeting");

				issue.Owner = s.Get<UserOrganizationModel>(owner);
				updates.OwnerChanged = true;
			}
			if (priority != null && priority != issue.Priority && issue.LastUpdate_Priority < now) {
				issue.LastUpdate_Priority = now.Value;
				updates.oldPriority = issue.Priority;
				issue.Priority = priority.Value;
				s.Update(issue);
				updates.PriorityChanged = true;
			}
			if (rank != null && rank != issue.Rank && issue.LastUpdate_Priority < now) {
				issue.LastUpdate_Priority = now.Value;
				updates.oldRank = issue.Rank;
				issue.Rank = rank.Value;
				s.Update(issue);
				updates.RankChanged = true;
			}

			if (status != null) {
				if (status == IssueCompartment.ShortTerm && issue.DeleteTime != null) {
					updates.CompartmentChanged = true;
					await MoveIssueFromVtoViaIssueRecurrenceId(s, perms, issue.Id);
				} else if (status == IssueCompartment.LongTerm && issue.DeleteTime == null) {
					updates.CompartmentChanged = true;
					MoveIssueToVto(s, perms, issue.Id, perms.GetCaller().NotNull(x=>x.GetClientRequestId()));
				}

			}

			var now1 = DateTime.UtcNow;
			if (complete != null) {
				if (complete.Value && issue.CloseTime == null) {
					updates.CompletionChanged = true;
				} else if (!complete.Value && issue.CloseTime != null) {
					updates.CompletionChanged = true;
				}
				_UpdateIssueCompletion_Unsafe(s, issue, complete.Value, now1);
			}



			if (awaitingSolve != null && awaitingSolve != issue.AwaitingSolve) {
				issue.AwaitingSolve = awaitingSolve.Value;
				s.Update(issue);
				updates.AwaitingSolveChanged = true;
			}

			await HooksRegistry.Each<IIssueHook>((ses, x) => x.UpdateIssue(ses, perms.GetCaller(), issue, updates));

		}

		public static VtoItem_String MoveIssueToVto(ISession s, PermissionsUtility perm, long issue_recurrence, string connectionId) {
			using (var rt = RealTimeUtility.Create(connectionId)) {
				var recurIssue = s.Get<IssueModel.IssueModel_Recurrence>(issue_recurrence);
				
				recurIssue.Rank = 0;
				recurIssue.Priority = 0;
				recurIssue.DeleteTime = DateTime.UtcNow;
				s.Update(recurIssue);

				var recur = s.Get<L10Recurrence>(recurIssue.Recurrence.Id);

				//remove from list
				rt.UpdateRecurrences(recur.Id).AddLowLevelAction(x => x.removeIssueRow(recurIssue.Id));
				var arecur = new AngularRecurrence(recur.Id);
				arecur.IssuesList.Issues = AngularList.CreateFrom(AngularListType.Remove, new AngularIssue(recurIssue));
				rt.UpdateRecurrences(recur.Id).Update(arecur);
				
				perm.EditVTO(recur.VtoId);
				var vto = s.Get<VtoModel>(recur.VtoId);

				var str = VtoAccessor.AddString(s, perm, recur.VtoId, VtoItemType.List_Issues,
					(v, list) => new AngularVTO(v.Id) { Issues = list },
					true, forModel: ForModel.Create(recurIssue), value: recurIssue.Issue.Message);

				return str;
			}
		}

		public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVtoViaIssueRecurrenceId(ISession s, PermissionsUtility perms, long issue_recurrence) {
			var modelType = ForModel.GetModelType<IssueModel.IssueModel_Recurrence>();
			var found = s.QueryOver<VtoItem_String>().Where(x => x.DeleteTime == null && x.ForModel.ModelId == issue_recurrence && x.ForModel.ModelType == modelType).Take(1).SingleOrDefault();

			return await MoveIssueFromVto(s, perms, found.Id);
		}


		public async static Task<IssueModel.IssueModel_Recurrence> MoveIssueFromVto(ISession s, PermissionsUtility perm, long vtoIssue) {
			var now = DateTime.UtcNow;
			var vtoIssueStr = s.Get<VtoItem_String>(vtoIssue);

			IssueModel.IssueModel_Recurrence issueRecur;
			perm.EditVTO(vtoIssueStr.Vto.Id);

			vtoIssueStr.DeleteTime = now;
			s.Update(vtoIssueStr);

			if (vtoIssueStr.ForModel != null) {
				if (vtoIssueStr.ForModel.ModelType != ForModel.GetModelType<IssueModel.IssueModel_Recurrence>())
					throw new PermissionsException("ModelType was unexpected");
				issueRecur = s.Get<IssueModel.IssueModel_Recurrence>(vtoIssueStr.ForModel.ModelId);

				var recur = s.Get<L10Recurrence>(issueRecur.Recurrence.Id);

				perm.EditL10Recurrence(issueRecur.Recurrence.Id);

				issueRecur.DeleteTime = null;
				s.Update(issueRecur);
				//Add back to issues list (does not need to be added below. CreateIssue calls this.
				var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
				var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(issueRecur.Recurrence.Id));
				meetingHub.appendIssue(".issues-list", IssuesData.FromIssueRecurrence(issueRecur), recur.OrderIssueBy);
			} else {
				var vto = s.Get<VtoModel>(vtoIssueStr.Vto.Id);
				if (vto.L10Recurrence == null)
					throw new PermissionsException("Expected L10Recurrence was null");
				var creation = IssueCreation.CreateL10Issue(vtoIssueStr.Data, null, perm.NotNull(x=>x.GetCaller().Id), vto.L10Recurrence.Value);
				var issue = await IssuesAccessor.CreateIssue(s, perm, creation);
				var recur = s.Get<L10Recurrence>(vto.L10Recurrence.Value);

				issueRecur = issue.IssueRecurrenceModel;
			}
			//Remove from vto
			var vtoHub = GlobalHost.ConnectionManager.GetHubContext<VtoHub>();
			var group = vtoHub.Clients.Group(VtoHub.GenerateVtoGroupId(vtoIssueStr.Vto.Id));
			vtoIssueStr.Vto = null;
			group.update(new AngularUpdate() { AngularVtoString.Create(vtoIssueStr) });
			
			return issueRecur;
		}

		public static void _UpdateIssueCompletion_Unsafe(ISession s, /*RealTimeUtility rt,*/ IssueModel.IssueModel_Recurrence issue, bool complete, DateTime? now = null) {
			now = now ?? DateTime.UtcNow;
			bool? added = null;
			if (complete && issue.CloseTime == null) {
				issue.CloseTime = now;
				added = false;
			} else if (!complete && issue.CloseTime != null) {
				issue.CloseTime = null;
				issue.MarkedForClose = false;
				added = true;
			}

			s.Update(issue);
			if (added != null) {
				var others = s.QueryOver<IssueModel.IssueModel_Recurrence>()
                      .Where(x => x.DeleteTime == null && x.Issue.Id == issue.Issue.Id)
                      .List().ToList();

				//Not sure what I was thinking here...
				foreach (var o in others) {
					if (o.Id != issue.Id) {
						o.MarkedForClose = complete;
						s.Update(o);
					}
				}
			}
		}

		public static IssueModel GetIssue(UserOrganizationModel caller, long issueId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewIssue(issueId);
					return s.Get<IssueModel>(issueId);
				}
			}
		}

		public static List<IssueModel.IssueModel_Recurrence> GetVisibleIssuesForUser(UserOrganizationModel caller, long userId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller).ViewUserOrganization(userId, false);

					// only get meetings visible to me.
					var list = L10Accessor.GetVisibleL10Meetings_Tiny(s, perms, caller.Id, true, false).Select(x => x.Id).ToList();

					return s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null
						&& x.CloseTime == null
						&& x.Owner.Id == userId).WhereRestrictionOn(x => x.Recurrence.Id).IsIn(list).Fetch(x => x.Issue).Eager.List().ToList();
				}
			}
		}


		public static List<IssueModel.IssueModel_Recurrence> GetRecurrenceIssuesForUser(UserOrganizationModel caller, long userId, long recurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// throw new NotImplementedException("query is incorrect");
					PermissionsUtility.Create(s, caller).ViewRecurrenceIssuesForUser(userId, recurrenceId);

					return s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null
						&& x.Recurrence.Id == recurrenceId
						&& x.CloseTime == null
						&& x.Owner.Id == userId).Fetch(x => x.Issue).Eager.List().ToList();
				}
			}
		}

		public static IssueModel.IssueModel_Recurrence GetIssue_Recurrence(UserOrganizationModel caller, long recurrence_issue) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var found = s.Get<IssueModel.IssueModel_Recurrence>(recurrence_issue);

					PermissionsUtility.Create(s, caller)
						.ViewL10Recurrence(found.Recurrence.Id)
						.ViewIssue(found.Issue.Id);

					found.Issue = s.Get<IssueModel>(found.Issue.Id);
					found.Recurrence = s.Get<L10Recurrence>(found.Recurrence.Id);

					return found;
				}
			}
		}

		public static IssueModel.IssueModel_Recurrence CopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;
					var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

					PermissionsUtility.Create(s, caller)
						.ViewL10Recurrence(parent.Recurrence.Id)
						.ViewIssue(parent.Issue.Id);

					var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

					if (childRecur.Organization.Id != caller.Organization.Id)
						throw new PermissionsException("You cannot copy an issue into this meeting.");
					if (parent.DeleteTime != null)
						throw new PermissionsException("Issue does not exist.");

					var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
					if (possible.All(x => x.Id != childRecurrenceId)) {
						throw new PermissionsException("You do not have permission to copy this issue.");
					}

					var issue_recur = new IssueModel.IssueModel_Recurrence() {
						ParentRecurrenceIssue = null,
						CreateTime = now,
						CopiedFrom = parent,
						CreatedBy = caller,
						Issue = s.Load<IssueModel>(parent.Issue.Id),
						Recurrence = s.Load<L10Recurrence>(childRecurrenceId),
						Owner = parent.Owner
					};
					s.Save(issue_recur);
					var viewModel = IssuesData.FromIssueRecurrence(issue_recur);
					_RecurseCopy(s, viewModel, caller, parent, issue_recur, now);
					tx.Commit();
					s.Flush();

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(childRecurrenceId));

					meetingHub.appendIssue(".issues-list", viewModel);
					var issue = s.Get<IssueModel>(parent.Issue.Id);
					Audit.L10Log(s, caller, parent.Recurrence.Id, "CopyIssue", ForModel.Create(issue_recur), issue.NotNull(x => x.Message) + " copied into " + childRecur.NotNull(x => x.Name));
					return issue_recur;
				}
			}
		}

		private static void _RecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, IssueModel.IssueModel_Recurrence parent, DateTime now) {
			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
				.List();
			var childrenVMs = new List<IssuesData>();
			foreach (var child in children) {
				var issue_recur = new IssueModel.IssueModel_Recurrence() {
					ParentRecurrenceIssue = parent,
					CreateTime = now,
					CopiedFrom = child,
					CreatedBy = caller,
					Issue = s.Load<IssueModel>(child.Issue.Id),
					Recurrence = s.Load<L10Recurrence>(parent.Recurrence.Id),
					Owner = s.Load<UserOrganizationModel>(parent.Owner.Id)
				};
				s.Save(issue_recur);
				var childVM = IssuesData.FromIssueRecurrence(issue_recur);
				childrenVMs.Add(childVM);
				_RecurseCopy(s, childVM, caller, child, issue_recur, now);
			}
			viewModel.children = childrenVMs.ToArray();
		}

		public static IssueModel.IssueModel_Recurrence UnCopyIssue(UserOrganizationModel caller, long parentIssue_RecurrenceId, long childRecurrenceId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow;

					var parent = s.Get<IssueModel.IssueModel_Recurrence>(parentIssue_RecurrenceId);

					PermissionsUtility.Create(s, caller)
						.ViewL10Recurrence(parent.Recurrence.Id)
						.ViewIssue(parent.Issue.Id);

					var childRecur = s.Get<L10Recurrence>(childRecurrenceId);

					if (childRecur.Organization.Id != caller.Organization.Id)
						throw new PermissionsException("You cannot Uncopy an issue into this meeting.");
					if (parent.DeleteTime != null)
						throw new PermissionsException("Issue does not exist.");

					var possible = L10Accessor._GetAllL10RecurrenceAtOrganization(s, caller, caller.Organization.Id);
					if (possible.All(x => x.Id != childRecurrenceId)) {
						throw new PermissionsException("You do not have permission to uncopy this issue.");
					}

					var getL10RecurrenceChild = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.Where(x => x.DeleteTime == null && x.Recurrence.Id == childRecurrenceId && x.Issue.Id == parent.Issue.Id)
						.SingleOrDefault();

					if (getL10RecurrenceChild == null) {
						throw new PermissionsException("Issue Recurrence does not exist.");
					}

					getL10RecurrenceChild.DeleteTime = now;
					s.Update(getL10RecurrenceChild);

					var viewModel = IssuesData.FromIssueRecurrence(getL10RecurrenceChild);
					_UnRecurseCopy(s, viewModel, caller, parent, now);
					tx.Commit();
					s.Flush();

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(childRecurrenceId));

					meetingHub.removeIssueRow(getL10RecurrenceChild.Id);
					var issue = s.Get<IssueModel>(parent.Issue.Id);
					Audit.L10Log(s, caller, parent.Recurrence.Id, "UnCopyIssue", ForModel.Create(getL10RecurrenceChild), issue.NotNull(x => x.Message) + " Uncopied from " + childRecur.NotNull(x => x.Name));
					return getL10RecurrenceChild;
				}
			}
		}

		private static void _UnRecurseCopy(ISession s, IssuesData viewModel, UserOrganizationModel caller, IssueModel.IssueModel_Recurrence copiedFrom, DateTime now) {
			var children = s.QueryOver<IssueModel.IssueModel_Recurrence>()
				.Where(x => x.DeleteTime == null && x.ParentRecurrenceIssue.Id == copiedFrom.Id)
				.List();
			var childrenVMs = new List<IssuesData>();
			foreach (var child in children) {
				child.DeleteTime = now;
				s.Update(child);
				var childVM = IssuesData.FromIssueRecurrence(child);
				childrenVMs.Add(childVM);
				_UnRecurseCopy(s, childVM, caller, child, now);
			}
			viewModel.children = childrenVMs.ToArray();
		}

		public static Csv Listing(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// var p = s.Get<PeriodModel>(period);

					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

					var sb = new StringBuilder();
					sb.Append("Id,Depth,Owner,Created,Closed,Issue");
					var csv = new Csv();
					IssueModel issueA = null;

					var issues = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.JoinAlias(x => x.Issue, () => issueA)
						.Where(x => x.DeleteTime == null)
						.Where(x => issueA.OrganizationId == organizationId)
						.Fetch(x => x.Issue).Eager
						.List().ToList();

					foreach (var t in issues) {
						var time = "";
						csv.Add("" + t.Id, "Owner", t.Owner.NotNull(x => x.GetName()));
						csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
						if (t.CloseTime != null)
							time = t.CloseTime.Value.ToShortDateString();
						csv.Add("" + t.Id, "Completed", time);
						csv.Add("" + t.Id, "Issue", "" + t.Issue.Message);

						//if (false /*&& includeDetails*/) {
						//	var padDetails = await PadAccessor.GetText(t.PadId);
						//	csv.Add("" + t.Id, "Details", "" + padDetails);
						//}
					}

					csv.SetTitle("Issues");
					return csv;
				}
			}
		}

		public class IssueAndTodos {
			public IssueModel.IssueModel_Recurrence Issue { get; set; }
			public List<TodoModel> Todos { get; set; }
		}
		public static async Task<SLDocument> GetIssuesAndTodosSpreadsheetAtOrganization(UserOrganizationModel caller, long orgId, bool loadDetails = false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.ManagingOrganization(orgId);

					IssueModel issueAlias = null;
					var issuesQ = s.QueryOver<IssueModel.IssueModel_Recurrence>()
						.JoinAlias(x => x.Issue, () => issueAlias)
						.Where(x => x.DeleteTime == null && issueAlias.OrganizationId == orgId && issueAlias.DeleteTime == null)
						.Future();


					var todosQ = s.QueryOver<TodoModel>()
						.Where(x => x.ForModel == "IssueModel" && x.DeleteTime == null && x.OrganizationId == orgId)
						.Future();

					var result = new List<IssueAndTodos>();

					var allTodos = todosQ.ToList();


					var pads = new List<string>();
					foreach (var issue in issuesQ) {
						var iat = new IssueAndTodos();
						iat.Issue = issue;
						iat.Todos = allTodos.Where(x => x.ForModelId == issue.Issue.Id && x.ForModel == "IssueModel").ToList();
						pads.Add(issue.Issue.PadId);
						pads.AddRange(iat.Todos.Select(x => x.PadId));
						result.Add(iat);
					}

					var padLookup = new Dictionary<string, string>();

					if (loadDetails) {
						padLookup = await PadAccessor.GetTexts(pads);
					}

					var issuesSheet = new Csv("Issues");
					foreach (var iat in result) {
						var ir = iat.Issue;
						var issue = ir.Issue;
						var id = issue.Id;
						issuesSheet.Add("" + id, "Issue", issue.Message);
						if (loadDetails) {
							var details = padLookup.GetOrDefault(issue.PadId, "");
							issuesSheet.Add("" + id, "Details", details);
						}
						issuesSheet.Add("" + id, "Owner", ir.Owner.Name);
						issuesSheet.Add("" + id, "Completed", ir.CloseTime.NotNull(x => x.Value.ToShortDateString()));
						issuesSheet.Add("" + id, "Created", issue.CreateTime.ToShortDateString());

						issuesSheet.Add("" + id, "# Todos", "" + iat.Todos.Count());

					}

					var todoSheet = new Csv("Todos");
					foreach (var todo in result.SelectMany(x => x.Todos)) {
						var id = todo.Id;
						todoSheet.Add("" + id, "Todo", todo.Message);
						if (loadDetails) {
							var details = padLookup.GetOrDefault(todo.PadId, "");
							todoSheet.Add("" + id, "Details", details);
						}
						todoSheet.Add("" + id, "Owner", todo.AccountableUser.Name);
						todoSheet.Add("" + id, "Completed", todo.CompleteTime.NotNull(x => x.Value.ToShortDateString()));
						todoSheet.Add("" + id, "Created", todo.CreateTime.ToShortDateString());
						todoSheet.Add("" + id, "IssueId", "" + todo.ForModelId);
					}
					return CsvUtility.ToXls(issuesSheet, todoSheet);
				}
			}
		}

		public static async Task<StringBuilder> BuildIssuesSolvedTable(List<IssueModel.IssueModel_Recurrence> issues, string title = null, long? recurrenceId = null, bool showDetails = false, Dictionary<string, HtmlString> padLookup = null) {
			title = title.NotNull(x => x.Trim()) ?? "Issues";
			var table = new StringBuilder();
			try {

				table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
				table.Append(@"<tr><th colspan=""2"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">" + title + @"</th></tr>");
				var i = 1;
				if (issues.Any()) {
					var org = issues.FirstOrDefault().NotNull(x => x.Issue.Organization);
					var now = issues.FirstOrDefault().NotNull(x => x.Issue.Organization.ConvertFromUTC(DateTime.UtcNow).Date);
					var format = org.NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";
					foreach (var issue in issues.OrderBy(x => x.CloseTime)) {
						var url = "#";
						if (recurrenceId != null)
							url = Config.BaseUrl(org) + @"L10/Details/" + recurrenceId + "#/Issues";

						table.Append(@" <tr><td width=""1px"" style=""vertical-align: top;""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
							.Append(i).Append(@". </a></b></td><td align=""left""><b><a style=""color:#333333;text-decoration:none;"" href=""" + url + @""">")
							.Append(issue.Issue.Message).Append(@"</a></b></td></tr>");

						if (showDetails) {
							HtmlString details = null;
							if (padLookup == null || !padLookup.ContainsKey(issue.Issue.PadId)) {
								details = await PadAccessor.GetHtml(issue.Issue.PadId);
							} else {
								details = padLookup[issue.Issue.PadId];
							}

							if (!String.IsNullOrWhiteSpace(details.ToHtmlString())) {
								table.Append(@"<tr><td></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + url + @""">").Append(details.ToHtmlString()).Append("</a></i></td></tr>");
							}
						}

						i++;
					}
				}
			} catch (Exception e) {
				log.Error(e);
			}
			table.Append("</table>");
			return table;
		}
	}
}
