using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ionic.Zip;
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
			UnitType? unitType = null;
			switch (name.ToLower()){
				case "target": target = value.ToDecimal(); break;
				case "direction":	direction = (LessGreater)Enum.Parse(typeof(LessGreater), value); break;
				case "unittype": unitType = (UnitType)Enum.Parse(typeof(UnitType), value); break;
				case "title":		title = value; break;
				case "admin":		adminId = value.ToLong(); break;
				case "accountable": accountableId = value.ToLong(); break;
				default: throw new ArgumentOutOfRangeException("name");
			}

			L10Accessor.UpdateMeasurable(GetUser(), meeting_measureableId, title, direction, target, accountableId, adminId, unitType);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult ExportScorecard(long id,string type="csv")
		{
			var csv = ExportAccessor.Scorecard(GetUser(), id, type);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), id, false);
			return File(csv, "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + recur.Name + "_Scorecard.csv");
		}


	    [Access(AccessLevel.UserOrganization)]
	    public FileStreamResult ExportAll(long id)
	    {
			/*Response.Clear();
			Response.BufferOutput = false; // false = stream immediately
			System.Web.HttpContext c = System.Web.HttpContext.Current;

			Response.ContentType = "application/zip";
			Response.AddHeader("content-disposition", "filename=" + archiveName);*/

			var recur = L10Accessor.GetL10Recurrence(GetUser(), id, false);
		    var time = DateTime.UtcNow.ToJavascriptMilliseconds();

			var memoryStream = new MemoryStream();
			using (var zip = new ZipFile())
			{
				zip.AddEntry(String.Format("Scorecard.csv", time, recur.Name), ExportAccessor.Scorecard(GetUser(), id));
				zip.AddEntry(String.Format("To-Do.csv", time, recur.Name), ExportAccessor.TodoList(GetUser(), id));
				zip.AddEntry(String.Format("Issues.csv", time, recur.Name), ExportAccessor.IssuesList(GetUser(), id));
				zip.AddEntry(String.Format("Rocks.csv", time, recur.Name), ExportAccessor.Rocks(GetUser(), id));
				zip.AddEntry(String.Format("MeetingSummary.csv", time, recur.Name), ExportAccessor.MeetingSummary(GetUser(), id));

				foreach (var note in ExportAccessor.Notes(GetUser(), id))
					zip.AddEntry(String.Format("{2}", time, recur.Name, note.Item1), note.Item2);

				zip.Save(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);
				var archiveName = String.Format("{0}_{1}.zip", time, recur.Name);
				return File(memoryStream, "application/gzip", archiveName);
			}

		

	    }

	    public class MeasurableOrdering
	    {
		    public long[] ordering { get; set; }
			public long recurrenceId { get; set; }
	    }

		[HttpPost]
	    [Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateMeasurableOrdering(MeasurableOrdering model)
	    {
			L10Accessor.SetMeasurableOrdering(GetUser(), model.recurrenceId, model.ordering.ToList());
		    return Json(ResultObject.SilentSuccess());
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
        public JsonResult UpdateIssueCompletion(long id, long issueId, bool @checked, DateTime? time = null, string connectionId = null)
        {
            time = time ?? DateTime.UtcNow;
			var recurrenceId = id;
            L10Accessor.UpdateIssue(GetUser(), issueId, time.Value, complete: @checked, connectionId: connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssue(long id, DateTime? time=null, string message = null, string details = null, long? owner = null,int? priority=null)
		{
            time = time ?? DateTime.UtcNow;
			L10Accessor.UpdateIssue(GetUser(), id,time.Value, message, details,owner:owner,priority: priority);
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

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult XUpdateTodo(string pk, string name, string value)
		{
			var todoId = pk.ToLong();
			switch (name)
			{
				case "accountable": return UpdateTodo(todoId, null, null, null, value.ToLong());
				case "title": return UpdateTodo(todoId, value, null, null, null);
				case "details": return UpdateTodo(todoId, null, value, null, null);
				case "duedate": return UpdateTodo(todoId, null, null, value.ToLong().ToDateTime(), null);
				default: throw new ArgumentOutOfRangeException("name");
			}
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult XUpdateIssue(string pk, string name, string value)
		{
			var issueId = pk.ToLong();
			switch (name){
				case "title":		return UpdateIssue(issueId,DateTime.UtcNow, value, null, null);
                case "details":     return UpdateIssue(issueId, DateTime.UtcNow, null, value, null);
                case "owner":       return UpdateIssue(issueId, DateTime.UtcNow, null, null, value.ToLong());
				default: throw new ArgumentOutOfRangeException("name");
			}
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