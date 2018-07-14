using Hangfire;
using Hangfire.Storage;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.SqlCommand;
using RadialReview.Accessors;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Todo;
using RadialReview.Properties;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static RadialReview.Accessors.TodoAccessor;
using RadialReview.Models.Scorecard;

namespace RadialReview.Crosscutting.ScheduledJobs {
	public class TodoEmailsScheduler{


		//[AutomaticRetry(Attempts = 0)]
		//public async Task SendEmail(int currentTime) {
		//	var divisor = VariableAccessor.Get(Variable.Names.TODO_DIVISOR, ()=>43);
		//	for (var i = 0; i < divisor; i++) {
		//		//Que up all the emailers
		//		BackgroundJob.Enqueue(() => SendEmail(currentTime, divisor, i));
		//	}
		//}

		//[AutomaticRetry(Attempts = 0)]
		//public async Task SendEmail(int currentTime, int divisor, int remainder) {
		//	_ConstructTodoEmails(currentTime,
		//}


		public class TodoEmailHelpers {
			/// <summary>
			/// remainder < divisor
			/// </summary>
			/// <param name="currentTime"></param>
			/// <param name="unsent"></param>
			/// <param name="s"></param>
			/// <param name="nowUtc"></param>
			/// <param name="divisor"></param>
			/// <param name="remainder"></param>
			/// <returns></returns>
			public static async Task _ConstructTodoEmails(int currentTime, List<Mail> unsent, ISession s, DateTime nowUtc, int divisor, int remainder) {
				var tomorrow = nowUtc.Date.AddDays(2).AddTicks(-1);
				var rangeLow = nowUtc.Date.AddDays(-1);
				var rangeHigh = nowUtc.Date.AddDays(4).AddTicks(-1);
				var nextWeek = nowUtc.Date.AddDays(7);
				if (nowUtc.DayOfWeek == DayOfWeek.Friday)
					rangeHigh = rangeHigh.AddDays(1);

				var todos = _QueryTodoModulo(s, currentTime, divisor, remainder, rangeLow, rangeHigh, nextWeek);

				var dictionary = new Dictionary<string, List<TinySchedulerTodo>>();
				foreach (var t in todos.Where(x=>x.AccountableUserEmail!=null).GroupBy(x => x.AccountableUserEmail)) {
					if (t.Key != null) {
						dictionary.GetOrAddDefault(t.Key, x => new List<TinySchedulerTodo>()).AddRange(t);
					}
				}

				foreach (var userTodos in dictionary) {
					await _ConstructTodoEmail(currentTime, unsent, nowUtc, userTodos.Value);
				}
			}

			public class TinySchedulerTodo : ITodoTiny {
				public TinySchedulerTodo(long id, string message, string accountableUserEmail, DateTime dueDate, DateTime? completeTime, string originName, long? originId, string padId, long accountableUserId, int? sendTimeKey, string accountableUserName, int accountableUserTimezoneOffset, string accountableUserDateFormat) {
					Id = id;
					Message = message;
					AccountableUserEmail = accountableUserEmail;
					DueDate = dueDate;
					CompleteTime = completeTime;
					OriginName = originName;
					OriginId = originId;
					PadId = padId;
					AccountableUserId = accountableUserId;
					SendTime = sendTimeKey;
					AccountableUserName = accountableUserName;
					AccountableUserTimezoneOffset = accountableUserTimezoneOffset;
					AccountableUserDateFormat = accountableUserDateFormat;
				}

				public long Id { get; set; }
				public string Message { get; set; }
				public string AccountableUserEmail { get; set; }
				public DateTime DueDate { get; set; }
				public DateTime? CompleteTime { get; set; }
				public string OriginName { get; set; }
				public long? OriginId { get; set; }
				public string PadId { get; set; }
				public long AccountableUserId { get; set; }
				public int? SendTime { get; set; }

				public string AccountableUserName { get; set; }

				public int AccountableUserTimezoneOffset { get; set; }
				public string AccountableUserDateFormat { get; set; }


			}


