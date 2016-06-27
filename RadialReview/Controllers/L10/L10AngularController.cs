using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Issues;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Angular.Scorecard;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Json;
using RadialReview.Utilities;
using RadialReview.Models.Askables;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Issues;
using System.Threading.Tasks;
using RadialReview.Models.Todo;
using RadialReview.Models.Angular.Users;

namespace RadialReview.Controllers {
    public partial class L10Controller :BaseController {

        #region Ancillary
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularBasics(AngularBasics model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularMeetingNotes(AngularMeetingNotes model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }
        #endregion

        #region Scorecard
        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult AddAngularMeasurable(long id)
        {
            var recurrenceId = id;
            L10Accessor.CreateMeasurable(GetUser(), recurrenceId, AddMeasurableVm.CreateNewMeasurable(recurrenceId, new MeasurableModel() {
                OrganizationId = GetUser().Organization.Id,
                AdminUserId = GetUser().Id,
                AccountableUserId = GetUser().Id,
            }, true));
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult RemoveAngularMeasurable(long recurrenceId, AngularMeasurable model, string connectionId = null)
        {
            //var recurrenceId = id;
            L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
       

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularMeasurable(AngularMeasurable model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }
        
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularScore(AngularScore model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }
        #endregion

        #region Rocks
        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult AddAngularRock(long id)
        {
            var recurrenceId = id;
            var rock = new RockModel() {
                OrganizationId = GetUser().Organization.Id,
                ForUserId = GetUser().Id,
            };
            L10Accessor.CreateRock(GetUser(), recurrenceId, AddRockVm.CreateRock(recurrenceId, rock, true));
            return Json(ResultObject.SilentSuccess(rock), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularRock(AngularRock model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        } 
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult RemoveAngularRock(long recurrenceId, AngularRock model, string connectionId = null)
        {
            //var recurrenceId = id;
            L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Todos
        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> AddAngularTodo(long id)
        {
            var recurrenceId = id;
            await TodoAccessor.CreateTodo(GetUser(), recurrenceId, new TodoModel());
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularTodo(AngularTodo model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult RemoveAngularTodo(long recurrenceId, AngularTodo model, string connectionId = null)
        {
            L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Issues
        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> AddAngularIssue(long id)
        {
            var recurrenceId = id;
            await IssuesAccessor.CreateIssue(GetUser(), recurrenceId, GetUser().Id, new IssueModel());
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult UpdateAngularIssue(AngularIssue model, string connectionId = null)
        {
            L10Accessor.Update(GetUser(), model, connectionId);
            return Json(ResultObject.SilentSuccess());
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult RemoveAngularIssue(long recurrenceId, AngularIssue model, string connectionId = null)
        {
            //var recurrenceId = id;
            L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        #endregion

        #region Attendeees

        [HttpGet]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult AddAngularUser(long id,long userid)
        {
            var recurrenceId = id;
            L10Accessor.AddAttendee(GetUser(), recurrenceId, userid);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult RemoveAngularUser(long recurrenceId, AngularUser model, string connectionId = null)
        {
            //var recurrenceId = id;
            L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }
        #endregion
    }
}