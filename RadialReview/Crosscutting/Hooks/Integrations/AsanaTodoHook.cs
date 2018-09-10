using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate;
using RadialReview.Models;
using RadialReview.Models.Todo;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using RadialReview.Utilities;
using RadialReview.Crosscutting.Integrations.Asana;
using System.Text;
using Newtonsoft.Json;
using RadialReview.Models.Integrations;
using RadialReview.Models.Integrations.Asana;
using RadialReview.Hangfire;
using Hangfire;
using RadialReview.Crosscutting.Schedulers;
using RestSharp;

namespace RadialReview.Crosscutting.Hooks.Integrations {
	public class AsanaTodoHook : ITodoHook {
		public bool CanRunRemotely() {
			return false;
		}

		public HookPriority GetHookPriority() {
			return HookPriority.Lowest;
		}

		public async Task CreateTodo(ISession s, TodoModel todo) {
			await ExecuteCreate(s, todo);
		}


		public class TaskUpdate {
			public Dictionary<string,object> data { get; set; }
		}

		public async Task UpdateTodo(ISession s, UserOrganizationModel caller, TodoModel todo, ITodoHookUpdates updates) {

			var todoLinks = s.QueryOver<TodoLink>()
							.Where(x => x.InternalTodoId == todo.Id)
							.List().Where(x => x.Service == TodoService.Asana)
							.ToList();

			//If adding a user that is listening...
			if (updates.AccountableUserChanged) {
				//add to new user
				await ExecuteCreate(s, todo);
				//Delete from old user
				foreach (var link in todoLinks) {
					await ExecuteDelete(s, updates.PreviousAccountableUser, link.ServiceTodoId);
				}
			} else {

				//Update listening users
				if (todoLinks.Any()) {
					foreach (var link in todoLinks) {
						var todoUpdates = new List<KeyValuePair<string, string>>();

						if (updates.CompletionChanged) {
							todoUpdates.Add(new KeyValuePair<string, string>("completed",""+(todo.CompleteTime != null)));
						}

						if (updates.DueDateChanged) {
							todoUpdates.Add(new KeyValuePair<string, string>("due_at", "" + todo.DueDate.ToString("yyyy-MM-ddTHH:mm:ssZ")));
						}

						if (updates.MessageChanged) {
							todoUpdates.Add(new KeyValuePair<string, string>("name", todo.Message));
						}

						if (todoUpdates.Any()) {
							var token = await AsanaAccessor.GetUserToken_Unsafe(s, todo.AccountableUserId);
							if (token != null) {
								var actions = await AsanaAccessor.GetActionsForToken_Unsafe(s, token.Id);
								foreach (var action in actions) {
									if (action.ActionType == AsanaActionType.SyncMyTodos) {
										Scheduler.Enqueue(() => UpdateTodoInAsana_Hangfire(token.AccessToken, link.ServiceTodoId, todoUpdates));
										//Exit After First One
										break;
									}
								}
							}
						}
					}
				}
			}
		}

		private static async Task ExecuteDelete(ISession s, long userId, long asanaTodoId) {
			var token = await AsanaAccessor.GetUserToken_Unsafe(s, userId);

			if (token != null) {
				var actions = await AsanaAccessor.GetActionsForToken_Unsafe(s, token.Id);
				foreach (var action in actions) {
					if (action.ActionType == AsanaActionType.SyncMyTodos) {
						Scheduler.Enqueue(() => DeleteTodoInAsana_Hangfire(asanaTodoId, token.AccessToken));
						//Exit After First One
						break;
					}
				}
			}
		}

		private static async Task ExecuteCreate(ISession s, TodoModel todo) {
			var userId = todo.AccountableUserId;
			var token = await AsanaAccessor.GetUserToken_Unsafe(s, userId);

			if (token != null) {
				var actions = await AsanaAccessor.GetActionsForToken_Unsafe(s, token.Id);
				foreach (var action in actions) {
					if (action.ActionType == Models.Integrations.AsanaActionType.SyncMyTodos) {
						Scheduler.Enqueue(() => CreateTodoInAsana_Hangfire(todo.Id,todo.DueDate, todo.Message, todo.Details, token.AsanaUserId, token.AccessToken, action.ProjectId,action.WorkspaceId));
						//Exit After First One
						//break;
					}
				}
			}
		}


