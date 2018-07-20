using NHibernate;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Payments;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TrelloNet;

namespace RadialReview.Controllers {
	public class PaymentController : BaseController {
		[Access(AccessLevel.Any)]
		public ActionResult Index(int? count) {
			//GetUserModel();

			if (count == null)
				return RedirectToAction("Index", "Organization");

			ViewBag.Count = count;
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult SetCard() {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditCompanyPayment(GetUser().Organization.Id));

			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult SetACH() {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditCompanyPayment(GetUser().Organization.Id));

			return View();
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Plan() {
			var plan = PaymentAccessor.GetPlan(GetUser(), GetUser().Organization.Id);
			return View(plan);
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan(PaymentPlanModel model) {
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly(long? orgid = null, bool msg = false) {
			var id = orgid ?? GetUser().Organization.Id;

			AllowAdminsWithoutAudit();

			var plan = PaymentAccessor.GetPlan(GetUser(), id);
			var org = _OrganizationAccessor.GetOrganization(GetUser(), id);
			ViewBag.ShowPostMsg = msg;
			if (plan is PaymentPlan_Monthly) {
				var pr = (PaymentPlan_Monthly)plan;
				pr._Org = org;
				return View(pr);
			}
			return View(new PaymentPlan_Monthly() {
				BaselinePrice = 149,
				FirstN_Users_Free = 10,
				L10PricePerPerson = 10,
				ReviewPricePerPerson = 2,
				PlanCreated = DateTime.UtcNow,
				SchedulePeriod = SchedulePeriodType.Monthly,
				OrgId = id,
				_Org = org
			});
		}

		[Access(AccessLevel.Radial)]
		public JsonResult FreeUntil(long id, DateTime date) {

			var plan = PaymentAccessor.GetPlan(GetUser(), id);
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(id);
					var model = s.Get<PaymentPlan_Monthly>(org.PaymentPlan.Id);

					DateTime? originalScheduledFire = null;
					DateTime? lastFire = null;
					if (model == null)
						throw new PermissionsException("Payment plan doesnt exist");

					if (model != null && model.Task != null) {
						var originalTaskId = model.Task.OriginalTaskId;
						_DeleteOldTasks(s, originalTaskId, ref lastFire, out originalScheduledFire);
						//var delete = s.QueryOver<ScheduledTask>().Where(x => x.DeleteTime == null && x.OriginalTaskId == model.Task.OriginalTaskId).List().ToList();
						//foreach (var oldTask in delete) {
						//	oldTask.DeleteTime = DateTime.UtcNow;
						//	s.Update(oldTask);
						//}
					}


					//var fireTime = DateTime.MaxValue;
					//var setDate = false;
					var result = ResultObject.SilentSuccess();
					//adjust the date
					if (model.LastExecuted != null) {
						date = Math2.Max(model.LastExecuted.Value + model.SchedulerPeriod(), date);
						result.Message = "Date must be after last payment execution. Adjusting.";
						result.Silent = false;
						result.Refresh = true;
					}

					//model.FreeUntil = Math2.Max(DateTime.UtcNow, date);


					//if (model.FreeUntil.Date > DateTime.UtcNow.Date) {
					//	fireTime = Math2.Min(fireTime, model.FreeUntil.Date);
					//	setDate = true;
					//}

					//if (model.L10FreeUntil != null) {
					//	fireTime = Math2.Min(fireTime, model.L10FreeUntil.Value.Date);
					//	setDate = true;
					//}

					//if (model.ReviewFreeUntil != null) {
					//	fireTime = Math2.Min(fireTime, model.ReviewFreeUntil.Value.Date);
					//	setDate = true;
					//}

					//if (setDate == false)
					//	fireTime = DateTime.UtcNow.Date;

					//fireTime = Math2.Max(DateTime.UtcNow.Date, fireTime);

					DateTime fireTime = _CalculateFireTime(model, originalScheduledFire, lastFire);


					_SavePaymentTask(s, fireTime, model);
					//model.Task = new ScheduledTask() {
					//	MaxException = 1,
					//	Url = "/Scheduler/ChargeAccount/" + model.OrgId,
					//	NextSchedule = model.SchedulerPeriod(),
					//	Fire = fireTime,
					//	FirstFire = fireTime,
					//	TaskName = ScheduledTask.MonthlyPaymentPlan,
					//	EmailOnException = true,
					//};
					//s.Save(model.Task);
					//model.Task.OriginalTaskId = model.Task.Id;
					//model.Task.CreatedFromTaskId = model.Task.Id;
					//s.Update(model.Task);

					tx.Commit();
					s.Flush();

					return Json(result, JsonRequestBehavior.AllowGet);
				}
			}
		}


		public class Charge {
			public DateTime CreateTime { get; set; }
			public string Status { get; set; }
			public string Label { get; set; }
			public string Message { get; set; }
			public string TransactionId { get; set; }
			public decimal Amount { get; set; }
			public long TaskId { get; set; }
			public long OrganizationId { get; set; }
			public string Organization { get; set; }
		}



		[Access(AccessLevel.Radial)]
		public ActionResult AllCharges(int id = 7) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow.Subtract(TimeSpan.FromDays(id));

					var items = s.QueryOver<InvoiceModel>().Where(x => x.CreateTime > now).List().ToList();

					var errors = s.QueryOver<PaymentErrorLog>().Where(x => x.HandledAt == null || x.HandledAt > now).List().ToList();

					var allCharges = new List<Charge>();

					allCharges.AddRange(items.Select(x => new Charge() {
						Amount = x.InvoiceItems.Sum(y => y.AmountDue),
						CreateTime = x.CreateTime,
						Message = "<Invoice>",
						Label = x.PaidTime != null ? "success" : "warning",
						Status = x.PaidTime != null ? "Charged" : "Not Charged",
						TransactionId = x.TransactionId,
						Organization = x.Organization.GetName(),
						OrganizationId = x.Organization.Id,
					}));

					allCharges.AddRange(errors.Select(x => new Charge() {
						Amount = x.Amount,
						Label = "danger",
						Status = "Error",
						CreateTime = x.OccurredAt,
						Message = "<" + x.Type + "> " + x.Message,
						TaskId = x.TaskId,
						Organization = x.OrganizationName,
						OrganizationId = x.OrganizationId,
					}));


					return View(allCharges);
				}
			}
		}


		[Access(AccessLevel.Radial)]
		public ActionResult Errors(int id = 7) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var now = DateTime.UtcNow.Subtract(TimeSpan.FromDays(id));

					var items = s.QueryOver<PaymentErrorLog>().Where(x => x.HandledAt == null || x.HandledAt > now).List().ToList();
					return View(items);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult SetErrorHandled(long id, bool handled) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var e = s.Get<PaymentErrorLog>(id);

					if (handled) {
						e.HandledAt = DateTime.UtcNow;
					} else {
						e.HandledAt = null;
					}
					tx.Commit();
					s.Flush();
				}
			}

			return Json(ResultObject.SilentSuccess(new {
				id = id,
				handled = handled,
			}), JsonRequestBehavior.AllowGet);
		}

		[HttpGet]
		[Access(AccessLevel.Radial)]
		public JsonResult RemoveCredit(long id) {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, user);
					perms.RadialAdmin();

					var c = s.Get<PaymentCredit>(id);

					if (c.DeleteTime != null)
						throw new PermissionsException("Already deleted");

					c.DeleteTime = DateTime.UtcNow;
					s.Update(c);

					tx.Commit();
					s.Flush();
					return Json(ResultObject.SilentSuccess(),JsonRequestBehavior.AllowGet);
				}
			}
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public JsonResult ApplyCredit(PaymentCredit model) {
			var user = GetUser();
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var perms = PermissionsUtility.Create(s, user);
					perms.RadialAdmin();

					//add creator
					model.CreatedBy = user.Id;
					model.AmountRemaining = model.OriginalAmount;

					var org = s.Get<OrganizationModel>(model.OrgId);
					var credits = s.QueryOver<PaymentCredit>().Where(x => x.OrgId == model.OrgId && x.DeleteTime == null).List().ToList();

					if (org == null)
						throw new PermissionsException("Organization does not exist");
					if (model.OriginalAmount > 1000)
						throw new PermissionsException("Credit must be less than $1000");
					if (credits.Sum(x => x.AmountRemaining) + model.AmountRemaining < 0)
						throw new PermissionsException("Total credits must be zero or greater.");

					s.Save(model);

					tx.Commit();
					s.Flush();
				}
			}

			return Json(ResultObject.SilentSuccess(model));
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly(PaymentPlan_Monthly model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					var org = s.Get<OrganizationModel>(model.OrgId);

					if (String.IsNullOrWhiteSpace(Request.Form["TaskId"])) {
						DateTime? originalScheduledFire = null;
						DateTime? lastFire = null;
						if (!String.IsNullOrWhiteSpace(Request.Form["OldTaskId"])) {
							var originalTaskId = Request["OldTaskId"].ToLong();
							_DeleteOldTasks(s, originalTaskId, ref lastFire, out originalScheduledFire);
						}

						DateTime fireTime = _CalculateFireTime(model, originalScheduledFire, lastFire);
						_SavePaymentTask(s, fireTime, model);

					} else {
						model.Task = s.Get<ScheduledTask>(Request["TaskId"].ToLong());
					}

					org.PaymentPlan = model;

					s.Update(org);
					tx.Commit();
					s.Flush();

					model._Org = org;

					return RedirectToAction("Plan_Monthly", new { msg = true, orgId = org.Id });
				}
			}
		}

		private static void _SavePaymentTask(ISession s, DateTime fireTime, PaymentPlan_Monthly model) {
			model.Task = new ScheduledTask() {
				MaxException = 1,
				Url = "/Scheduler/ChargeAccount/" + model.OrgId,
				NextSchedule = model.SchedulerPeriod(),
				Fire = fireTime,
				FirstFire = fireTime,
				TaskName = ScheduledTask.MonthlyPaymentPlan,
				EmailOnException = true,
			};
			s.Save(model.Task);
			model.Task.OriginalTaskId = model.Task.Id;
			model.Task.CreatedFromTaskId = model.Task.Id;
			s.Update(model.Task);
		}

		private static void _DeleteOldTasks(ISession s, long originalTaskId, ref DateTime? lastFire, out DateTime? originalScheduledFire) {
			var delete = s.QueryOver<ScheduledTask>().Where(x => x.OriginalTaskId == originalTaskId).List().ToList();
			var maxDate = DateTime.MinValue;
			foreach (var oldTask in delete) {
				if (oldTask.DeleteTime == null) {
					oldTask.DeleteTime = DateTime.UtcNow;
					s.Update(oldTask);
				}
				if (oldTask.Executed == null) {
					maxDate = Math2.Max(oldTask.Fire, maxDate);
				}
				if (oldTask.Executed != null && oldTask.Executed < DateTime.UtcNow) {
					lastFire = Math2.Max(lastFire ?? DateTime.MinValue, oldTask.Executed.Value);
				}
			}
			originalScheduledFire = maxDate;
		}

		private DateTime _CalculateFireTime(PaymentPlan_Monthly model, DateTime? originalScheduledFire, DateTime? lastFire) {
			var fireTime = DateTime.MaxValue;
			var setDate = false;


			if (model.FreeUntil.Date > DateTime.UtcNow.Date) {
				fireTime = Math2.Min(fireTime, model.FreeUntil.Date);
				setDate = true;
			}

			if (model.L10FreeUntil != null) {
				fireTime = Math2.Min(fireTime, model.L10FreeUntil.Value.Date);
				setDate = true;
			}

			if (model.ReviewFreeUntil != null) {
				fireTime = Math2.Min(fireTime, model.ReviewFreeUntil.Value.Date);
				setDate = true;
			}

			if (originalScheduledFire != null) {
				fireTime = Math2.Min(fireTime, originalScheduledFire.Value);
				setDate = true;
			}

			if (setDate == false) {
				fireTime = DateTime.UtcNow.Date.AddDays(30);
				ShowAlert("Hey something went very wrong. Fallback date was used. Contact your manager with this information: <br/> {org:" + model.OrgId + ",originalFire:" + model.NotNull(x => x.Task.Fire) + ",newFire:" + fireTime + "}", AlertType.Error);
			}
			if (fireTime < DateTime.UtcNow.Date) {
				fireTime = Math2.Max(fireTime, DateTime.UtcNow.Date);
				ShowAlert("Hey something probably went very wrong. Customer will be charged today! Contact your manager with this information: <br/> {org:" + model.OrgId + ",originalFire:" + model.NotNull(x => x.Task.Fire) + ",newFire:" + fireTime + "}", AlertType.Error);
			}

			var period = model.SchedulePeriod ?? SchedulePeriodType.Monthly;
			if (lastFire != null && lastFire <= fireTime && fireTime < lastFire.Value.Add(period.GetPeriod())) {
				fireTime = lastFire.Value.Add(period.GetPeriod());
				ShowAlert("Hey something probably went wrong. Customer would have been charged less than " + period.GetPeriod().TotalDays + " days after most recent charge! Automatically adjusting charge date. Contact your manager with this information: <br/> {org:" + model.OrgId + ",originalFire:" + model.NotNull(x => x.Task.Fire) + ",newFire:" + fireTime + "}", AlertType.Error);
			}

			return fireTime;
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> SetCard(bool submit) {
			await PaymentAccessor.SetCard(
				GetUser(),
				GetUser().Organization.Id,
				Request.Form["id"],
				Request.Form["class"],
				Request.Form["card_type"],
				Request.Form["card_owner_name"],
				Request.Form["last_4"],
				Request.Form["card_exp_month"].ToInt(),
				Request.Form["card_exp_year"].ToInt(),
				Request.Form["address_1"],
				Request.Form["address_2"],
				Request.Form["city"],
				Request.Form["state"],
				Request.Form["zip"],
				Request.Form["phone"],
				Request.Form["website"],
				Request.Form["country"],
				Request.Form["email"],
				true);

			return RedirectToAction("Advanced", "Manage");
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> SetACH(bool submit) {
			await PaymentAccessor.SetACH(
				GetUser(),
				GetUser().Organization.Id,
				Request.Form["id"],
				Request.Form["class"],
				Request.Form["token_type"],

				Request.Form["bank_account_type"],
				Request.Form["bank_account_holder_first_name"],
				Request.Form["bank_account_holder_last_name"],
				Request.Form["bank_account_number_last_4"],
				Request.Form["bank_routing_number"],
				Request.Form["address_1"],
				Request.Form["address_2"],
				Request.Form["city"],
				Request.Form["state"],
				Request.Form["zip"],
				Request.Form["phone"],
				Request.Form["website"],
				Request.Form["country"],
				Request.Form["email"],
				true);

			return RedirectToAction("Advanced", "Manage");
		}

		public ActionResult EditPlan(long id) {
			throw new NotImplementedException();
		}
	}
}