using System.EnterpriseServices;
using System.Text;
using log4net.Repository.Hierarchy;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
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
		public async Task<JsonResult> ChargeAccount(long id, long taskId)
		{
			PaymentException capturedPaymentException = null;
			Exception capturedException = null;
			try{
				await PaymentAccessor.ChargeOrganization(id, taskId, false);
			}catch (PaymentException e){
				capturedPaymentException = e;
            } catch (FallthroughException e) {
                log.Error("FallthroughException", e);
                Response.StatusCode = 501;
                var type = PaymentExceptionType.Fallthrough;
                if (capturedPaymentException != null)
                    type = capturedPaymentException.Type;
                return Json(new {
                    charged = false,
                    payment_exception = true,
                    error = type,
                    message = e.NotNull(x=>x.Message)??"Exception was null"
                }, JsonRequestBehavior.AllowGet);
            } catch (Exception e) {
				capturedException = e;
            }

			if (capturedPaymentException != null){

				try{
					using (var s = HibernateSession.GetCurrentSession())
					{
						using (var tx = s.BeginTransaction()){
							s.Save(PaymentErrorLog.Create(capturedPaymentException, taskId));
							tx.Commit();
							s.Flush();
						}
					}
				}catch (Exception e){
					log.Error("FatalPaymentException", e);
				}
				log.Error("PaymentException",capturedPaymentException);
				try{
					var orgName = capturedPaymentException.OrganizationName + "(" + capturedPaymentException.OrganizationId + ")";
					var trace = capturedPaymentException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
					var email = Mail.To(EmailTypes.PaymentException,ProductStrings.PaymentExceptionEmail)
						.Subject(EmailStrings.PaymentException_Subject, orgName)
						.Body(EmailStrings.PaymentException_Body,capturedPaymentException.Message,"<b>" + capturedPaymentException.Type + "</b> for '" + orgName + "'  ($" + capturedPaymentException.ChargeAmount + ") at " + capturedPaymentException.OccurredAt + " [TaskId=" + taskId + "]", trace);

					await Emailer.SendEmail(email, true);
				}
				catch (Exception e){
					log.Error("FatalPaymentException1",e);
				}
				Response.StatusCode = 501;
				return Json(new{
					charged=false,
					payment_exception = true,
					error=capturedPaymentException.Type
				},JsonRequestBehavior.AllowGet);
			}
			if (capturedException != null)
			{
				log.Error("Exception during Payment", capturedException);
				try{
					var trace = capturedException.StackTrace.NotNull(x => x.Replace("\n", "</br>"));
					var email = Mail.To(EmailTypes.PaymentException, ProductStrings.ErrorEmail)
						.Subject(EmailStrings.PaymentException_Subject, "<Non-payment exception>")
						.Body(EmailStrings.PaymentException_Body, capturedException.NotNull(x=>x.Message), "<Non-payment>", trace);

					await Emailer.SendEmail(email, true);
				}
				catch (Exception e)
				{
					log.Error("FatalPaymentException2", e);
				}
				Response.StatusCode = 500;
				return Json(new
				{
					charged = false,
					payment_exception = false,
					error = capturedException.NotNull(x => x.Message)
				}, JsonRequestBehavior.AllowGet);
			}
			return Json(new{
				charged=true
			},JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Any)]
		public async Task<string> EmailTodos(int currentTime)
		{
			var unsent = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{

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
						rangeHigh = rangeHigh.AddDays(1);



					var todos = s.QueryOver<TodoModel>().Where(x => ((rangeLow <= x.DueDate && x.DueDate <= rangeHigh) || (x.CompleteTime == null && x.DueDate <= nextWeek)) && x.DeleteTime == null).List().ToList();

					var dictionary = new Dictionary<string, List<TodoModel>>();

					foreach (var t in todos.GroupBy(x => x.AccountableUser.NotNull(y => y.User.NotNull(z => z.Email))))
					{
						if (t.Key != null)
						{
							dictionary.GetOrAddDefault(t.Key, x => new List<TodoModel>()).AddRange(t);
						}
					}

					foreach (var userTodos in dictionary)
					{
						string subject = null;
						var nowLocal = userTodos.Value.First().Organization.ConvertFromUTC(nowUtc).Date;

						var overDue = userTodos.Value.Count(x => x.DueDate.Date <= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);
						if (overDue == 1)
							subject = "You have an overdue to-do";
						else if (overDue > 1)
							subject = "You have " + overDue + " overdue to-dos";
						else
						{
							var dueToday = userTodos.Value.Count(x => x.DueDate.Date == nowLocal.Date && x.CompleteTime == null);

							if (dueToday == 1)
								subject = "You have a to-do due today";
							else if (dueToday > 1)
								subject = "You have " + dueToday + " to-dos due today";
							else
							{
								var dueTomorrow = userTodos.Value.Count(x => x.DueDate.Date == nowLocal.AddDays(1).Date && x.CompleteTime == null);
								if (dueTomorrow == 1)
									subject = "You have a to-do due tomorrow";
								else if (dueTomorrow > 1)
									subject = "You have " + dueTomorrow + " to-dos due tomorrow";
								else
								{
									var dueSoon = userTodos.Value.Count(x => x.DueDate.Date > nowLocal.AddDays(1).Date && x.CompleteTime == null);
									if (dueSoon == 1)
										subject = "You have a to-do due soon";
									else if (dueSoon > 1)
										subject = "You have " + dueSoon + " to-dos due soon";
								}
							}
						}


						var shouldSend = userTodos.Value.Count(x => x.DueDate.Date >= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);

						if (subject != null && shouldSend > 0)
						{

							try
							{
								var user = userTodos.Value.First().AccountableUser;

								if ((user.User.NotNull(x => x.SendTodoTime)) == currentTime)
								{
									var email = user.GetEmail();

									var builder = new StringBuilder();
									foreach (var t in userTodos.Value.Where(x => x.CompleteTime == null || x.DueDate.Date > nowUtc.Date).GroupBy(x => x.ForRecurrenceId)){
										var table = await TodoAccessor.BuildTodoTable(t.ToList(), t.First().ForRecurrence.NotNull(x => x.Name + " To-do"));
										builder.Append(table);
										builder.Append("<br/>");
									}

									var mail = Mail.To(EmailTypes.DailyTodo,email)
										.Subject(EmailStrings.TodoReminder_Subject, subject)
										.Body(EmailStrings.TodoReminder_Body,
											user.GetName(),
											builder.ToString(),
											Config.ProductName(user.Organization),
											Config.BaseUrl(user.Organization) + "Todo/List"
										);

									unsent.Add(mail);
								}
							}
							catch (Exception ex)
							{

							}
						}
					}
					tx.Commit();
					s.Flush();
				}
			}
			try
			{
				await Emailer.SendEmails(unsent);
				return "sent: " + unsent.Count;
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}


		[Access(AccessLevel.Any)]
		public async Task<bool> Daily()
		{
			var any = false;
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).List().ToList();

					var tomorrow = DateTime.UtcNow.Date.AddDays(7);
					foreach (var o in orgs){
						var o1 = o;
						var period = s.QueryOver<PeriodModel>().Where(x => x.OrganizationId == o1.Id && x.DeleteTime == null&& x.StartTime <= tomorrow && tomorrow <  x.EndTime ).List().ToList();

						if (!period.Any()){

							var startOfYear = (int) o.Settings.StartOfYearMonth;

							if (startOfYear == 0)
								startOfYear = 1;

							var start = new DateTime(tomorrow.Year - 2, startOfYear, 1);

							//var curM = (int)o.Settings.StartOfYearMonth;
							//var curY = tomorrow.Year;
							//var last = 
							var quarter = 0;
							var prev = start;
							while (true){
								start=start.AddMonths(3);
								quarter += 1;
								var tick = start.AddDateOffset(o.Settings.StartOfYearOffset);
								if (tick > tomorrow){
									break;
								}
								prev = start;
							}

							var p = new PeriodModel(){
								StartTime = prev.AddDateOffset(o.Settings.StartOfYearOffset),
								EndTime = start.AddDateOffset(o.Settings.StartOfYearOffset).AddDays(-1),
								Name = prev.AddDateOffset(o.Settings.StartOfYearOffset).Year + " Q" + (((quarter+3)%4) + 1),// +3 same as -1
								Organization = o,
								OrganizationId = o.Id,
							};

							s.Save(p);
							any = true;
						}

						
					}

					tx.Commit();
					s.Flush();
				}
			}
			return any;
		}

		[Access(AccessLevel.Any)]
        [AsyncTimeout(60000*20)]//20 minutes..
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

			var o = true;//ServerUtility.RegisterCacheEntry();

			return o;
		}
	}
}