			public static List<TinySchedulerTodo> _QueryTodoModulo(ISession s, int currentTime, long divisor, long remainder, DateTime rangeLow, DateTime rangeHigh, DateTime nextWeek) {

				UserOrganizationModel accUser = null;
				UserModel userModel = null;
				OrganizationModel org = null;

				var query =  s.QueryOver<TodoModel>()
								.JoinAlias(x => x.AccountableUser, () => accUser, JoinType.LeftOuterJoin)
								.JoinAlias(x => accUser.User, () => userModel, JoinType.LeftOuterJoin)
								.JoinAlias(x => accUser.Organization, () => org, JoinType.LeftOuterJoin)
								.Where(x => ((rangeLow <= x.DueDate && x.DueDate <= rangeHigh) || (x.CompleteTime == null && x.DueDate <= nextWeek)) && x.DeleteTime == null)
								.Where(Restrictions.Eq(Projections.SqlFunction("mod", NHibernateUtil.Int64, Projections.Property<TodoModel>(x => x.AccountableUserId), Projections.Constant(divisor)), remainder));
				//query = query.Where(Restrictions.On(() => accUser).IsNotNull);
				//query = query.Where(Restrictions.On(() => userModel).IsNotNull);
				query = query.Where(x => userModel.SendTodoTime == currentTime);
				query = query.Select(
					x => x.Id, x => x.Message, x => userModel.UserName,
					x => x.DueDate, x => x.CompleteTime, x => x.ForRecurrenceId,
					x => x.PadId, x => x.AccountableUser.Id, x => org._Settings.TimeZoneId,
					x => userModel.FirstName, x => userModel.LastName, x => org._Settings.DateFormat,
					x => userModel.SendTodoTime
				);

				return query.List<object[]>().Select(x => {
					var id = (long)x[0];
					var message = (string)x[1];
					var accountableUserEmail = (string)x[2];
					var dueDate = (DateTime)x[3];
					var completeTime = (DateTime?)x[4];
					var forRecurrenceId = (long?)x[5];
					var padId = (string)x[6];
					var accountableUserId = (long)x[7];
					var timeZoneOffset = TimeData.GetTimezoneOffset((string)x[8]);
					var userFirstName = (string)x[9];
					var userLastName = (string)x[10];
					var dateFormat = (string)x[11];
					var sendTimeKey = (int?)x[12];

					var userFirstLastName = ((userFirstName + " " + userLastName) ?? "").Trim();

					return new TinySchedulerTodo(
						id,message,accountableUserEmail,dueDate,completeTime,null/*TODO get origin name*/,
						forRecurrenceId,padId,accountableUserId, sendTimeKey, userFirstLastName,
						timeZoneOffset,dateFormat
					);
				}).ToList();
			}

			public static async Task _ConstructTodoEmail(int currentTime, List<Mail> unsent, DateTime nowUtc, List<TinySchedulerTodo> userTodos) {
				string subject = null;
				var nowLocal = TimeData.ConvertFromServerTime(nowUtc, userTodos.FirstOrDefault().NotNull(x=>x.AccountableUserTimezoneOffset));// userTodos.First().Organization.ConvertFromUTC(nowUtc).Date;

				var overDue = userTodos.Count(x => x.DueDate.Date <= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);
				if (overDue == 1)
					subject = "You have an overdue to-do";
				else if (overDue > 1)
					subject = "You have " + overDue + " overdue to-dos";
				else {
					var dueToday = userTodos.Count(x => x.DueDate.Date == nowLocal.Date && x.CompleteTime == null);

					if (dueToday == 1)
						subject = "You have a to-do due today";
					else if (dueToday > 1)
						subject = "You have " + dueToday + " to-dos due today";
					else {
						var dueTomorrow = userTodos.Count(x => x.DueDate.Date == nowLocal.AddDays(1).Date && x.CompleteTime == null);
						if (dueTomorrow == 1)
							subject = "You have a to-do due tomorrow";
						else if (dueTomorrow > 1)
							subject = "You have " + dueTomorrow + " to-dos due tomorrow";
						else {
							var dueSoon = userTodos.Count(x => x.DueDate.Date > nowLocal.AddDays(1).Date && x.CompleteTime == null);
							if (dueSoon == 1)
								subject = "You have a to-do due soon";
							else if (dueSoon > 1)
								subject = "You have " + dueSoon + " to-dos due soon";
						}
					}
				}

				var shouldSend = userTodos.Count(x => x.DueDate.Date >= nowLocal.Date.AddDays(-1) && x.CompleteTime == null);

				if (subject != null && shouldSend > 0) {

					try {
						//var user = userTodos.First().AccountableUser;

						if (userTodos.First().SendTime == currentTime) {
							var first = userTodos.First();
							var email = first.AccountableUserEmail;
							var name = first.AccountableUserName;
							var tzOffset = first.AccountableUserTimezoneOffset;
							var dateFormat = first.AccountableUserDateFormat;

							var builder = new StringBuilder();
							foreach (var t in userTodos.Where(x => x.CompleteTime == null || x.DueDate.Date > nowUtc.Date).GroupBy(x => x.OriginId)) {
								var table = await TodoAccessor.BuildTodoTable(t.Cast<ITodoTiny>().ToList(),tzOffset,dateFormat, t.First().OriginName.NotNull(x => x + " To-do"));
								builder.Append(table);
								builder.Append("<br/>");
							}

							var mail = Mail.To(EmailTypes.DailyTodo, email)
								.Subject(EmailStrings.TodoReminder_Subject, subject)
								.Body(EmailStrings.TodoReminder_Body,
									name,
									builder.ToString(),
									Config.ProductName(null),
									Config.BaseUrl(null) + "Todo/List"
								);

							unsent.Add(mail);
						}
					} catch (Exception) {
						int a = 0;
					}
				}
			}
		}

	}
}