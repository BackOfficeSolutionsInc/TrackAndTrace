using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate.Hql.Ast.ANTLR;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Components;
using RadialReview.Models.Issues;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Utilities.Encrypt;
using RadialReview.Controllers;
using NHibernate;
using RadialReview.Utilities.DataTypes;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using static RadialReview.Controllers.DashboardController;
using RadialReview.Models.Angular.Dashboard;
using RadialReview.Models.Dashboard;
using NHibernate.Criterion;
using RadialReview.Utilities.RealTime;
using RadialReview.Models.Rocks;
using RadialReview.Models.Askables;

namespace RadialReview.Accessors {

	public class TodoCreation {

		private string Message { get; set; }
		private string Details { get; set; }
		private DateTime? DueDate { get; set; }
		private TodoType TodoType { get; set; }
		private long? CreatedDuringMeetingId { get; set; }
		private long? RecurrenceId { get; set; }
		private long? AccountableUserId { get; set; }
		private DateTime? Now { get; set; }
		private string ForModelType { get; set; }
		private long ForModelId { get; set; }

		private bool _ensured { get; set; }

		private TodoCreation(string message, string details, long? accountableUserId, DateTime? dueDate, TodoType todoType, long? recurrenceId, long? createdDuringMeetingId, string modelType, long modelId, DateTime? now) {
			Message = message;
			Details = details;
			AccountableUserId = accountableUserId;
			DueDate = dueDate;
			TodoType = todoType;
			CreatedDuringMeetingId = createdDuringMeetingId;
			RecurrenceId = recurrenceId;
			ForModelType = modelType;
			ForModelId = modelId;
			Now = now;
		}

		public static TodoCreation CreatePersonalTodo(string message, string details = null, long? accountableUserId = null, DateTime? dueDate = null, DateTime? now = null) {
			return new TodoCreation(message, details, accountableUserId, dueDate, TodoType.Personal, null, null, "TodoModel", -1, now);
		}
		/// <summary>
		/// Best to pass in the due date as the beginning of the day on the day it is due, converted to server time.
		/// If todo is due on 2/28/2018, and the client is in Pacific time, pass in "2/28/2018 7:00:00". This method will add 23:59:59 to the date to make it due at end of day.
		/// In other words, pass in GetUser().GetTimeSettings().ConvertToServer(new Date(2018,2,28,0,0,0))
		/// </summary>
		/// <param name="recurrenceId"></param>
		/// <param name="message"></param>
		/// <param name="details"></param>
		/// <param name="accountableUserId"></param>
		/// <param name="dueDate"></param>
		/// <param name="createdDuringMeeting"></param>
		/// <param name="modelType"></param>
		/// <param name="modelId"></param>
		/// <param name="now"></param>
		/// <returns></returns>
		public static TodoCreation CreateL10Todo(long recurrenceId, string message, string details, long? accountableUserId, DateTime? dueDate, long? createdDuringMeeting = null, string modelType = "TodoModel", long modelId = -1, DateTime? now = null) {
			return new TodoCreation(message, details, accountableUserId, dueDate, TodoType.Recurrence, recurrenceId, createdDuringMeeting, modelType, modelId, now);
		}

