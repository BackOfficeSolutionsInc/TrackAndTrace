using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Application;
using RadialReview.Models.Payments;
using RadialReview.Models.Periods;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using RadialReview.Models.Json;
using NHibernate;
using System.Threading;
using RadialReview.Hooks;
using RadialReview.Utilities.Hooks;
using RadialReview.Crosscutting.EventAnalyzers.Interfaces;
using RadialReview.Crosscutting.ScheduledJobs;

namespace RadialReview.Controllers {

	/// <summary>
	/// DO NOT RENAME CONTROLLER. ChargeAccount is needed under Scheduler
	/// </summary>
	public class SchedulerController : BaseController {
		//
		// GET: /Scheduler/
		[Access(AccessLevel.Any)]
		public bool Index() {
			return true;
		}

		[Access(AccessLevel.Radial)]
		[AsyncTimeout(130000)]
		public async Task<ActionResult> Wait(CancellationToken ct, bool with = true) {
			//	HttpContext.Server.ScriptTimeout = 130;
			if (with) {
				//get the Report Generation Timeout
				//int reportTimeout = Int32.Parse(ConfigurationManager.AppSettings["reportTimeout"]);
				//

				//if (itemsToShow == null)
				//	itemsToShow = 5;
				////var results = _imageReportService.GetAllReports().OrderByDescending(x => x.Id);
				//var results = _imageReportService.GetLastXReports((int)itemsToShow).OrderByDescending(x => x.Id);

				//var model = _mapper.Map<IEnumerable<ImageReport>, IEnumerable<ImageReportViewModel>>(results);
				//ViewBag.NumberOfImagesToReport = _imageReportService.GetNumberOfImageItemsAwaitingReporting();
				//return View(model);

				//System.Web.HttpContext.Current.GetType().GetField("_timeoutState", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(System.Web.HttpContext.Current, 1);
			}
			await Task.Delay((int)(120000));
			return Content("reached " + DateTime.UtcNow.ToJsMs());
		}


		public class ChargeAccountResult {
			public ChargeAccountResult(bool isRunning, bool? wasPayment_exception, string errorType, string statusMessage) {
				running = isRunning;
				error = errorType;
				message = statusMessage;
				payment_exception = wasPayment_exception;
			}

			public string job_id { get; set; }
			public bool running { get; private set; }
			public string error { get; private set; }
			public string message { get; private set; }
			public bool? payment_exception { get; private set; }
		}

		/// <summary>
		/// Do not change controller name. 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="taskId"></param>
		/// <returns></returns>
		[Access(AccessLevel.Any)]
		[AsyncTimeout(20 * 60 * 1000)]
		public async Task<JsonResult> ChargeAccount(CancellationToken ct, long id, long taskId) {
			var organizationId = id;
			try {

				var jobId = await PaymentAccessor.EnqueueChargeOrganizationFromTask(organizationId, taskId, false);
				log.Info("ChargingOrganizationEnqueued(" + organizationId + ")");
				return Json(new ChargeAccountResult(true, null, null, null) {
					job_id = jobId,
				}, JsonRequestBehavior.AllowGet);
			} catch (PaymentException paymentException) {
				Response.StatusCode = 501;
				await PaymentAccessor.Unsafe.RecordCapturedPaymentException(paymentException, taskId);
				return Json(new ChargeAccountResult(false, true, "" + paymentException.Type, null), JsonRequestBehavior.AllowGet);
			} catch (FallthroughException fallthroughException) {
				log.Error("FallthroughCaptured", fallthroughException);
				return Json(new ChargeAccountResult(false, true, "" + PaymentExceptionType.Fallthrough, fallthroughException.NotNull(x => x.Message) ?? "no-details"), JsonRequestBehavior.AllowGet);
			} catch (Exception unknownException) {
				Response.StatusCode = 500;
				await PaymentAccessor.Unsafe.RecordUnknownPaymentException(unknownException, organizationId, taskId);
				return Json(new ChargeAccountResult(false, false, unknownException.NotNull(x => x.Message) ?? "no-details", null), JsonRequestBehavior.AllowGet);
			}		
		}



		[Access(AccessLevel.Any)]
		[AsyncTimeout(60 * 60 * 1000)]
		public async Task<ActionResult> EmailTodos(int currentTime, CancellationToken ct, int divisor = 13, int remainder = 0, int sent = 0, string error = null, double duration = 0, bool mockEmails = false) {
			if (remainder >= divisor) {
				return Content("Sent:" + sent + "<br/>Duration:" + duration + "s" + (mockEmails ? "<br/>mocked" : ""));
			}
			var start = DateTime.UtcNow;

			if (divisor <= 0)
				divisor = 1;

			var unsent = new List<Mail>();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var nowUtc = DateTime.UtcNow;
					if (!mockEmails && (nowUtc.DayOfWeek == DayOfWeek.Saturday || nowUtc.DayOfWeek == DayOfWeek.Sunday))
						return Content("No fire on weekend.");

					var started = s.QueryOver<ScheduledTask>().Where(x => x.TaskName == ApplicationAccessor.DAILY_EMAIL_TODO_TASK && x.Started != null).List().ToList();
					if (!started.Any())
						if (!mockEmails && !Config.IsLocal())
							throw new PermissionsException("Task not started");

					await TodoEmailsScheduler.TodoEmailHelpers._ConstructTodoEmails(currentTime, unsent, s, nowUtc, divisor, remainder);

					tx.Commit();
					s.Flush();
				}
			}
			try {
				log.Info("EmailTodos sending " + unsent.Count + " emails.");
				if (!mockEmails && Config.IsLocal()) {
					await Task.Delay(6000);// 100 * unsent.Count);
				} else if (mockEmails) {
					await Task.Delay(300 * unsent.Count);
				} else {
					await Emailer.SendEmails(unsent);
				}
				sent += unsent.Count;

			} catch (Exception e) {
				log.Error("EmailTodos (" + divisor + "," + remainder + ") error.", e);
				error = error ?? "";
				error += " | " + e.Message;
			}
			duration += (DateTime.UtcNow - start).TotalSeconds;

