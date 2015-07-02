using System.EnterpriseServices;
using System.Text;
using log4net.Repository.Hierarchy;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Application;
using RadialReview.Models.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Properties;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
	public class SchedulerController : BaseController
	{
		//
		// GET: /Scheduler/
		[Access(AccessLevel.Any)]
		public bool Index()
		{
			return true;
		}

		[Access(AccessLevel.Any)]
		public async Task<string> EmailTodos()
		{
			var unsent = new List<MailModel>();
			using (var s = HibernateSession.GetCurrentSession()){
				using (var tx = s.BeginTransaction()){
					
					var nowUtc = DateTime.UtcNow;
					if (nowUtc.DayOfWeek == DayOfWeek.Saturday || nowUtc.DayOfWeek == DayOfWeek.Sunday)
						return "No fire on weekend.";

					var started = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ApplicationAccessor.DAILY_EMAIL_TODO_TASK && x.Started != null).List().ToList();
					if (!started.Any())
						throw new PermissionsException("Task not started");


					var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
					var rangeLow = nowUtc.Date.AddDays(-1);
					var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
					var nextWeek = nowUtc.Date.AddDays(7);
					if (nowUtc.DayOfWeek == DayOfWeek.Friday)
						rangeHigh=rangeHigh.AddDays(1);



					var todos = s.QueryOver<TodoModel>().Where(x => ((rangeLow <= x.DueDate && x.DueDate <= rangeHigh) || (x.CompleteTime == null && x.DueDate <= nextWeek)) && x.DeleteTime == null).List().ToList();

					var dictionary = new Dictionary<string, List<TodoModel>>();
					
					foreach (var t in todos.GroupBy(x => x.AccountableUser.NotNull(y=>y.User.NotNull(z=>z.Email)))){
						if (t.Key != null){
							dictionary.GetOrAddDefault(t.Key, x => new List<TodoModel>()).AddRange(t);
						}
					}

					foreach (var userTodos in dictionary){
						string subject = null;
						var nowLocal = userTodos.Value.First().Organization.ConvertFromUTC(nowUtc).Date;

						var overDue = userTodos.Value.Count(x => x.DueDate.Date <= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);
						if (overDue == 1)
							subject = "You have an overdue task";
						else if (overDue > 1)
							subject = "You have " + overDue + " overdue tasks";
						else{
							var dueToday = userTodos.Value.Count(x => x.DueDate.Date == nowLocal.Date && x.CompleteTime == null);

							if (dueToday == 1)
								subject = "You have a task due today";
							else if (dueToday > 1)
								subject = "You have " + dueToday + " tasks due today";
							else{
								var dueTomorrow = userTodos.Value.Count(x => x.DueDate.Date == nowLocal.AddDays(1).Date && x.CompleteTime == null);
								if (dueTomorrow == 1)
									subject = "You have a task due tomorrow";
								else if (dueTomorrow > 1)
									subject = "You have " + dueTomorrow + " tasks due tomorrow";
								else{
									var dueSoon = userTodos.Value.Count(x => x.DueDate.Date > nowLocal.AddDays(1).Date && x.CompleteTime == null);
									if (dueSoon == 1)
										subject = "You have a task due soon";
									else if (dueSoon > 1)
										subject = "You have " + dueSoon + " tasks due soon";
								}
							}
						}


						var shouldSend = userTodos.Value.Count(x => x.DueDate.Date >= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);

						if (subject != null && shouldSend>0)
						{

							try{
								var user = userTodos.Value.First().AccountableUser;
								var email = user.GetEmail();

								var builder = new StringBuilder();
								foreach (var t in userTodos.Value.Where(x=>x.CompleteTime==null || x.DueDate.Date>nowUtc.Date).GroupBy(x => x.ForRecurrenceId)){
									builder.Append(TodoAccessor.BuildTodoTable(t.ToList(), t.First().ForRecurrence.NotNull(x => x.Name + " To-do")));
									builder.Append("<br/>");
								}
								
								var mail = MailModel.To(email)
									.Subject(EmailStrings.TodoReminder_Subject, subject)
									.Body(EmailStrings.TodoReminder_Body, 
										user.GetName(), 
										builder.ToString(), 
										Config.ProductName(user.Organization),
										Config.BaseUrl(user.Organization)+"Todo/List"
										);

								unsent.Add(mail);
							}catch (Exception ex){

							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			try{
				await Emailer.SendEmails(unsent);
				return "sent: "+unsent.Count;
			}
			catch (Exception e){
				return e.Message;
			}
		}



		[Access(AccessLevel.Any)]
		public async Task<bool> Reschedule()
		{
			var now = DateTime.UtcNow;
			var tasks = _TaskAccessor.GetTasksToExecute(now);
			var toCreate = new List<ScheduledTask>();
			try
			{
				_TaskAccessor.MarkStarted(tasks, now);
				var results = await Task.WhenAll(tasks.Select(task =>
				{
					try
					{
						return _TaskAccessor.ExecuteTask(Config.BaseUrl(null), task);
					}
					catch (Exception e)
					{
						log.Error("Task execution exception.", e);
						return null;
					}
				}));
				toCreate = results.Where(x => x != null).SelectMany(x => x).ToList();
			}
			finally
			{
				_TaskAccessor.MarkStarted(tasks, null);
			}
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

					foreach (var c in toCreate)
						s.Save(c);
					foreach (var task in tasks)
						s.Update(task);
					tx.Commit();
					s.Flush();
				}
			}

			_TaskAccessor.UpdateScorecard(now);

			return ServerUtility.RegisterCacheEntry();
		}
	}
}