		public TodoModel Generate(ISession s, PermissionsUtility perms) {
			//if (_ensured == false)
			//	throw new Exception("Permissions testing.");
			UserOrganizationModel creator = perms.GetCaller();
			var assignTo = AccountableUserId ?? creator.Id;
			EnsurePermitted(perms, creator.Organization.Id, assignTo);
			var duringMeeting = CreatedDuringMeetingId > 0 ? CreatedDuringMeetingId : null;
			var forRecur = RecurrenceId > 0 ? RecurrenceId : null;
			Now = Now ?? DateTime.UtcNow;

			var dueDate = Now.Value.AddDays(7);
			if (DueDate != null) {
				dueDate = DueDate.Value;// creator.GetTimeSettings().ConvertToServerTime(DueDate.Value);
				dueDate = dueDate.AddDays(1).AddSeconds(-1);
			}

			return new TodoModel {
				AccountableUserId = assignTo,
				AccountableUser = s.Load<UserOrganizationModel>(assignTo),
				CreatedDuringMeetingId = duringMeeting,
				CreatedDuringMeeting = duringMeeting.NotNull(x => s.Load<L10Meeting>(x)),
				ClearedInMeeting = null,
				CloseTime = null,
				CompleteTime = null,
				CompleteDuringMeetingId = null,
				CreatedBy = s.Load<UserOrganizationModel>(creator.Id),
				CreatedById = creator.Id,
				CreateTime = Now.Value,
				DeleteTime = null,
				Details = Details,
				DueDate = dueDate,//DueDate ?? Now.Value.AddDays(7),
				ForModel = ForModelType,
				ForModelId = ForModelId,
				ForRecurrence = forRecur.NotNull(x => s.Load<L10Recurrence>(x)),
				ForRecurrenceId = forRecur,
				Message = Message,
				Ordering = 0,
				Organization = creator.Organization,
				OrganizationId = creator.Organization.Id,
				TodoType = TodoType,
			};
		}

		[Untested("Appropriately tested?")]
		private void EnsurePermitted(PermissionsUtility perms, long orgId,long assignTo) {
			_ensured = true;

			if (CreatedDuringMeetingId != null && CreatedDuringMeetingId > 0)
				perms.ViewL10Meeting(CreatedDuringMeetingId.Value);

			perms.ViewOrganization(orgId);

			if (RecurrenceId != null)
				perms.EditL10Recurrence(RecurrenceId.Value);

			if (RecurrenceId == null && TodoType == TodoType.Recurrence)
				throw new PermissionsException("Recurrence Id is required to create a meeting todo.");
			
			perms.ViewUserOrganization(assignTo, false);
			perms.AssignTodo(assignTo, RecurrenceId);
		}
	}

	public class TodoAccessor : BaseAccessor {

