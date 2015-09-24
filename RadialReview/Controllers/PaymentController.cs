﻿using RadialReview.Models;
using RadialReview.Models.Tasks;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

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
			var plan = _PaymentAccessor.GetPlan(GetUser(), GetUser().Organization.Id);
			return View(plan);
		}

		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan(PaymentPlanModel model)
		{
			return View(model);
		}

		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly()
		{
			var plan = _PaymentAccessor.GetPlan(GetUser(), GetUser().Organization.Id);
			if (plan is PaymentPlan_Monthly)
			{
				return View((PaymentPlan_Monthly)plan);
			}
			return View(new PaymentPlan_Monthly()
			{
				FirstN_Users_Free = 2,
				L10PricePerPerson = 12,
				ReviewPricePerPerson = 4,
				PlanCreated = DateTime.UtcNow,
				OrganizationId = GetUser().Organization.Id,

			});
		}


		[HttpPost]
		[Access(AccessLevel.Radial)]
		public ActionResult Plan_Monthly(PaymentPlan_Monthly model)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction())
				{
					var org = s.Get<OrganizationModel>(model.OrganizationId);

					if (String.IsNullOrWhiteSpace(Request.Form["TaskId"]))
					{

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
							Url = "/Scheduler/ChargeAccount/" + model.OrganizationId,
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
					return View(model);
				}
			}
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<ActionResult> SetCard(bool submit)
		{
			await _PaymentAccessor.SetCard(
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
				true);

			return RedirectToAction("Advanced", "Manage");
		}

		public ActionResult EditPlan(long id)
		{
			throw new NotImplementedException();
		}
	}
}