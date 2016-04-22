using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models.L10;

namespace RadialReview.Controllers {
    public class DropDownController : BaseController {
        public class DropDownItem {
            public string value { get; set; }
            public string text { get; set; }
        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult Type(string id)
        {
            var result = new List<DropDownItem>();
            switch (id.ToLower()) {
                case "lessgreater":
                    foreach (var i in Enum.GetValues(typeof(LessGreater)))
                        result.Add(new DropDownItem() { text = ((LessGreater)i).GetDescription(), value = i.ToString() });
                    break;
                case "unittype":
                    foreach (var i in Enum.GetValues(typeof(UnitType)))
                        result.Add(new DropDownItem() { text = ((UnitType)i).ToTypeString(), value = i.ToString() });
                    break;
                case "l10teamtype":
                    foreach (var i in Enum.GetValues(typeof(L10TeamType)))
                        if ((int)i != 0)
                            result.Add(new DropDownItem() { text = ((L10TeamType)i).GetDisplayName(), value = i.ToString() });
                    break;
                default: throw new ArgumentOutOfRangeException(id);
            }
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult OrganizationMembers(long id, bool userId = false)
        {
            var recurrenceId = id;
            var attendees = _OrganizationAccessor.GetOrganizationMembers(GetUser(), recurrenceId, false, false);

            var result = attendees.Select(x => new DropDownItem() {
                text = x.GetName(),
                value = "" + x.Id
            });
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [Access(AccessLevel.UserOrganization)]
        public JsonResult MeetingMembers(long id, bool userId = false)
        {
            var recurrenceId = id;
            var attendees = L10Accessor.GetL10Recurrence(GetUser(), id, true)._DefaultAttendees;

            var result = attendees.Select(x => new {
                text = x.User.GetName(),
                value = userId ? "" + x.User.Id : "" + x.Id,
                url = x.User.ImageUrl(true, ImageSize._32),
                profilePicture = true
            }).OrderBy(x => x.text);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult AngularMeetingMembers(long id, bool userId = false)
        {
            var recurrenceId = id;
            var attendees = L10Accessor.GetAngularRecurrence(GetUser(), id).Attendees;

            return Json(attendees, JsonRequestBehavior.AllowGet);

            //var result = attendees.Select(x => new {
            //    text = x.User.GetName(),
            //    value = userId ? "" + x.User.Id : "" + x.Id,
            //    url = x.User.ImageUrl(true, ImageSize._32),
            //    profilePicture = true
            //}).OrderBy(x => x.text);
            //return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.UserOrganization)]
        public JsonResult OrganizationRGM(string q)
        {

            q = q.ToLower();

            var rgm = OrganizationAccessor.GetOrganizationResponsibilityGroupModels(GetUser(), GetUser().Organization.Id).Where(x => x.GetName().ToLower().Contains(q));
            var result = rgm.Select(x => new DropDownItem() {
                text = x.GetName() + "<span class='label label-default group-" + x.GetGroupType() + "'>" + x.GetGroupType() + "</span>",
                value = "" + x.Id
            }).OrderBy(x => x.text);
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}