		public static async Task<TodoModel> CreateTodo(UserOrganizationModel caller, TodoCreation creation) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					var todo = await CreateTodo(s, perms, creation);
					tx.Commit();
					s.Flush();
					return todo;
				}
			}
		}

		public static async Task<TodoModel> CreateTodo(ISession s, PermissionsUtility perms, TodoCreation creation) {
			var todo = creation.Generate(s, perms);

			if (!string.IsNullOrWhiteSpace(todo.Details))
				await PadAccessor.CreatePad(todo.PadId, todo.Details);

			s.Save(todo);
			todo.Ordering = -todo.Id;
			s.Update(todo);

			await HooksRegistry.Each<ITodoHook>((ses, x) => x.CreateTodo(ses, todo));

            if (todo.ForRecurrenceId.HasValue && todo.ForRecurrenceId > 0) {
                await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.AttachTodo(ses, perms.GetCaller(), todo));
            }

			return todo;

		}

		public static async Task UpdateTodo(UserOrganizationModel caller, long todoId, string message = null, DateTime? dueDate = null, long? accountableUser = null, bool? complete = null, ForModel source = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
                    await UpdateTodo(s, perms, todoId, message, dueDate, accountableUser, complete, source: source);
                    //L10Recurrence L10 = new L10Recurrence();
                    //ForModel.Create(L10);
                    //ForModel.Create<L10Recurrence>(1);

					tx.Commit();
					s.Flush();
				}
			}
		}
		
        public static async Task UpdateTodo(ISession s, PermissionsUtility perms, long todoId, string message = null, DateTime? dueDate = null, long? accountableUser = null, bool? complete = null, bool duringMeeting = false, ForModel source = null /*, string connectionId = null, bool duringMeeting = false, bool? delete = null*/) {
			perms.EditTodo(todoId);

			var todo = s.Get<TodoModel>(todoId);
			var updates = new ITodoHookUpdates();

			//Message
			if (message != null && todo.Message != message) {
				todo.Message = message;
				updates.MessageChanged = true;
			}
			//Due Date
			if (dueDate != null && dueDate != todo.DueDate) {
				todo.DueDate = dueDate.Value;
				updates.DueDateChanged = true;
			}
			//Accountable User
			if (accountableUser != null && todo.AccountableUserId != accountableUser) {
				perms.AssignTodo(accountableUser.Value, todo.ForRecurrenceId);
				todo.AccountableUserId = accountableUser.Value;
				todo.AccountableUser = s.Load<UserOrganizationModel>(accountableUser.Value);
				updates.AccountableUserChanged = true;
			}
			//Complete
			if (complete != null) {
				if (complete == true && todo.CompleteTime == null) {
					todo.CompleteTime = DateTime.UtcNow;
					updates.CompletionChanged = true;
				} else if (complete == false && todo.CompleteTime != null) {
					todo.CompleteTime = null;
					todo.CompleteDuringMeetingId = null;
					updates.CompletionChanged = true;
				}
				//Completed during meeting
				if (duringMeeting && todo.ForRecurrenceId != null && complete==true) {
					try {
						var meetingId = L10Accessor._GetCurrentL10Meeting(s, perms, todo.ForRecurrenceId.Value, true, false, false).NotNull(x => x.Id);
						if (meetingId != 0)
							todo.CompleteDuringMeetingId = meetingId;
					} catch (Exception) { }
				}
            }

            List<Action> actionList = new List<Action>();
            if (source != null) {
                actionList = await ChangeTodoSource(s,perms, source, todo);
			}

			s.Update(todo);
			await HooksRegistry.Each<ITodoHook>((ses, x) => x.UpdateTodo(ses,perms.GetCaller(), todo, updates));

            foreach (var item in actionList) {
                item();
            }
        }

        private static async Task<List<Action>> ChangeTodoSource(ISession s, PermissionsUtility perms, ForModel newSource_, TodoModel todo) {
            
            List<Action> actionList = new List<Action>();
            var l10Todo = ForModel.GetModelType<L10Recurrence>();
            var personalTodo = ForModel.GetModelType<UserOrganizationModel>();

            {
                // deal with oldSource
                var oldListSource = todo.GetListSource();

                if(oldListSource.ModelType != l10Todo && (oldListSource.ModelType != personalTodo)) {
                    throw new PermissionsException("Unhandled List Type.");
                }

                if (oldListSource.Equals(newSource_)) {
                    return actionList;
                }

                if (oldListSource.ModelType == l10Todo) {
                    perms.EditL10Recurrence(oldListSource.ModelId);
                    actionList.Add((async () => await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.DetachTodo(ses, perms.GetCaller(), todo))));
                    //await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.DetachTodo(ses, perms.GetCaller(), todo, oldListSource.ModelId));
                }

                if (oldListSource.ModelType == personalTodo) {
                    perms.Self(oldListSource.ModelId);
                    actionList.Add((async () => await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.DetachTodo(ses, perms.GetCaller(), todo))));
                }
            }

            {
                // deal with newSource
                var newSource = newSource_;

                if (newSource.ModelType != l10Todo && (newSource.ModelType != personalTodo)) {
                    throw new PermissionsException("Unhandled List Type.");
                }

                if (newSource.ModelType == l10Todo) {
                    perms.EditL10Recurrence(newSource.ModelId);
                    actionList.Add((async () => await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.AttachTodo(ses, perms.GetCaller(), todo))));

                    todo.ForRecurrenceId = newSource.ModelId;
                    todo.ForRecurrence = s.Get<L10Recurrence>(newSource.ModelId);
                    todo.TodoType = TodoType.Recurrence;
                }

                if (newSource.ModelType == personalTodo) {
                    perms.Self(newSource.ModelId);

                    if (newSource.ModelId != todo.AccountableUserId) {
                        throw new PermissionsException("You can only create personal todo for yourself.");
                    }

                    actionList.Add((async () => await HooksRegistry.Each<IMeetingTodoHook>((ses, x) => x.AttachTodo(ses, perms.GetCaller(), todo))));

                    todo.ForRecurrenceId = null; // for personal todo
                    todo.ForRecurrence = null;
                    todo.TodoType = TodoType.Personal;
                }
            }
            
            return actionList;
		}

		public static async Task CompleteTodo(UserOrganizationModel caller, long todoId, bool completed = true,bool duringMeeting=false) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					await CompleteTodo(s, perms, todoId, completed, duringMeeting);
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static async Task CompleteTodo(ISession s, PermissionsUtility perms, long todoId, bool completed = true, bool duringMeeting = false) {
			var todo = s.Get<TodoModel>(todoId);
			if (todo.CompleteTime != null && completed)
				throw new PermissionsException("To-do already checked.");
			if (todo.CompleteTime == null && !completed)
				throw new PermissionsException("To-do already unchecked.");
			await UpdateTodo(s, perms, todoId, complete: completed, duringMeeting: duringMeeting);
		}

		public static TodoModel GetTodo(UserOrganizationModel caller, long todoId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ViewTodo(todoId);
					var found = s.Get<TodoModel>(todoId);
					var a = found.AccountableUser.GetName();
					var b = found.AccountableUser.ImageUrl(true);
					var c = found.NotNull(x => x.GetIssueMessage());
					var d = found.NotNull(x => x.GetIssueDetails());

					return found;
				}
			}
		}
		public static List<AngularTodo> GetTodosForUser(UserOrganizationModel caller, long userId, bool excludeCompleteDuringMeeting = false, DateRange range = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, caller).ManagesUserOrganizationOrSelf(userId);
					var found = GetTodosForUsers_Unsafe(s, new[] { userId }, excludeCompleteDuringMeeting, range);
					return found;
				}
			}
		}

		public static List<AngularTodo> GetMyTodosAndMilestones(UserOrganizationModel caller, long userId, bool excludeCompleteDuringMeeting = false, DateRange range = null, bool includeTodos = true, bool includeMilestones = true) {

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);
					perms.Self(userId);

					var allSelf = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.DeleteTime == null && x.User.Id == caller.User.Id)
						.Select(x => x.Id)
						.List<long>().ToArray();
					return GetTodosForUsers_Unsafe(s, allSelf, excludeCompleteDuringMeeting, range, includeTodos, includeMilestones);
				}
			}
		}
		private static List<AngularTodo> GetTodosForUsers_Unsafe(ISession s, long[] userIds, bool excludeCompleteDuringMeeting, DateRange range, bool includeTodos = true, bool includeMilestones = false) {
			// List<TodoModel> found;
			var weekAgo = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday).AddDays(-7);
			List<IEnumerable<TodoModel>> todosMany = new List<IEnumerable<TodoModel>>();


			if (includeTodos) {
				foreach (var a in userIds) {
					var q = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.AccountableUserId == a);
					//.WhereRestrictionOn(x => x.AccountableUserId)
					//.IsIn(userIds);

					var useStart = weekAgo;
					if (range != null) {
						useStart = range.StartTime;
					}
					
					if (excludeCompleteDuringMeeting)
						q = q.Where(x => x.CompleteTime == null || (x.CompleteTime != null && x.CompleteTime >= useStart && x.CompleteDuringMeetingId == null));
					if (false && range != null)
						q = q.Where(x => x.CompleteTime == null || (x.CompleteTime != null && x.CompleteTime >= range.StartTime && x.CompleteTime <= range.EndTime));
					todosMany.Add(q.List().ToList());
				}
			}

			//Add milestones
			var milestones = new List<Milestone>();
			var rockName = new Dictionary<long, string>();
			var rockOwnerLookup = new Dictionary<long, long>();
			if (includeMilestones) {
				var rockAndOwnerIds = s.QueryOver<RockModel>()
					.Where(x => x.DeleteTime == null)
					.WhereRestrictionOn(x => x.AccountableUser.Id).IsIn(userIds)
					.Select(x => x.Id, x => x.AccountableUser.Id, x => x.Rock)
					.Future<object[]>()
					.Select(x => new {
						RockId = (long)x[0],
						UserId = (long)x[1],
						Name = (string)x[2]
					}).ToList();

				var rockIds = rockAndOwnerIds.Select(x => x.RockId).ToArray();
				rockOwnerLookup = rockAndOwnerIds.ToDictionary(x => x.RockId, x => x.UserId);
				rockName = rockAndOwnerIds.ToDictionary(x => x.RockId, x => x.Name);
				var mq = s.QueryOver<Milestone>().Where(x => x.DeleteTime == null).WhereRestrictionOn(x => x.RockId).IsIn(rockIds);
				if (range != null) {
					mq = mq.Where(x => x.CompleteTime == null || (x.CompleteTime != null && x.CompleteTime >= range.StartTime && x.CompleteTime <= range.EndTime));
				}
				milestones = mq.List().ToList();
			}

			var angular = new List<AngularTodo>();

			var ownerLookup = new DefaultDictionary<long, UserOrganizationModel>(x => {
				var user = s.Get<UserOrganizationModel>(x);
				var a = user.GetName();
				var b = user.ImageUrl(true, ImageSize._32);
				return user;
			});

			if (includeTodos) {
				var todosResolved = todosMany.ToList().SelectMany(x => x).ToList();

				foreach (var f in todosResolved) {
					var a = f.ForRecurrence.NotNull(x => x.Id);
					var b = f.AccountableUser.NotNull(x => x.GetName());
					var c = f.AccountableUser.NotNull(x => x.ImageUrl(true, ImageSize._32));
					var d = f.CreatedDuringMeeting.NotNull(x => x.Id);
					var e = f.ForRecurrence.NotNull(x => x.Name);

				}
				//Populate dictionary
				foreach (var u in todosResolved.Select(x => x.AccountableUser)) {
					ownerLookup[u.Id] = u;
				}
				angular.AddRange(todosResolved.Select(x => new AngularTodo(x)));
			}

			if (includeMilestones) {
				angular.AddRange(milestones.Select(milestone => {
					var rockId = milestone.RockId;
					var ownerId = rockOwnerLookup[rockId];
					var owner = ownerLookup[ownerId];
					var rock = rockName[rockId];
					return new AngularTodo(milestone, owner, origin: rock);
				}));
			}
			return angular;
		}
		

		public static Csv Listing(UserOrganizationModel caller, long organizationId) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					// var p = s.Get<PeriodModel>(period);

					PermissionsUtility.Create(s, caller).ManagingOrganization(organizationId);

					var csv = new Csv();

					var todos = s.QueryOver<TodoModel>().Where(x => x.DeleteTime == null && x.OrganizationId == organizationId).List().ToList();
					foreach (var t in todos) {
						csv.Add("" + t.Id, "Owner", t.AccountableUser.NotNull(x => x.GetName()));
						csv.Add("" + t.Id, "Created", t.CreateTime.ToShortDateString());
						csv.Add("" + t.Id, "Due Date", t.DueDate.ToShortDateString());
						var time = "";
						if (t.CompleteTime != null)
							time = t.CompleteTime.Value.ToShortDateString();
						csv.Add("" + t.Id, "Completed", time);
						csv.Add("" + t.Id, "To-Do", "" + t.Message);

						//if (false /*&& includeDetails*/) {
						//	var padDetails = await PadAccessor.GetText(t.PadId);
						//	csv.Add("" + t.Id, "Details", "" + padDetails);
						//}
					}

					csv.SetTitle("Todos");
					return csv;
				}
			}
		}
		public static string _SharedSecretTodoPrefix(long userId) {
			return "402F5DE7-DB3C-40D3-B634-42EF5E7D9118+" + userId;
		}


		public static async Task<StringBuilder> BuildTodoTable(IEnumerable<ITodoTiny> todos, int timezoneOffset, string dateFormat,  string title = null, bool showDetails = false, Dictionary<string, HtmlString> padLookup = null) {
			title = title.NotNull(x => x.Trim()) ?? "To-do";
			var table = new StringBuilder();
			try {

				table.Append(@"<table width=""100%""  border=""0"" cellpadding=""0"" cellspacing=""0"">");
				table.Append(@"<tr><th colspan=""3"" align=""left"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;"">" + title + @"</th><th align=""right"" style=""font-size:16px;border-bottom: 1px solid #D9DADB;width: 80px;"">Due Date</th></tr>");
				var i = 1;
				if (todos.Any()) {
					//var org = todos.FirstOrDefault().NotNull(x => x.Organization);
					//var ts = timesettings.GetTimeSettings();
					var now = TimeData.ConvertFromServerTime(DateTime.UtcNow, timezoneOffset).Date;
					var format = dateFormat ?? "MM-dd-yyyy";// for .NotNull(x => x.Settings.NotNull(y => y.GetDateFormat())) ?? "MM-dd-yyyy";
					foreach (var todo in todos.OrderBy(x => x.DueDate.Date).ThenBy(x => x.Message)) {
						var color = todo.DueDate.Date <= now ? "color:#F22659;" : "color: #34AD00;";
						var completionIcon = Config.BaseUrl(null) + @"Image/TodoCompletion?id=" + HttpUtility.UrlEncode(Crypto.EncryptStringAES("" + todo.Id, _SharedSecretTodoPrefix(todo.AccountableUserId))) + "&userId=" + todo.AccountableUserId;
						var duedate = todo.DueDate;
						duedate = TimeData.ConvertFromServerTime(DateTime.UtcNow, timezoneOffset);

						table.Append(@"<tr><td width=""16px"" valign=""top"" style=""padding: 2px 0 0 0;""><img src='").Append(completionIcon).Append("' width='15' height='15'/>").Append(@"</td><td width=""1px"" style=""vertical-align: top;""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(null) + @"Todo/List"">")
							.Append(i).Append(@". </a></b></td><td align=""left"" valign=""top""><b><a style=""color:#333333;text-decoration:none;"" href=""" + Config.BaseUrl(null) + @"Todo/List?todo=" + todo.Id + @""">")
							.Append(todo.Message).Append(@"</a></b></td><td  align=""right"" valign=""top"" style=""" + color + @""">")
							.Append(duedate.ToString(format)).Append("</td></tr>");

						if (showDetails) {

							HtmlString details = null;
							if (padLookup == null || !padLookup.ContainsKey(todo.PadId)) {
								details = await PadAccessor.GetHtml(todo.PadId);
							} else {
								details = padLookup[todo.PadId];
							}

							//var details = await PadAccessor.GetHtml(todo.PadId);

							if (!String.IsNullOrWhiteSpace(details.ToHtmlString())) {
								table.Append(@"<tr><td colspan=""2""></td><td><i style=""font-size:12px;"">&nbsp;&nbsp;<a style=""color:#333333;text-decoration: none;"" href=""" + Config.BaseUrl(null) + @"Todo/List"">").Append(details.ToHtmlString()).Append("</a></i></td><td></td></tr>");
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


		#region old		
		//[Obsolete("Do not use", true)]
		//public static List<TodoModel> GetRecurrenceTodos(UserOrganizationModel caller, long recurrenceId, bool excludeCompleteDuringMeeting = false, DateRange range = null) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			return s.QueryOver<TodoModel>()
		//				.Where(x => x.DeleteTime == null
		//				&& x.ForRecurrenceId == recurrenceId).List().ToList();
		//		}
		//	}
		//}
		//[Obsolete("Do not use", true)]
		//public static TodoModel MarkComplete(UserOrganizationModel caller, long todoId, DateTime completeTime) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var perm = PermissionsUtility.Create(s, caller).EditTodo(todoId);
		//			var found = s.Get<TodoModel>(todoId);

		//			found.CompleteTime = completeTime;
		//			s.Update(found);

		//			tx.Commit();
		//			s.Flush();
		//			return found;
		//		}
		//	}
		//}

		//[Untested("Hooks", "AssignTodo permissions")]
		//[Obsolete("Use other", true)]
		//public static async Task<bool> CreateTodo(ISession s, PermissionsUtility perms, long recurrenceId, TodoModel todo) {
		//	if (todo.Id != 0)
		//		throw new PermissionsException("Id was not zero");

		//	if (todo.CreatedDuringMeetingId == -1)
		//		todo.CreatedDuringMeetingId = null;
		//	perms.ConfirmAndFix(todo,
		//		x => x.CreatedDuringMeetingId,
		//		x => x.CreatedDuringMeeting,
		//		x => x.ViewL10Meeting);

		//	if (todo.OrganizationId == 0 && todo.Organization == null)
		//		todo.OrganizationId = perms.GetCaller().Organization.Id;
		//	perms.ConfirmAndFix(todo,
		//		x => x.OrganizationId,
		//		x => x.Organization,
		//		x => x.ViewOrganization);

		//	perms.ConfirmAndFix(todo,
		//		x => x.ForRecurrenceId,
		//		x => x.ForRecurrence,
		//		x => x.EditL10Recurrence);

		//	if ((todo.ForRecurrenceId == null || todo.ForRecurrence == null) && todo.TodoType == TodoType.Recurrence)
		//		throw new PermissionsException("Recurrence Id is required to create a meeting todo.");


		//	if (todo.CreatedById == 0 && todo.CreatedBy == null)
		//		todo.CreatedById = perms.GetCaller().Id;
		//	perms.ConfirmAndFix(todo,
		//		x => x.CreatedById,
		//		x => x.CreatedBy,
		//		x => y => x.ViewUserOrganization(y, false));

		//	if (todo.AccountableUserId == 0 && todo.AccountableUser == null)
		//		todo.AccountableUserId = perms.GetCaller().Id;
		//	perms.ConfirmAndFix(todo,
		//		x => x.AccountableUserId,
		//		x => x.AccountableUser,
		//		x => y => x.ViewUserOrganization(y, false));


		//	//Added
		//	perms.AssignTodo(todo.AccountableUserId, todo.ForRecurrenceId);

		//	L10Recurrence r = null;
		//	if (recurrenceId > 0) {
		//		r = s.Get<L10Recurrence>(recurrenceId);
		//		//r.Pristine = false;
		//		await L10Accessor.Depristine_Unsafe(s, perms.GetCaller(), r);
		//		s.Update(r);
		//	}
		//	if (todo.TodoType == TodoType.Recurrence)
		//		ExternalTodoAccessor.AddLink(s, perms, ForModel.Create(r), todo.AccountableUserId, todo);
		//	else if (todo.TodoType == TodoType.Personal)
		//		ExternalTodoAccessor.AddLink(s, perms, ForModel.Create(todo.AccountableUser), todo.AccountableUserId, todo);
		//	else
		//		throw new PermissionsException("unhandled TodoType");

		//	if (String.IsNullOrWhiteSpace(todo.PadId))
		//		todo.PadId = Guid.NewGuid().ToString();

		//	if (!string.IsNullOrWhiteSpace(todo.Details))
		//		await PadAccessor.CreatePad(todo.PadId, todo.Details);
		//	if (recurrenceId > 0) {
		//		todo.ForRecurrenceId = recurrenceId;
		//		todo.ForRecurrence = r;
		//	}

		//	s.Save(todo);
		//	//if (todo.DueDate == todo.DueDate.Date)
		//	//    todo.DueDate = todo.DueDate.Date.AddDays(1).AddMinutes(-todo.Organization.GetTimezoneOffset()).AddMilliseconds(-1);
		//	todo.Ordering = -todo.Id;
		//	s.Update(todo);

		//	await HooksRegistry.Each<ITodoHook>((ses, x) => x.CreateTodo(ses, todo));

		//	if (todo.TodoType == TodoType.Personal) {

		//		var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
		//		var userMeetingHub = hub.Clients.Group(MeetingHub.GenerateUserId(todo.AccountableUserId));
		//		var todoData = TodoData.FromTodo(todo);
		//		userMeetingHub.appendTodo(".todo-list", todoData);
		//		var updates = new AngularRecurrence(recurrenceId);
		//		updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
		//		userMeetingHub.update(updates);
		//	}

		//	if (recurrenceId > 0) {
		//		var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
		//		var meetingHub = hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(recurrenceId));
		//		var todoData = TodoData.FromTodo(todo);

		//		if (todo.CreatedDuringMeetingId != null)
		//			todoData.isNew = true;
		//		meetingHub.appendTodo(".todo-list", todoData);

		//		var message = "Created to-do.";

		//		var showWhoCreatedDetails = true;
		//		if (showWhoCreatedDetails) {
		//			try {
		//				if (perms.GetCaller() != null && perms.GetCaller().GetFirstName() != null) {
		//					message = perms.GetCaller().GetFirstName() + " created a to-do.";
		//				}
		//			} catch (Exception) {
		//			}
		//		}

		//		meetingHub.showAlert(message, 1500);

		//		var updates = new AngularRecurrence(recurrenceId);
		//		updates.Todos = AngularList.CreateFrom(AngularListType.Add, new AngularTodo(todo));
		//		updates.Focus = "[data-todo='" + todo.Id + "'] input:visible:first";
		//		meetingHub.update(updates);

		//		#region Add to L10 Tile
		//		try {
		//			Dashboard dashboardAlias = null;
		//			var dashs = s.QueryOver<TileModel>()
		//				.JoinAlias(x => x.Dashboard, () => dashboardAlias)
		//				.Where(x => x.DeleteTime == null && x.Type == TileType.Url && x.DataUrl == "/TileData/L10Todos/" + recurrenceId)
		//				.Select(x => x.Dashboard.Id, x => x.Id, x => dashboardAlias.ForUser.Id)
		//				.List<object[]>()
		//				.Select(x => new {
		//					DashboardId = (long)x[0],
		//					TileId = (long)x[1],
		//					UserId = (string)x[2]
		//				}).ToList();

		//			if (dashs.Any()) {
		//				//Only do if there are tiles
		//				var dashUserIds = dashs.Select(x => x.UserId).Distinct().ToArray();
		//				var dashUsers = s.QueryOver<UserOrganizationModel>()
		//									.Where(x => x.DeleteTime == null)
		//									.WhereRestrictionOn(x => x.User.Id)
		//									.IsIn(dashUserIds)
		//									.List().ToList();

		//				var canView = new DefaultDictionary<string, bool>(x => false);
		//				foreach (var u in dashUsers) {
		//					if (canView[u.User.Id] == false) {
		//						try {
		//							PermissionsUtility.Create(s, u).ViewL10Recurrence(recurrenceId);
		//							canView[u.User.Id] = true;
		//						} catch (PermissionsException) {
		//						}
		//					}
		//				}

		//				foreach (var d in dashs) {
		//					if (canView[d.UserId]) {
		//						var tile = new AngularTileId<IEnumerable<AngularTodo>>(d.TileId, recurrenceId, null, AngularTileKeys.L10TodoList(recurrenceId)) {
		//							Contents = AngularList.Create(AngularListType.Add, new[] { new AngularTodo(todo) })
		//						};
		//						meetingHub.update(new AngularUpdate() { tile });
		//					}
		//				}
		//			}
		//		} catch (Exception e) {
		//			//Special stuff,
		//			log.Error(e);
		//		}
		//		#endregion
		//		Audit.L10Log(s, perms.GetCaller(), recurrenceId, "CreateTodo", ForModel.Create(todo), todo.NotNull(x => x.Message));
		//	}

		//	return true;
		//}
		//[Obsolete("Use other", true)]
		//public static async Task<bool> CreateTodo(UserOrganizationModel caller, long recurrenceId, TodoModel todo) {
		//	using (var s = HibernateSession.GetCurrentSession()) {
		//		using (var tx = s.BeginTransaction()) {
		//			var perms = PermissionsUtility.Create(s, caller);
		//			//.ViewL10Recurrence(recurrenceId); //Tested below
		//			//perms.ViewL10Recurrence(recurrenceId);
		//			var created = await CreateTodo(s, perms, recurrenceId, todo);

		//			tx.Commit();
		//			s.Flush();

		//			return created;
		//		}
		//	}
		//}

		#endregion

	}
}
