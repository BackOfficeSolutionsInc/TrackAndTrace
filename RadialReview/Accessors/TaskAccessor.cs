using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using RadialReview.Controllers;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Enums;
using RadialReview.Models.Prereview;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.UserModels;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RadialReview.Accessors {
	public class TaskAccessor : BaseAccessor {
		public class TaskResult {
			public List<ScheduledTask> CreateTasks { get; set; }
			public ExecutionResult Result { get; set; }
			public List<Mail> SendEmails { get; set; }

			public TaskResult() {
				CreateTasks = new List<ScheduledTask>();
				SendEmails = new List<Mail>();
				Result = new ExecutionResult();
			}
		}

		public class ExecutionResult {
			public long TaskId { get; set; }
			public bool Executed { get; set; }
			public bool Error { get; set; }
			public string ErrorType { get; set; }
			public DateTime? StartTime { get; set; }
			public DateTime? EndTime { get; set; }
			public double DurationMs { get; set; }
			public string Message { get; set; }
			public bool ErrorEmailSent { get; set; }
			public string Url { get; set; }

			public dynamic Response { get; set; }

			public List<ScheduledTask> NewTasks { get; set; }
		}
		public static async Task<List<ExecutionResult>> ExecuteTasks(DateTime? now = null) {
			var nowV = now ?? DateTime.UtcNow;
			var tasks = GetTasksToExecute(nowV);
			return await _ExecuteTasks(tasks, nowV, d_ExecuteTaskFunc);
		}

		[Obsolete("Used only in testing")]
		public static async Task<ExecutionResult> ExecuteTask_Test(ScheduledTask task, DateTime now) {
			return (await _ExecuteTasks(task.AsList(), now, d_ExecuteTaskFunc_Test)).Single();
		}
		[Obsolete("Used only in testing")]
		public static async Task<List<ExecutionResult>> ExecuteTasks_Test(List<ScheduledTask> tasks, DateTime now) {
			return await _ExecuteTasks(tasks, now, d_ExecuteTaskFunc_Test);
		}
		
		protected static async Task<List<ExecutionResult>> _ExecuteTasks(List<ScheduledTask> tasks, DateTime now, ExecuteTaskFunc executeTaskFunc) {
			var toCreate = new List<ScheduledTask>();
			var emails = new List<Mail>();
			var res = new List<ExecutionResult>();

			try {
				MarkStarted(tasks, now);
				var results = await Task.WhenAll(tasks.Select(task => {
					try {
						return ExecuteTask_Internal(task, now, executeTaskFunc);
					} catch (Exception e) {
						log.Error("Task execution exception.", e);
						return null;
					}
				}));
				toCreate = results.Where(x => x != null).SelectMany(x => x.CreateTasks).ToList();
				emails = results.Where(x => x != null).SelectMany(x => x.SendEmails).ToList();
				res = results.Where(x => x != null).Select(x => {
					x.Result.NewTasks = x.CreateTasks;
					return x.Result;
				}).ToList();
			} finally {
				MarkStarted(tasks, null);
			}

			//Sessions Must be separated.
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					foreach (var c in toCreate)
						s.Save(c);
					foreach (var task in tasks)
						s.Update(task);
					tx.Commit();
					s.Flush();
				}
			}

			UpdateScorecard(now);
			try {
				await Emailer.SendEmails(emails);
			} catch (Exception e) {
				log.Error("Task execution exception. Email failed (2).", e);
			}
			return res;
		}

		protected delegate Task<dynamic> ExecuteTaskFunc(string server, ScheduledTask task,DateTime now);
		protected static async Task<TaskResult> ExecuteTask_Internal(ScheduledTask task,DateTime now, ExecuteTaskFunc execute) {
			var o = new TaskResult();
			var newTasks = o.CreateTasks;
			var sr = o.Result;
			sr.TaskId = task.Id;

			if (task != null) {
				sr.StartTime = DateTime.UtcNow;
				try {
					if (task.Url != null) {
						try {
							sr.Response = await execute(Config.BaseUrl(null), task, now);
						} catch (WebException webEx) {
							var response = webEx.Response as HttpWebResponse;
							if (response != null && response.StatusCode==HttpStatusCode.NotImplemented) {
								//Fallthrough Exception...
								log.Info("Task Fallthrough [OK] (taskId:"+task.Id+") (url:"+ task.Url + ")");
							} else {
								throw webEx;
							}
						}
					}
					sr.EndTime = DateTime.UtcNow;
					sr.DurationMs = (sr.EndTime.Value - sr.StartTime.Value).TotalMilliseconds;
					log.Debug("Scheduled task was executed. " + task.Id);
					task.Executed = DateTime.UtcNow;
					sr.Executed = true;
					if (task.NextSchedule != null) {
						var nt = new ScheduledTask() {
							FirstFire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
							Fire = (task.FirstFire ?? task.Fire).AddTimespan(task.NextSchedule.Value),
							NextSchedule = task.NextSchedule,
							Url = task.Url,
							TaskName = task.TaskName,
							MaxException = task.MaxException,
							OriginalTaskId = task.OriginalTaskId,
							CreatedFromTaskId = task.Id,
						};
						while (nt.Fire < DateTime.UtcNow) {
							nt.Fire = nt.Fire.AddTimespan(task.NextSchedule.Value);
							if (nt.FirstFire != null)
								nt.FirstFire = nt.FirstFire.Value.AddTimespan(task.NextSchedule.Value);
						}
						newTasks.Add(nt);
					}
				} catch (Exception e) {
					if (sr.EndTime == null) {
						sr.EndTime = DateTime.UtcNow;
						sr.DurationMs = (sr.EndTime.Value - sr.StartTime.Value).TotalMilliseconds;
					}
					if (e != null) {
						sr.ErrorType = "" + e.GetType();
						sr.Message = e.Message;
					}
					sr.Url = task.Url;
					sr.Error = true;
					log.Error("Scheduled task error. " + task.Id, e);

					//Send an email
					if (task != null && task.EmailOnException) {
						try {
							var builder = new StringBuilder();
							builder.AppendLine("TaskId:" + task.Id + "<br/>");
							if (e != null) {
								builder.AppendLine("ExceptionType:" + e.GetType() + "<br/>");
								builder.AppendLine("Exception:" + e.Message + "<br/>");
								builder.AppendLine("ExceptionCount:" + task.ExceptionCount + " out of " + task.MaxException + "<br/>");
								builder.AppendLine("Url:" + task.Url + "<br/>");
								builder.AppendLine("StackTrace:<br/>" + (e.StackTrace ?? "").Replace("\n", "\n<br/>") + "<br/>");
							} else {
								builder.AppendLine("Exception was null <br/>");
							}
							var mail = Mail.To(EmailTypes.CustomerSupport, ProductStrings.EngineeringEmail)
								.SubjectPlainText("Task failed to execute. Action Required.")
								.BodyPlainText(builder.ToString());

							o.SendEmails.Add(mail);
							sr.ErrorEmailSent = true;
						} catch (Exception ee) {
							log.Error("Task execution exception. Email failed (1).", ee);
						}
					}
					task.ExceptionCount++;
					if (task.MaxException != null && task.ExceptionCount >= task.MaxException) {
						if (task.NextSchedule != null) {
							newTasks.Add(new ScheduledTask() {
								FirstFire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
								Fire = (task.FirstFire ?? task.Fire).Add(task.NextSchedule.Value),
								NextSchedule = task.NextSchedule,
								Url = task.Url,
								MaxException = task.MaxException,
								TaskName = task.TaskName,
								OriginalTaskId = task.OriginalTaskId,
								CreatedFromTaskId = task.Id,
							});
							task.Executed = DateTime.MaxValue;
						}
					}
					task.Fire = DateTime.UtcNow + TimeSpan.FromMinutes(5 + Math.Pow(2, task.ExceptionCount + 1));
				}
			}
			return o;
		}
		protected static async Task<dynamic> d_ExecuteTaskFunc(String server, ScheduledTask task, DateTime _unused) {
			var webClient = new WebClient();
			var url = (server.TrimEnd('/') + "/" + task.Url.TrimStart('/'));
			if (url.Contains("?"))
				url += "&taskId=" + task.Id;
			else
				url += "?taskId=" + task.Id;
			var str = await webClient.DownloadStringTaskAsync(new Uri(url, UriKind.Absolute));
			return str;
		}
		protected static async Task<dynamic> d_ExecuteTaskFunc_Test(string _unused, ScheduledTask task,DateTime now) {
			var t = task;
			var url = t.Url;
			string[] parts = url.Split(new char[] { '?', '&' });
			var query = parts.ToDictionary(x => x.Split('=')[0].ToLower(), x => x.Split('=').ToList().Skip(1).LastOrDefault());
			var path = parts[0];
			var pathParts = path.Split('/');

			BaseController b=null;
			if (path.StartsWith("/Scheduler/ChargeAccount/")) {
				//var sc = new SchedulerController();
				//var re = await sc.ChargeAccount(pathParts.Last().ToLong(), task.Id/*,executeTime:now.ToJavascriptMilliseconds()*/);
				return await PaymentAccessor.ChargeOrganization(pathParts.Last().ToLong(), task.Id, true, executeTime:now);

			} else {
				throw new Exception("Unhandled URL: " + url);
			}

			//if (b != null) {
			//	if (b.Response.StatusCode != 200)
			//		throw new Exception("Status code was " + b.Response.StatusCode);
			//}

		}


		public static List<ScheduledTask> GetTasksToExecute(DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					//var all = s.QueryOver<ScheduledTask>().List().ToList();
					return s.QueryOver<ScheduledTask>().Where(x => x.Executed == null && x.Started == null && now >= x.Fire && x.DeleteTime == null && x.ExceptionCount <= 11 && (x.MaxException == null || x.ExceptionCount < x.MaxException)).List()
						.Where(x => x.ExceptionCount < (x.MaxException ?? 12))
						.ToList();
				}
			}
		}
		public static long AddTask(AbstractUpdate update, ScheduledTask task) {
			update.Save(task);
			task.OriginalTaskId = task.Id;
			update.Update(task);
			return task.Id;
		}

		public static long AddTask(ScheduledTask task) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var output = AddTask(s.ToUpdateProvider(), task);
					tx.Commit();
					s.Flush();
					return output;
				}
			}
		}


		public static void MarkStarted(List<ScheduledTask> tasks, DateTime? date) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					foreach (var t in tasks) {
						t.Started = date;
						s.Update(t);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}

		public static int GetUnstartedTaskCountForUser(ISession s, long forUserId, DateTime now) {
			return new Cache().GetOrGenerate(CacheKeys.UNSTARTED_TASKS, ctx => {
				var profileImage = 0;
				try {
					profileImage = String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid) ? 1 : 0;
				} catch {
				}
				var reviewCount = s.QueryOver<ReviewModel>().Where(x => x.ReviewerUserId == forUserId && x.DueDate > now && !x.Complete && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				var prereviewCount = s.QueryOver<PrereviewModel>().Where(x => x.ManagerId == forUserId && x.PrereviewDue > now && !x.Started && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				var nowPlus = now.Add(TimeSpan.FromDays(1));
				var todoCount = s.QueryOver<TodoModel>().Where(x => x.AccountableUserId == forUserId && x.DueDate < nowPlus && x.CompleteTime == null && x.DeleteTime == null).Select(Projections.RowCount()).FutureValue<int>();
				//var scorecardCount = s.QueryOver<ScoreModel>().Where(x => x.AccountableUserId == forUserId && x.DateDue < nowPlus && x.DateEntered == null).Select(Projections.RowCount()).FutureValue<int>();
				var total = reviewCount.Value + prereviewCount.Value /*+ scorecardCount.Value */+ profileImage + todoCount.Value;
				return total;
			});
		}

		public static List<TaskModel> GetTasksForUser(UserOrganizationModel caller, long forUserId, DateTime now) {
			var tasks = new List<TaskModel>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, caller);

					//Reviews
					var reviews = ReviewAccessor
						.GetReviewsForUser(s, perms, caller, forUserId, 0, int.MaxValue, now)
						.ToListAlive()
						.GroupBy(x => x.ForReviewContainerId);

					var reviewTasks = reviews.Select(x => new TaskModel() {
						Id = x.First().ForReviewContainerId,
						Type = TaskType.Review,
						Completion = CompletionModel.FromList(x.Select(y => y.GetCompletion())),
						DueDate = x.Max(y => y.DueDate),
						Name = x.First().Name
					});
					tasks.AddRange(reviewTasks);

					//Prereviews
					var prereviews = PrereviewAccessor.GetPrereviewsForUser(s.ToQueryProvider(true), perms, forUserId, now)
						.Where(x => x.Executed == null).ToListAlive();
					var reviewContainers = new Dictionary<long, String>();
					var prereviewCount = new Dictionary<long, int>();
					foreach (var p in prereviews) {
						reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
						prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
					}
					var prereviewTasks = prereviews.Select(x => new TaskModel() {
						Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
					});
					tasks.AddRange(prereviewTasks);
					var todos = TodoAccessor.GetTodosForUser(caller, caller.Id).Where(x =>
						x.DeleteTime == null &&
						(x.CompleteTime == null && x.DueDate < DateTime.UtcNow.AddDays(7)) //|| 
																						   //(x.DueDate > DateTime.UtcNow.AddDays(-1) && x.DueDate< DateTime.UtcNow.AddDays(1))
					).ToList();

					var todoTasks = todos.Select(x => new TaskModel() {
						Id = x.Id,
						Type = TaskType.Todo,
						DueDate = x.DueDate,
						Name = x.Message,
					});
					tasks.AddRange(todoTasks);

					//Scorecard
					/*var scores = s.QueryOver<ScoreModel>()
						.Where(x => x.AccountableUserId == forUserId && x.DateDue < now.AddDays(1) && x.DateEntered == null)
						.List().ToList();

					var scoreTasks = scores.GroupBy(x => x.DateDue.Date).Select(x => new TaskModel()
					{
						Count = x.Count(),
						DueDate = x.First().DateDue,
						Name = "Enter Scorecard Metrics",
						Type = TaskType.Scorecard
					});
					tasks.AddRange(scoreTasks);*/

					try {
						if (String.IsNullOrEmpty(s.Get<UserOrganizationModel>(forUserId).User.ImageGuid)) {
							tasks.Add(new TaskModel() {
								Type = TaskType.Profile,
								Name = "Update Profile (Picture)",
								DueDate = DateTime.MaxValue,
							});
						}
					} catch {

					}


					/*

					  .Where(x => x.Executed == null).ToListAlive();

					foreach (var p in prereviews)
					{
						reviewContainers[p.ReviewContainerId] = ReviewAccessor.GetReviewContainer(s.ToQueryProvider(true), perms, p.ReviewContainerId).ReviewName;
						prereviewCount[p.Id] = s.QueryOver<PrereviewMatchModel>()
							.Where(x => x.PrereviewId == p.Id && x.DeleteTime == null)
							.RowCount();
					}
					var prereviewTasks = prereviews.Select(x => new TaskModel()
					{
						Id = x.Id,
						Type = TaskType.Prereview,
						Count = prereviewCount[x.Id],
						DueDate = x.PrereviewDue,
						Name = reviewContainers[x.ReviewContainerId]
					});*/

				}
			}
			return tasks;
		}

		public static void UpdateScorecard(DateTime now) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var measurables = s.QueryOver<MeasurableModel>().Where(x => x.DeleteTime == null && x.NextGeneration <= now).List().ToList();

					//var weekLookup = new Dictionary<long, DayOfWeek>();

					//Next Thursday
					foreach (var m in measurables) {

						//var startOfWeek =weekLookup.GetOrAddDefault(m.OrganizationId, x => m.Organization.Settings.WeekStart);

						var nextDue = m.NextGeneration.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddDays((int)m.DueDate).Add(m.DueTime);

						var score = new ScoreModel() {
							AccountableUserId = m.AccountableUserId,
							DateDue = nextDue,
							MeasurableId = m.Id,
							Measurable = m,
							OrganizationId = m.OrganizationId,
							ForWeek = nextDue.StartOfWeek(DayOfWeek.Sunday),
							OriginalGoal = m.Goal,
							OriginalGoalDirection = m.GoalDirection
						};
						s.Save(score);
						m.NextGeneration = nextDue;
						s.Update(m);
					}
					tx.Commit();
					s.Flush();
				}
			}
		}
	}
}