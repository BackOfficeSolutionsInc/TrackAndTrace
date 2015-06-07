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
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;

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
			/*public List<SelectListItem> AvailableMembers { get; set; }
			public long SelectedAccountableMember { get; set; }*/
			//public long SelectedAdminMember { get; set; }
			public List<MeasurableModel> Measurables { get; set; }

	    }

		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public ActionResult AddMeasurable(long id)
		{
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);

			var allMeasurables = ScorecardAccessor.GetPotentialMeetingMeasurables(GetUser(), recurrenceId, true);
			//var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var members = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();
			var already = recurrence._DefaultMeasurables.Select(x => x.Measurable.Id).ToList();

			var addableMeasurables = allMeasurables
				.Where(x => members.Contains(x.AccountableUserId) || members.Contains(x.AdminUserId))
				.Where(x => !already.Contains(x.Id))
				.ToList();

			//var addableMeasurables = allMeasurables.Except(, x => x.Id);

			var am = new AddMeasurableVm()
			{
				AvailableMeasurables = addableMeasurables.ToSelectList(x => x.Title+"("+x.AccountableUser.GetName()+")", x => x.Id),
				//AvailableMembers = allMembers.ToSelectList(x => x.GetName(), x => x.Id),
				RecurrenceId = recurrenceId,
			};

			am.AvailableMeasurables.Add(new SelectListItem(){Text = "<Create Measurable>", Value = "-3"});

			return PartialView(am);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult AddMeasurable(AddMeasurableVm model)
		{
			ValidateValues(model,x=>x.RecurrenceId);
			L10Accessor.CreateMeasurable(GetUser(), model.RecurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}

	    [HttpPost]
	    [Access(AccessLevel.UserOrganization)]
	    public JsonResult UpdateArchiveMeasurable(string pk, string name, string value)
		{
			var measurableId = pk.Split('_')[0].ToLong();
			var recurrenceId = pk.Split('_')[1].ToLong();
			string title = null;
			LessGreater? direction = null;
			decimal? target = null;
			long? adminId = null;
			long? accountableId = null;
			switch (name)
			{
				case "target": target = value.ToDecimal(); break;
				case "direction": direction = (LessGreater)Enum.Parse(typeof(LessGreater), value); break;
				case "title": title = value; break;
				case "admin": adminId = value.ToLong(); break;
				case "accountable": accountableId = value.ToLong(); break;
				default: throw new ArgumentOutOfRangeException("name");
			}

			L10Accessor.UpdateArchiveMeasurable(GetUser(), measurableId, recurrenceId, title, direction, target, accountableId, adminId);
			return Json(ResultObject.SilentSuccess());
	    }


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateMeasurable(long pk,string name,string value)
		{
			var meeting_measureableId = pk;
			
			string title=null;
			LessGreater? direction = null;
			decimal? target = null;
			long? adminId = null;
			long? accountableId = null;
			switch (name){
				case "target":		target = value.ToDecimal(); break;
				case "direction":	direction = (LessGreater)Enum.Parse(typeof(LessGreater), value); break;
				case "title":		title = value; break;
				case "admin":		adminId = value.ToLong(); break;
				case "accountable": accountableId = value.ToLong(); break;
				default: throw new ArgumentOutOfRangeException("name");
			}

			L10Accessor.UpdateMeasurable(GetUser(), meeting_measureableId, title, direction, target,accountableId,adminId);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult ExportScorecard(long id,string type="csv")
		{
			var scores = L10Accessor.GetScoresForRecurrence(GetUser(),id);
			var recur=L10Accessor.GetL10Recurrence(GetUser(), id, false);

			switch(type.ToLower()){
				case "csv":{
					var csv = new Csv();
					csv.SetTitle("Measurable");
					foreach (var s in scores.GroupBy(x => x.MeasurableId)){
						var ss = s.First();
						csv.Add(ss.Measurable.Title, "Owner", ss.Measurable.AccountableUser.GetName());
						csv.Add(ss.Measurable.Title, "Admin", ss.Measurable.AdminUser.GetName());
						csv.Add(ss.Measurable.Title, "Target", "" + ss.Measurable.Goal);
						csv.Add(ss.Measurable.Title, "TargetDirection", "" + ss.Measurable.GoalDirection);
					}
					foreach (var s in scores.OrderBy(x=>x.ForWeek)){
						csv.Add(s.Measurable.Title, s.ForWeek.ToShortDateString(), s.Measured.NotNull(x => x.Value.ToString()) ?? "");
					}
					
					return File(new System.Text.UTF8Encoding().GetBytes(csv.ToCsv()), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + recur.Name +"_Scorecard.csv");
					break;
				}
				default: throw new Exception("Unrecognized Type");
			}
		
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
			L10Accessor.UpdateIssue(GetUser(), issueId, complete: @checked, connectionId: connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssue(long id, string message, string details)
		{
			L10Accessor.UpdateIssue(GetUser(), id, message, details);
			return Json(ResultObject.SilentSuccess());
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
		public JsonResult UpdateTodo(long id,string message,string details,DateTime? dueDate,long? accountableUser)
		{
			L10Accessor.UpdateTodo(GetUser(), id, message, details, dueDate, accountableUser);
			return Json(ResultObject.SilentSuccess());
		}
		
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodoCompletion(long id, long todoId, bool @checked, string connectionId = null)
		{
			var recurrenceId = id;
			L10Accessor.UpdateTodo(GetUser(), todoId, complete:@checked, connectionId:connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodoDate(long id, long date)
		{
			var todo = id;
			var dateR = date.ToDateTime();
			L10Accessor.UpdateTodo(GetUser(), id, dueDate: dateR);
			return Json(ResultObject.SilentSuccess(date.ToString()));
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
			var cache =new Cache();
			cache.Get(CacheKeys.LAST_SEND_NOTE_TIME);

			if (cache.Get(CacheKeys.LAST_SEND_NOTE_TIME) == null || model.SendTime > (long)cache.Get(CacheKeys.LAST_SEND_NOTE_TIME)){
				cache.Push(CacheKeys.LAST_SEND_NOTE_TIME,model.SendTime,LifeTime.Session);
				L10Accessor.EditNote(GetUser(), model.NoteId, model.Contents, model.Name, model.ConnectionId);
				return Json(ResultObject.SilentSuccess(model).NoRefresh());
			}
			return Json(ResultObject.SilentSuccess("stale").NoRefresh());
		}


		#endregion

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Members(long id)
		{
			var recurrenceId = id;
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
			var result = recur._DefaultAttendees.Select(x => new{
				id = x.User.Id,
				name = x.User.GetName(),
				imageUrl = x.User.ImageUrl(true,ImageSize._32)
			});
			return Json(ResultObject.SilentSuccess(result), JsonRequestBehavior.AllowGet);
		}

	}
}