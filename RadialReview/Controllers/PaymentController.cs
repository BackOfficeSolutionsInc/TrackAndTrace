using RadialReview.Accessors;
using RadialReview.Models;
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

namespace RadialReview.Controllers
{
	public class PaymentController : BaseController
	{
		[Access(AccessLevel.Any)]
		public ActionResult Index(int? count)
		{
			//GetUserModel();

			if (count == null)
				return RedirectToAction("Index", "Organization");

			ViewBag.Count = count;
			return View();
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult SetCard()
		{
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditCompanyPayment(GetUser().Organization.Id));

			return View();
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult Plan()
		{
			var plan = PaymentAccessor.GetPlan(GetUser(), GetUser().Organization.Id);
			return View(plan);
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan(PaymentPlanModel model)
		{
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly(long? orgid=null)
		{
			var id = orgid ?? GetUser().Organization.Id;

			var plan = PaymentAccessor.GetPlan(GetUser(), id);
			var org = _OrganizationAccessor.GetOrganization(GetUser(), id);
			if (plan is PaymentPlan_Monthly){
				var pr = (PaymentPlan_Monthly) plan;
				pr._Org = org;
				return View(pr);
			}
			return View(new PaymentPlan_Monthly()
			{
                BaselinePrice = 149,
				FirstN_Users_Free = 10,
				L10PricePerPerson = 10,
				ReviewPricePerPerson = 4,
				PlanCreated = DateTime.UtcNow,
				OrgId = id,
				_Org =org
			});
		}


		public class Charge
		{
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
		public ActionResult AllCharges(int id = 7)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var now = DateTime.UtcNow.Subtract(TimeSpan.FromDays(id));

					var items = s.QueryOver<InvoiceModel>().Where(x => x.CreateTime > now).List().ToList();

					var errors = s.QueryOver<PaymentErrorLog>().Where(x => x.HandledAt == null || x.HandledAt > now).List().ToList();

					var allCharges = new List<Charge>();

					allCharges.AddRange(items.Select(x=>new Charge(){
						Amount = x.InvoiceItems.Sum(y=>y.AmountDue),
						CreateTime = x.CreateTime,
						Message =  "<Invoice>",
						Label = x.PaidTime != null?"success":"warning",
						Status = x.PaidTime!=null?"Charged":"Not Charged",
						TransactionId = x.TransactionId,
						Organization = x.Organization.GetName(),
						OrganizationId = x.Organization.Id,
					}));

					allCharges.AddRange(errors.Select(x=>new Charge(){
						Amount = x.Amount,
						Label = "danger",
						Status = "Error",
						CreateTime = x.OccurredAt,
						Message = "<"+x.Type+"> "+x.Message,
						TaskId = x.TaskId,
						Organization = x.OrganizationName,
						OrganizationId = x.OrganizationId,
					}));


					return View(allCharges);
				}
			}
		}




		[Access(AccessLevel.Radial)]
		public ActionResult Errors(int id=7)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var now = DateTime.UtcNow.Subtract(TimeSpan.FromDays(id));

					var items = s.QueryOver<PaymentErrorLog>().Where(x => x.HandledAt == null || x.HandledAt > now).List().ToList();
					return View(items);
				}
			}
		}

		[Access(AccessLevel.Radial)]
		public JsonResult SetErrorHandled(long id,bool handled)
		{
			using(var s = HibernateSession.GetCurrentSession())
			{
				using(var tx=s.BeginTransaction()){
					var e = s.Get<PaymentErrorLog>(id);

					if (handled){
						e.HandledAt = DateTime.UtcNow;
					}
					else{
						e.HandledAt = null;
					}
					tx.Commit();
					s.Flush(); 
				}
			}

			return Json(ResultObject.SilentSuccess(new{
				id=id,
				handled=handled,
			}), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly(PaymentPlan_Monthly model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var org = s.Get<OrganizationModel>(model.OrgId);

					if (String.IsNullOrWhiteSpace(Request.Form["TaskId"]))
					{
						if (!String.IsNullOrWhiteSpace(Request.Form["OldTaskId"])){
							var delete = s.QueryOver<ScheduledTask>().Where(x=>x.DeleteTime==null && x.OriginalTaskId==Request["OldTaskId"].ToLong()).List().ToList();
							foreach (var oldTask in delete){
								oldTask.DeleteTime = DateTime.UtcNow;
								s.Update(oldTask);
							}
							
						}


						var fireTime = DateTime.MaxValue;
						var setDate = false;

						if (model.FreeUntil.Date > DateTime.UtcNow.Date)
						{
							fireTime = Math2.Min(fireTime, model.FreeUntil.Date);
							setDate = true;
						}

						if (model.L10FreeUntil != null)
						{
							fireTime = Math2.Min(fireTime, model.L10FreeUntil.Value.Date);
							setDate = true;
						}

						if (model.ReviewFreeUntil != null)
						{
							fireTime = Math2.Min(fireTime, model.ReviewFreeUntil.Value.Date);
							setDate = true;
						}

						if (setDate == false)
							fireTime = DateTime.UtcNow.Date;

						fireTime = Math2.Max(DateTime.UtcNow.Date, fireTime);



						model.Task = new ScheduledTask()
						{
							MaxException = 1,
							Url = "/Scheduler/ChargeAccount/" + model.OrgId,
							NextSchedule = model.SchedulerPeriod(),
							Fire = fireTime,
							FirstFire = fireTime,
							TaskName = ScheduledTask.MonthlyPaymentPlan,
						};
						s.Save(model.Task);
						model.Task.OriginalTaskId = model.Task.Id;
						model.Task.CreatedFromTaskId = model.Task.Id;
						s.Update(model.Task);

					}
					else
					{
						model.Task = s.Get<ScheduledTask>(Request["TaskId"].ToLong());
					}



					org.PaymentPlan = model;

					s.Update(org);

					tx.Commit();
					s.Flush();

                    model._Org = org;

					return View(model);
				}
			}
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> SetCard(bool submit)
		{
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

		public ActionResult EditPlan(long id)
		{
			throw new NotImplementedException();
		}
	}
}