		[Queue(HangfireQueues.Immediate.ASANA_EVENTS)]
		[AutomaticRetry(Attempts = 3)]
		public async Task UpdateTodoInAsana_Hangfire(string asanaToken,long asanaTodoId, List<KeyValuePair<string,string>> updates) {
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			using (var client = new WebClient()) {
				client.Headers.Add("Authorization", "Bearer " + asanaToken);

				var param = new NameValueCollection();
				foreach (var u in updates) {
					param.Add(u.Key, u.Value);
				}

				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				await client.UploadValuesTaskAsync(AsanaAccessor.AsanaUrl("tasks/" + asanaTodoId), "PUT", param);				
			}			
		}

		[Queue(HangfireQueues.Immediate.ASANA_EVENTS)]
		[AutomaticRetry(Attempts =3)]
		public async static Task<long> DeleteTodoInAsana_Hangfire(long asanaTodoId, string asanaToken) {
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

			using (var client = new WebClient()) {
				client.Headers.Add("Authorization", "Bearer " + asanaToken);
				client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
				await client.UploadValuesTaskAsync(AsanaAccessor.AsanaUrl("tasks/" + asanaTodoId), "DELETE", new NameValueCollection());
			}

			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var links = s.QueryOver<TodoLink>().Where(x => x.ServiceTodoId == asanaTodoId && x.Service == TodoService.Asana).List().ToList();
					var i = 0;
					foreach (var link in links) {
						s.Delete(link);
						i++;
					}
					tx.Commit();
					s.Flush();
					return i;
				}
			}
		}


		[Queue(HangfireQueues.Immediate.ASANA_EVENTS)]
		[AutomaticRetry(Attempts =3)]
		public static async Task<TodoLink> CreateTodoInAsana_Hangfire(long todoId,DateTime due, string todoMessage, string todoDetails, long asanaUserId, string asanaToken, long projectId, long workspaceId) {
			TodoLink link;

			//var pclient = new RestClient("https://app.asana.com/api/1.0/tasks");
			//var prequest = new RestRequest(Method.POST);
			//prequest.AddHeader("postman-token", "98b69928-ad68-4fc6-ee7e-700c7e43cd88");
			//prequest.AddHeader("cache-control", "no-cache");
			//prequest.AddHeader("authorization", "Bearer " + asanaToken);
			//prequest.AddHeader("accept", "application/json");
			//prequest.Timeout = 4000;

			//prequest.

			//var response = pclient.Execute(prequest);


			using (var client = new WebClient()) {
				var param = new NameValueCollection();
				param.Add("assignee", "" + asanaUserId);
				param.Add("name", todoMessage??"");
				if (todoDetails != null) {
					param.Add("notes", todoDetails);
				}
				param.Add("projects", "" + projectId);
				param.Add("workspace", "" + workspaceId);
				param.Add("due_at", "" + due.ToString("yyyy-MM-ddTHH:mm:ssZ"));

				client.Headers.Add("Authorization", "Bearer " + asanaToken);
				ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
				


				byte[] responsebytes = await client.UploadValuesTaskAsync(AsanaAccessor.AsanaUrl("tasks/"), "POST", param);
				var responseBody = Encoding.UTF8.GetString(responsebytes);
				var response = JsonConvert.DeserializeObject<AsanaTaskDTO>(responseBody);

				link = new TodoLink() {
					InternalTodoId = todoId,
					Service = TodoService.Asana,
					ServiceTodoId = response.data.id
				};
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					s.Save(link);
					tx.Commit();
					s.Flush();
					return link;
				}
			}
		}
	}
}