			//Give some other requests a chance to go.
			//await Task.Delay(1500);

			return RedirectToAction("EmailTodos", new {
				currentTime = currentTime,
				divisor = divisor,
				remainder = remainder + 1,
				sent = sent,
				duration = duration,
				mockEmails = mockEmails
			});
		}




		/// <summary>
		/// 
		/// </summary>
		/// <param name="frequency"></param>
		/// <returns></returns>
		[Access(AccessLevel.Any)]
		//DO NOT RENAME
		[AsyncTimeout(60 * 60 * 1000)]
		public async Task<bool> ExecuteEvents(EventFrequency frequency, long taskId) {
			await EventAccessor.ExecuteAll(frequency, taskId, DateTime.UtcNow);
			return true;
		}



		[Access(AccessLevel.Any)]
		public async Task<bool> Daily() {
			var any = false;
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var orgs = s.QueryOver<OrganizationModel>().Where(x => x.DeleteTime == null).List().ToList();

					var tomorrow = DateTime.UtcNow.Date.AddDays(7);
					foreach (var o in orgs) {
						var o1 = o;
						var period = s.QueryOver<PeriodModel>().Where(x => x.OrganizationId == o1.Id && x.DeleteTime == null && x.StartTime <= tomorrow && tomorrow < x.EndTime).List().ToList();

						if (!period.Any()) {

							var startOfYear = (int)o.Settings.StartOfYearMonth;

							if (startOfYear == 0)
								startOfYear = 1;

							var start = new DateTime(tomorrow.Year - 2, startOfYear, 1);

							//var curM = (int)o.Settings.StartOfYearMonth;
							//var curY = tomorrow.Year;
							//var last = 
							var quarter = 0;
							var prev = start;
							while (true) {
								start = start.AddMonths(3);
								quarter += 1;
								var tick = start.AddDateOffset(o.Settings.StartOfYearOffset);
								if (tick > tomorrow) {
									break;
								}
								prev = start;
							}

							var p = new PeriodModel() {
								StartTime = prev.AddDateOffset(o.Settings.StartOfYearOffset),
								EndTime = start.AddDateOffset(o.Settings.StartOfYearOffset).AddDays(-1),
								Name = prev.AddDateOffset(o.Settings.StartOfYearOffset).Year + " Q" + (((quarter + 3) % 4) + 1),// +3 same as -1
								Organization = o,
								OrganizationId = o.Id,
							};

							s.Save(p);
							any = true;
						}
					}


					#region Cleanup Sync model
					try {
						{
							var syncTable = "Sync";
							s.CreateSQLQuery("delete from " + syncTable + " where CreateTime < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"")
							 .ExecuteUpdate();
						}
						{
							var syncTable = "SyncLock";
							s.CreateSQLQuery("delete from " + syncTable + " where LastUpdateDb < \"" + DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd") + "\"")
							 .ExecuteUpdate();
						}
					} catch (Exception e) {
						log.Error(e);
					}
					#endregion

					await EventUtil.GenerateAllDailyEvents(s, DateTime.UtcNow);

					await CheckCardExpirations(s);

					tx.Commit();
					s.Flush();
				}
			}
			return any;
		}

		private async Task CheckCardExpirations(ISession s) {
			var date = DateTime.UtcNow.Date;
			if (date == new DateTime(date.Year, date.Month, 1) || date == new DateTime(date.Year, date.Month, 15) || date == new DateTime(date.Year, date.Month, 21)) {
				var expireMonth = date.AddMonths(1);
				var tokens = s.QueryOver<PaymentSpringsToken>()
						.Where(x => x.Active && x.DeleteTime == null && x.TokenType == PaymentSpringTokenType.CreditCard && x.MonthExpire == expireMonth.Month && x.YearExpire == expireMonth.Year)
						.List().ToList();

				var tt = tokens.GroupBy(x => x.OrganizationId).Select(x => x.OrderByDescending(y => y.CreateTime).First());
				foreach (var t in tt)
					await HooksRegistry.Each<IPaymentHook>((ses, x) => x.CardExpiresSoon(ses, t));

			}
		}

		[Access(AccessLevel.Any)]
		[AsyncTimeout(60000 * 30)]//30 minutes..
		public async Task<JsonResult> Reschedule(CancellationToken ct) {
			//HttpContext.Server.ScriptTimeout = 20*60; // Twenty minutes..
			var res = await TaskAccessor.ExecuteTasks();
			return Json(res, JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.Any)]
		public async Task<JsonResult> Trigger(long id, EventType @event, decimal? arg1 = null) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					if (arg1 == null && @event == EventType.AccountAge_monthly) {
						arg1 = (decimal)((DateTime.UtcNow - s.Get<OrganizationModel>(id).CreationTime).TotalDays / 30.4375);
					}
				}
			}
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					await EventUtil.Trigger(x => x.Create(s, @event, null, id, ForModel.Create<OrganizationModel>(id), arg1: arg1));
					tx.Commit();
					s.Flush();
				}
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
	}

}
