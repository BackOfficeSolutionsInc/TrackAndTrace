using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class TwilioApiController : BaseController
    {
		// GET: TwilioApi
		public ActionResult Index()
		{
			PhoneAccessor.GetAllPhoneActionsForUser(GetUser(),GetUser().Id);

			return View();
		}

	    public class PhoneVM 
	    {
		    public List<SelectListItem> PossibleNumbers {get;set;}
			public List<SelectListItem> PossibleActions { get; set; }
			public string SelectedAction { get; set; }
			public long RecurrenceId { get; set; }

	    }

		[Access(AccessLevel.UserOrganization)]
	    public ActionResult Modal(long recurrenceId)
	    {
			new PermissionsAccessor().Permitted(GetUser(),x=>x.ViewL10Recurrence(recurrenceId));

		    var model = new PhoneVM(){
				RecurrenceId = recurrenceId,
				PossibleActions = new List<SelectListItem>() { new SelectListItem() { Text = "Add Issue", Value = "issue" }, new SelectListItem() { Text = "Add To-Do", Value = "todo" } },
				PossibleNumbers = PhoneAccessor.GetUnusedCallablePhoneNumbersForUser(GetUser(),GetUser().Id).ToSelectList(x=>x.Number,x=>x.Id)
		    };
			

			return PartialView(model);
	    }

		[Access(AccessLevel.UserOrganization)]
	    public JsonResult Delete(long id)
	    {
		    PhoneAccessor.DeleteAction(GetUser(), id);
	    }

	
		[Access(AccessLevel.Any)]
		public ContentResult ReceiveText_53B006C3B7ED45C58EE31DBFA85D75BA()
		{
			try{
				return PhoneContent(PhoneAccessor.ReceiveText(
					Request["From"].ToLong(),
					Request["Body"],
					Request["To"].ToLong())
				);
			}
			catch (PhoneException e){
				return PhoneContent(e.Message);
			}
			catch (Exception){
				return Content("<Response><Sms>We're sorry, this service is unavailable at this time.</Sms></Response>");
			}
		}

	    protected ContentResult PhoneContent(string message){
		    return Content("<Response><Sms>"+message+"</Sms></Response>");
	    }
    }
}