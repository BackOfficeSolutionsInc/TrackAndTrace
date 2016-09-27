using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using FluentNHibernate.Conventions;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class TwilioApiController : BaseController
    {
		// GET: TwilioApi
		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index()
		{
			var actions=PhoneAccessor.GetAllPhoneActionsForUser(GetUser(),GetUser().Id);

			return View(actions);
		}

	    public class PhoneVM 
	    {
		    public List<SelectListItem> PossibleNumbers {get;set;}
			public List<SelectListItem> PossibleActions { get; set; }
			public string SelectedAction { get; set; }
			public string SelectedNumber { get; set; }
			public long RecurrenceId { get; set; }
			public bool TodoOnly { get; internal set; }
		}

		protected static List<SelectListItem> PossibleActions = new List<SelectListItem>(){
			new SelectListItem(){Text = "Add an Issue", Value = PhoneAccessor.ISSUE },
			new SelectListItem(){Text = "Add a To-Do", Value = PhoneAccessor.TODO },
			new SelectListItem(){Text = "Add a People Headline", Value = PhoneAccessor.HEADLINE },
		};

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult Modal(long recurrenceId,bool todoOnly=false)
		{
			if (recurrenceId != -2) {
				//Personal Todo
				new PermissionsAccessor().Permitted(GetUser(), x => x.ViewL10Recurrence(recurrenceId));
			}

			var posActions = PossibleActions.ToList();
			if (todoOnly)
				posActions = posActions.Where(x => x.Value == PhoneAccessor.TODO).ToList();

			var model = new PhoneVM()
			{
				RecurrenceId = recurrenceId,
				PossibleActions = posActions,
				PossibleNumbers = PhoneAccessor.GetUnusedCallablePhoneNumbersForUser(GetUser(), GetUser().Id).ToSelectList(x => x.Number.ToPhoneNumber(), x => x.Id),
				TodoOnly= todoOnly
			};

			return PartialView(model);
		}

		[Access(AccessLevel.UserOrganization)]
        public PartialViewResult ModalRecurrence()
		{
			var meetings = L10Accessor.GetVisibleL10Recurrences(GetUser(), GetUser().Id, false);

			if (!meetings.Any()){
				throw new PermissionsException("You are not connected to any meetings.");
			}

			var model = new PhoneVM()
			{
				RecurrenceId = 0,
				PossibleActions = PossibleActions,
				PossibleNumbers = PhoneAccessor.GetUnusedCallablePhoneNumbersForUser(GetUser(), GetUser().Id).ToSelectList(x => x.Number.ToPhoneNumber(), x => x.Id)
			};
			ViewBag.PossibleRecurrences = meetings.ToSelectList(x=>x.Recurrence.Name,x=>x.Recurrence.Id);


			return PartialView("Modal",model);
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult Modal(PhoneVM model)
		{
			//ValidateValues(model,x=>x.RecurrenceId);
			if (model.RecurrenceId != -2) {
				//Personal Todo
				new PermissionsAccessor().Permitted(GetUser(), x => x.ViewL10Recurrence(model.RecurrenceId));
			}
			if(PossibleActions.All(x => x.Value != model.SelectedAction))
				throw new PermissionsException("Action does not exist.");
			var code = PhoneAccessor.AddAction(GetUser(),GetUser().Id, model.SelectedAction, model.SelectedNumber.ToLong(),model.RecurrenceId);
			var phone = code.PhoneNumber.ToPhoneNumber();
			return Json(ResultObject.Success("Text '" + code.Code + "' to " + phone+" to activate."));
		}



		[Access(AccessLevel.UserOrganization)]
	    public JsonResult Delete(long id)
	    {
		    PhoneAccessor.DeleteAction(GetUser(), id);
			return Json(ResultObject.SilentSuccess());
	    }

	
		[Access(AccessLevel.Any)]
		public async Task<ContentResult> ReceiveText_53B006C3B7ED45C58EE31DBFA85D75BA()
		{
			try{
				return PhoneContent(await PhoneAccessor.ReceiveText(
					Request["From"].ToLong(),
					Request["Body"],
					Request["To"].ToLong())
				);
			}
			catch (PhoneException e){
				return PhoneContent(e.Message);
			}
			catch (Exception e){
				var error = "We're sorry, this service is unavailable at this time.";
				error += e.Message;
				return Content("<Response><Sms>"+e+"</Sms></Response>");
			}
		}

	    protected ContentResult PhoneContent(string message){
		    return Content("<Response><Sms>"+message+"</Sms></Response>");
	    }
    }
}