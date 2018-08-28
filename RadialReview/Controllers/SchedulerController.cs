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
using Hangfire;
using RadialReview.Crosscutting.Schedulers;

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
		public async Task<JsonResult> Daily() {
            Scheduler.Enqueue(() => TaskAccessor.DailyTask(DateTime.UtcNow));
            return Json(true, JsonRequestBehavior.AllowGet);
        }

		[Access(AccessLevel.Any)]
		[AsyncTimeout(60000 * 30)]//30 minutes..
		public async Task<JsonResult> Reschedule(CancellationToken ct) {
			//HttpContext.Server.ScriptTimeout = 20*60; // Twenty minutes..
			Scheduler.Enqueue(() => TaskAccessor.ExecuteTasks(DateTime.UtcNow));
			//var res = await TaskAccessor.ExecuteTasks(DateTime.UtcNow);
			return Json(true, JsonRequestBehavior.AllowGet);
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
