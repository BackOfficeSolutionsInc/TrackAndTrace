using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class DropDownController : BaseController
    {
		public class DropDownItem
	    {
		    public string value { get; set; }
			public string text { get; set; }
	    }


		[Access(AccessLevel.UserOrganization)]
		public JsonResult Type(string id)
		{
			var result = new List<DropDownItem>();
			switch (id.ToLower())
			{
				case "lessgreater":
					foreach (var i in Enum.GetValues(typeof(LessGreater)))
						result.Add(new DropDownItem() { text = ((LessGreater)i).ToSymbol(), value = i.ToString() });
					break;
				case "unittype":
					foreach (var i in Enum.GetValues(typeof(UnitType)))
						result.Add(new DropDownItem() { text = ((UnitType)i).ToTypeString(), value = i.ToString() });
					break;
				default: throw new ArgumentOutOfRangeException(id);
			}
			return Json(result, JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public JsonResult MeetingMembers(long id,bool userId=false)
		{
			var recurrenceId = id;
			var result =L10Accessor.GetCurrentL10Meeting(GetUser(), id, true, true, false)._MeetingAttendees.Select(x=>new DropDownItem(){
				text = x.User.GetName(),
				value = userId?""+x.User.Id:""+x.Id
			});
			return Json(result, JsonRequestBehavior.AllowGet);
		}
    }
}