using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models.Json;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Enums;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
	{

		#region Scorecard
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public ActionResult UpdateScore(long id, long s, long w, long m, string value, string dom)
		{
			var recurrenceId = id;
			var scoreId = s;
			var week = w.ToDateTime();
			var measurableId = m;
			decimal measured;
			decimal? val = null;
			string output = null;
			if (decimal.TryParse(value, out measured))
			{
				val = measured;
				output = value;
			}
			ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrenceId, scoreId, week, measurableId, val, dom);


			return Json(ResultObject.SilentSuccess(output), JsonRequestBehavior.AllowGet);
		}

	    public class AddMeasurableVm
		{
			public long RecurrenceId { get; set; }
			public List<SelectListItem> AvailableMeasurables  { get; set; }
			public long SelectedMeasurable { get; set; }
			public List<SelectListItem> AvailableMembers { get; set; }
			public long SelectedAccountableMember { get; set; }
			public long SelectedAdminMember { get; set; }

	    }
		
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AddMeasurable(long id)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
				
			var allMeasurables = ScorecardAccessor.GetOrganizationMeasurables(GetUser(), GetUser().Organization.Id, true);
			var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var addableMeasurables = allMeasurables.Except(recurrence._DefaultMeasurables.Select(x=>x.Measurable),x=>x.Id);

			var am = new AddMeasurableVm(){
				AvailableMeasurables = addableMeasurables.ToSelectList(x=>x.Title,x=>x.Id),
				AvailableMembers = allMembers.ToSelectList(x=>x.GetName(),x=>x.Id),
				RecurrenceId = recurrenceId,
			};

			return PartialView(am);
		}
		#endregion

		#region Issues
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssues(long id, IssuesDataList model)
		{
			var recurrenceId = id;
			L10Accessor.UpdateIssues(GetUser(), recurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}
		
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssueCompletion(long id, long issueId,bool @checked,string connectionId=null)
		{
			var recurrenceId = id;
			L10Accessor.UpdateIssueCompletion(GetUser(), recurrenceId, issueId, @checked, connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}
		#endregion

		#region Todos
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodos(long id, TodoDataList model)
		{
			var recurrenceId = id;
			L10Accessor.UpdateTodos(GetUser(), recurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodoCompletion(long id, long todoId, bool @checked, string connectionId = null)
		{
			var recurrenceId = id;
			L10Accessor.UpdateTodoCompletion(GetUser(), recurrenceId, todoId, @checked, connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateRockCompletion(long id, long rockId, RockState state, string connectionId = null)
		{
			var recurrenceId = id;
			L10Accessor.UpdateRockCompletion(GetUser(), recurrenceId, rockId, state, connectionId);
			return Json(ResultObject.SilentSuccess(state.ToString()));
		}

		#endregion

		#region Notes
		public class NoteVM
		{
			[Required]
			[Display(Name = "Page Name:")]
			public string Name { get; set; }
			[Required]
			public long RecurrenceId { get; set; }
			[Display(Name = "Contents:")]
			public string Contents { get; set; }
			public long NoteId { get; set; }
			public String ConnectionId { get; set; }
			public long SendTime { get; set; }
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Note(long id)
		{
			var note =L10Accessor.GetNote(GetUser(), id);
			return Json(ResultObject.SilentSuccess(new NoteVM(){
				Contents = note.Contents,
				Name = note.Name,
				NoteId = id
			}),JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
	    public ActionResult CreateNote(long recurrence)
	    {
		    return PartialView(new NoteVM(){RecurrenceId = recurrence});
	    }
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult CreateNote(NoteVM model)
		{
			ValidateValues(model,x=>x.RecurrenceId);
			if (ModelState.IsValid){
				L10Accessor.CreateNote(GetUser(), model.RecurrenceId, model.Name);
				return Json(ResultObject.SilentSuccess(model).NoRefresh());
			}
			return Json(ResultObject.CreateMessage(StatusType.Danger, "Error creating note").NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult EditNote(NoteVM model)
		{
			if (Session["LastSendNoteTime"]==null || model.SendTime > (long)Session["LastSendNoteTime"]){
				Session["LastSendNoteTime"] = model.SendTime;
				L10Accessor.EditNote(GetUser(), model.NoteId, model.Contents, model.Name, model.ConnectionId);
				return Json(ResultObject.SilentSuccess(model).NoRefresh());
			}
			return Json(ResultObject.SilentSuccess("stale").NoRefresh());
		}


		#endregion

	}
}