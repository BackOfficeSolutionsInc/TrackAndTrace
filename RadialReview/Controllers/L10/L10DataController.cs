using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Ionic.Zip;
using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Hubs;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Models.Askables;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Enums;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using System.Dynamic;
using Newtonsoft.Json;
using RadialReview.Accessors.VideoConferenceProviders;

namespace RadialReview.Controllers {
	public partial class L10Controller : BaseController {
		#region AddRock
		public class AddRockVm {
			public long RecurrenceId { get; set; }
			public List<SelectListItem> AvailableRocks { get; set; }
			public long SelectedRock { get; set; }
			/*public List<SelectListItem> AvailableMembers { get; set; }
            public long SelectedAccountableMember { get; set; }*/
			//public long SelectedAdminMember { get; set; }
			public List<RockModel> Rocks { get; set; }

			public bool AllowCreateCompanyRock { get; set; }

			public static AddRockVm CreateRock(long recurrenceId, RockModel model, bool allowBlankRock = false) {
				if (model == null)
					throw new ArgumentNullException("model", "Rock was null");

				if (model.ForUserId <= 0)
					throw new ArgumentOutOfRangeException("You must specify an accountable user id");
				if (model.OrganizationId <= 0)
					throw new ArgumentOutOfRangeException("You must specify an organization id");
				if (recurrenceId <= 0)
					throw new ArgumentOutOfRangeException("You must specify a recurrence id");
				if (String.IsNullOrWhiteSpace(model.Rock) && !allowBlankRock)
					throw new ArgumentOutOfRangeException("You must specify a title for the rock");

				return new AddRockVm() {
					SelectedRock = -3,
					Rocks = model.AsList(),
					RecurrenceId = recurrenceId,
				};
			}

		}

		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult AddRock(long id) {
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);

			var allRocks = RockAccessor.GetPotentialMeetingRocks(GetUser(), recurrenceId, true);
			//var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var members = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();
			var already = recurrence._DefaultRocks.Select(x => x.ForRock.Id).ToList();

			var addableRocks = allRocks
				.Where(x => members.Contains(x.AccountableUser.Id) || members.Contains(x.ForUserId))
				.Where(x => !already.Contains(x.Id))
				.ToList();

			//var addableMeasurables = allMeasurables.Except(, x => x.Id);

			var am = new AddRockVm() {
				AvailableRocks = addableRocks.ToSelectList(x => x.ToFriendlyString(), x => x.Id),
				//AvailableMembers = allMembers.ToSelectList(x => x.GetName(), x => x.Id),
				RecurrenceId = recurrenceId,
				AllowCreateCompanyRock = recurrence.TeamType == L10TeamType.LeadershipTeam
			};

			//am.AvailableRocks.Add(new SelectListItem() { Text = "<Create Rock>", Value = "-3" });

			return PartialView(am);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult AddRock(AddRockVm model) {
			ValidateValues(model, x => x.RecurrenceId);
			L10Accessor.CreateRock(GetUser(), model.RecurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}
		#endregion

		#region Scorecard
		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateScore(long id, long s, long w, long m, string value, string dom, string connection) {
			var recurrenceId = id;
			var scoreId = s;
			var week = w.ToDateTime();
			var measurableId = m;
			decimal measured;
			decimal? val = null;
			string output = null;
			if (decimal.TryParse(value, out measured)) {
				val = measured;
				output = value;
			}
			ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrenceId, scoreId, week, measurableId, val, dom, connection);


			return Json(ResultObject.SilentSuccess(output), JsonRequestBehavior.AllowGet);
		}

		public class AddMeasurableVm {
			public long RecurrenceId { get; set; }
			public List<SelectListItem> AvailableMeasurables { get; set; }
			public long SelectedMeasurable { get; set; }
			/*public List<SelectListItem> AvailableMembers { get; set; }
            public long SelectedAccountableMember { get; set; }*/
			//public long SelectedAdminMember { get; set; }
			public List<MeasurableModel> Measurables { get; set; }

			public static AddMeasurableVm CreateNewMeasurable(long recurrenceId, MeasurableModel model, bool allowBlankMeasurable = false) {
				if (model.AdminUserId <= 0)// && (model.AdminUser==null || model.AdminUser.Id<=0))
					throw new ArgumentOutOfRangeException("You must specify an admin user id");
				if (model.AccountableUserId <= 0)//&& (model.AccountableUser == null || model.AccountableUser.Id <= 0))
					throw new ArgumentOutOfRangeException("You must specify an accountable user id");
				if (model.OrganizationId <= 0)// && (model.Organization == null || model.Organization.Id <= 0))
					throw new ArgumentOutOfRangeException("You must specify an organization id");
				if (String.IsNullOrWhiteSpace(model.Title) && !allowBlankMeasurable)
					throw new ArgumentOutOfRangeException("You must specify a title for the measurable");
				if ((int)model.GoalDirection == 0) {
					model.GoalDirection = LessGreater.GreaterThan;
				}
				return new AddMeasurableVm() {
					SelectedMeasurable = -3,
					RecurrenceId = recurrenceId,
					Measurables = new List<MeasurableModel>() { model }
				};
			}

		}

		// GET: L10Data
		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult AddMeasurable(long id) {
			var recurrenceId = id;
			var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);

			var allMeasurables = ScorecardAccessor.GetPotentialMeetingMeasurables(GetUser(), recurrenceId, true);
			//var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var members = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true)._DefaultAttendees.Select(x => x.User.Id).ToList();
			var already = recurrence._DefaultMeasurables.Where(x => x.Measurable != null).Select(x => x.Measurable.Id).ToList();

			var addableMeasurables = allMeasurables
				.Where(x => members.Contains(x.AccountableUserId) || members.Contains(x.AdminUserId))
				.Where(x => !already.Contains(x.Id))
				.ToList();

			//var addableMeasurables = allMeasurables.Except(, x => x.Id);

			var am = new AddMeasurableVm() {
				AvailableMeasurables = addableMeasurables.ToSelectList(x => x.Title + "(" + x.AccountableUser.GetName() + ")", x => x.Id),
				//AvailableMembers = allMembers.ToSelectList(x => x.GetName(), x => x.Id),
				RecurrenceId = recurrenceId,
			};

			am.AvailableMeasurables.Add(new SelectListItem() { Text = "<Create Measurable>", Value = "-3" });

			return PartialView(am);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult AddMeasurableDivider(long recurrence) {
			L10Accessor.CreateMeasurableDivider(GetUser(), recurrence);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult RemoveMeasurableDivider(long id) {
			L10Accessor.DeleteMeetingMeasurableDivider(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult AddMeasurable(AddMeasurableVm model) {
			ValidateValues(model, x => x.RecurrenceId);
			L10Accessor.CreateMeasurable(GetUser(), model.RecurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult RanFireworks(long id) {
			L10Accessor.MarkFireworks(GetUser(), id);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateArchiveMeasurable(string pk, string name, string value) {
			var measurableId = pk.Split('_')[0].ToLong();
			var recurrenceId = pk.Split('_')[1].ToLong();
			string title = null;
			LessGreater? direction = null;
			decimal? target = null;
			long? adminId = null;
			long? accountableId = null;
			switch (name) {
				case "target":
					target = value.ToDecimal();
					break;
				case "direction":
					direction = (LessGreater)Enum.Parse(typeof(LessGreater), value);
					break;
				case "title":
					title = value;
					break;
				case "admin":
					adminId = value.ToLong();
					break;
				case "accountable":
					accountableId = value.ToLong();
					break;
				default:
					throw new ArgumentOutOfRangeException("name");
			}

			L10Accessor.UpdateArchiveMeasurable(GetUser(), measurableId, title, direction, target, accountableId, adminId);
			return Json(ResultObject.SilentSuccess());
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateMeasurable(long pk, string name, string value) {
			var meeting_measureableId = pk;

			string title = null;
			LessGreater? direction = null;
			decimal? target = null;
			long? adminId = null;
			long? accountableId = null;
			UnitType? unitType = null;
			switch (name.ToLower()) {
				case "target":
					target = value.ToDecimal();
					break;
				case "direction":
					direction = (LessGreater)Enum.Parse(typeof(LessGreater), value);
					break;
				case "unittype":
					unitType = (UnitType)Enum.Parse(typeof(UnitType), value);
					break;
				case "title":
					title = value;
					break;
				case "admin":
					adminId = value.ToLong();
					break;
				case "accountable":
					accountableId = value.ToLong();
					break;
				default:
					throw new ArgumentOutOfRangeException("name");
			}

			L10Accessor.UpdateMeasurable(GetUser(), meeting_measureableId, title, direction, target, accountableId, adminId, unitType);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult ExportScorecard(long id, string type = "csv") {
			var csv = ExportAccessor.Scorecard(GetUser(), id, type);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), id, false);
			return File(csv, "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + recur.Name + "_Scorecard.csv");
		}


		[Access(AccessLevel.Radial)]
		public string DeleteScorecard(long id) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {

					var ms = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.L10Recurrence.Id == id).List().ToList();

					var now = DateTime.UtcNow;

					foreach (var m in ms) {
						m.DeleteTime = now;
						s.Update(m);

						var others = s.QueryOver<L10Recurrence.L10Recurrence_Measurable>().Where(x => x.DeleteTime == null && x.Measurable.Id == m.Id).List().ToList();
						if (!others.Any()) {
							var meas = s.Get<MeasurableModel>(m.Measurable.Id);
							meas.DeleteTime = now;
							s.Update(meas);
						}

					}
					var cur = L10Accessor._GetCurrentL10Meeting(s, PermissionsUtility.Create(s, GetUser()), id, true, false, false);
					if (cur != null) {
						var ms2 = s.QueryOver<L10Meeting.L10Meeting_Measurable>().Where(x => x.DeleteTime == null && x.L10Meeting.Id == cur.Id).List().ToList();

						foreach (var m in ms2) {
							m.DeleteTime = now;
							s.Update(m);
						}
					}




					tx.Commit();
					s.Flush();

					return "Deleted " + ms.Count + " measurables from recurrence " + id + " at " + now.ToString();
				}
			}
		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<FileStreamResult> ExportAll(long id, bool includeDetails = false) {
			/*Response.Clear();
            Response.BufferOutput = false; // false = stream immediately
            System.Web.HttpContext c = System.Web.HttpContext.Current;

            Response.ContentType = "application/zip";
            Response.AddHeader("content-disposition", "filename=" + archiveName);*/

			var recur = L10Accessor.GetL10Recurrence(GetUser(), id, false);
			var time = DateTime.UtcNow.ToJavascriptMilliseconds();

			var memoryStream = new MemoryStream();
			using (var zip = new ZipFile()) {
				zip.AddEntry(String.Format("Scorecard.csv", time, recur.Name), ExportAccessor.Scorecard(GetUser(), id));
				zip.AddEntry(String.Format("To-Do.csv", time, recur.Name), await ExportAccessor.TodoList(GetUser(), id, includeDetails));
				zip.AddEntry(String.Format("Issues.csv", time, recur.Name), await ExportAccessor.IssuesList(GetUser(), id, includeDetails));
				zip.AddEntry(String.Format("Rocks.csv", time, recur.Name), ExportAccessor.Rocks(GetUser(), id));
				zip.AddEntry(String.Format("MeetingSummary.csv", time, recur.Name), ExportAccessor.MeetingSummary(GetUser(), id));

				foreach (var note in await ExportAccessor.Notes(GetUser(), id))
					zip.AddEntry(String.Format("{2}", time, recur.Name, note.Item1.Replace("/", "_")), note.Item2);

				zip.Save(memoryStream);
				memoryStream.Seek(0, SeekOrigin.Begin);
				var archiveName = String.Format("{0}_{1}.zip", time, recur.Name);
				return File(memoryStream, "application/gzip", archiveName);
			}



		}

		public class MeasurableOrdering {
			public long[] ordering { get; set; }
			public long recurrenceId { get; set; }
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateMeasurableOrdering(MeasurableOrdering model) {
			L10Accessor.SetMeetingMeasurableOrdering(GetUser(), model.recurrenceId, model.ordering.ToList());
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateRecurrenceMeasurableOrdering(MeasurableOrdering model) {
			L10Accessor.SetRecurrenceMeasurableOrdering(GetUser(), model.recurrenceId, model.ordering.ToList());
			return Json(ResultObject.SilentSuccess());
		}

		#endregion

		#region Issues
		public class IssuesListVm {
			public string connectionId { get; set; }
			public List<IssueListItemVm> issues { get; set; }
			public string orderby { get; set; }
			public IssuesListVm() {
				issues = new List<IssueListItemVm>();
			}
			public List<long> GetAllIds() {
				return issues.SelectMany(x => x.GetAllIds()).Distinct().ToList();
			}

			public class IssueListItemVm {
				public long id { get; set; }
				public List<IssueListItemVm> children { get; set; }
				public IssueListItemVm() {
					children = new List<IssueListItemVm>();
				}
				public List<long> GetAllIds() {
					var o = new List<long>() { id };
					if (children != null) {
						o.AddRange(children.SelectMany(x => x.GetAllIds()));
					}
					return o;
				}
			}

			public List<IssueEdit> GetIssueEdits() {
				return issuesRecurse(null, issues).ToList();
			}

			private IEnumerable<IssueEdit> issuesRecurse(long? parentIssueId, List<IssueListItemVm> data) {
				if (data == null)
					return new List<IssueEdit>();
				var output = data.Select((x, i) => new IssueEdit() {
					RecurrenceIssueId = x.id,
					ParentRecurrenceIssueId = parentIssueId,
					Order = i
				}).ToList();
				foreach (var d in data) {
					output.AddRange(issuesRecurse(d.id, d.children));
				}
				return output;
			}
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssues(long id, /*IssuesDataList*/ IssuesListVm model) {
			var recurrenceId = id;
			L10Accessor.UpdateIssues(GetUser(), recurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssueCompletion(long id, long issueId, bool @checked, DateTime? time = null, string connectionId = null) {
			time = time ?? DateTime.UtcNow;
			var recurrenceId = id;
			L10Accessor.UpdateIssue(GetUser(), issueId, time.Value, complete: @checked, connectionId: connectionId);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpGet]
		public JsonResult UpdateIssueCompleted(long id, bool @checked, string connectionId = null) {
			var time = DateTime.UtcNow;
			var recurrenceId = id;
			L10Accessor.UpdateIssue(GetUser(), id, time, complete: @checked, connectionId: connectionId);
			return Json(ResultObject.SilentSuccess(@checked), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssue(long id, DateTime? time = null, string message = null, string details = null, long? owner = null, int? priority = null, int? rank = null) {
			time = time ?? DateTime.UtcNow;
			L10Accessor.UpdateIssue(GetUser(), id, time.Value, message, details, owner: owner, priority: priority, rank: rank);
			return Json(ResultObject.SilentSuccess());
		}

		public class IssueRankVM {
			public long id { get; set; }
			public int rank { get; set; }
			public DateTime time { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateIssuesRank(List<IssueRankVM> arr) {
			foreach (var m in arr) {
				L10Accessor.UpdateIssue(GetUser(), m.id, DateTime.UtcNow, rank: m.rank);
			}
			return Json(ResultObject.SilentSuccess());
		}

		#endregion

		#region Todos
		public class UpdateTodoVM {
			public List<long> todos { get; set; }
			public string connectionId { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodos(long id, UpdateTodoVM model) {
			var recurrenceId = id;
			L10Accessor.UpdateTodos(GetUser(), recurrenceId, model);
			return Json(ResultObject.SilentSuccess());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodo(long id, string message, string details, DateTime? dueDate, long? accountableUser) {
			L10Accessor.UpdateTodo(GetUser(), id, message, details, dueDate, accountableUser);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult XUpdateTodo(string pk, string name, string value) {
			var todoId = pk.ToLong();
			switch (name) {
				case "accountable":
					return UpdateTodo(todoId, null, null, null, value.ToLong());
				case "title":
					return UpdateTodo(todoId, value, null, null, null);
				case "details":
					return UpdateTodo(todoId, null, value, null, null);
				case "duedate":
					return UpdateTodo(todoId, null, null, value.ToLong().ToDateTime(), null);
				default:
					throw new ArgumentOutOfRangeException("name");
			}
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult XUpdateIssue(string pk, string name, string value) {
			var issueId = pk.ToLong();
			switch (name) {
				case "title":
					return UpdateIssue(issueId, DateTime.UtcNow, value, null, null);
				case "details":
					return UpdateIssue(issueId, DateTime.UtcNow, null, value, null);
				case "owner":
					return UpdateIssue(issueId, DateTime.UtcNow, null, null, value.ToLong());
				default:
					throw new ArgumentOutOfRangeException("name");
			}
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodoCompletion(long id, long todoId, bool @checked, string connectionId = null) {
			var recurrenceId = id;
			L10Accessor.UpdateTodo(GetUser(), todoId, complete: @checked, connectionId: connectionId, duringMeeting: true);
			return Json(ResultObject.SilentSuccess(@checked));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateTodoDate(long id, long date) {
			var todo = id;
			var dateR = date.ToDateTime();
			L10Accessor.UpdateTodo(GetUser(), id, dueDate: dateR);
			return Json(ResultObject.SilentSuccess(date.ToString()));
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateRockCompletion(long id, long rockId, RockState state, string connectionId = null) {
			var recurrenceId = id;
			L10Accessor.UpdateRockCompletion(GetUser(), recurrenceId, rockId, state, connectionId);
			return Json(ResultObject.SilentSuccess(state.ToString()));
		}



		#endregion

		#region Headlines

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult UpdateHeadline(long id, string message) {
			var recurrenceId = id;
			L10Accessor.UpdateHeadline(GetUser(), id, message);
			return Json(ResultObject.SilentSuccess());
		}


		#endregion

		#region Notes
		public class NoteVM {
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
			public string PadId { get; set; }
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult Note(long id) {
			var note = L10Accessor.GetNote(GetUser(), id);
			return Json(ResultObject.SilentSuccess(new NoteVM() {
				Contents = note.Contents,
				Name = note.Name,
				NoteId = id
			}), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateNote(long recurrence) {
			return PartialView(new NoteVM() { RecurrenceId = recurrence });
		}
		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult CreateNote(NoteVM model) {
			ValidateValues(model, x => x.RecurrenceId);
			if (ModelState.IsValid) {
				var padId = L10Accessor.CreateNote(GetUser(), model.RecurrenceId, model.Name);
				model.PadId = padId;
				return Json(ResultObject.SilentSuccess(model).NoRefresh());
			}
			return Json(ResultObject.CreateMessage(StatusType.Danger, "Error creating note").NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult EditNote(NoteVM model) {
			var cache = new Cache();
			cache.Get(CacheKeys.LAST_SEND_NOTE_TIME);

			if (cache.Get(CacheKeys.LAST_SEND_NOTE_TIME) == null || model.SendTime > (long)cache.Get(CacheKeys.LAST_SEND_NOTE_TIME)) {
				cache.Push(CacheKeys.LAST_SEND_NOTE_TIME, model.SendTime, LifeTime.Request/*Session*/);
				L10Accessor.EditNote(GetUser(), model.NoteId, model.Contents, model.Name, model.ConnectionId);
				return Json(ResultObject.SilentSuccess(model).NoRefresh());
			}
			return Json(ResultObject.SilentSuccess("stale").NoRefresh());
		}

		public class DeleteNoteVM {
			public long NoteId { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult DeleteNote(long id) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Note(id));
			return PartialView(new DeleteNoteVM() { NoteId = id });
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public JsonResult DeleteNote(DeleteNoteVM model) {
			using (var s = HibernateSession.GetCurrentSession()) {
				using (var tx = s.BeginTransaction()) {
					PermissionsUtility.Create(s, GetUser()).ViewL10Note(model.NoteId);
					var n = s.Get<L10Note>(model.NoteId);
					n.DeleteTime = DateTime.UtcNow;

					var notes = s.QueryOver<L10Note>().Where(x => x.DeleteTime == null && x.Recurrence.Id == n.Recurrence.Id).List().ToList();

					var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
					hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(n.Recurrence.Id)).update(new AngularRecurrence(n.Recurrence.Id) {
						Notes = AngularList.Create(
							AngularListType.ReplaceAll,
							notes.Select(x => new AngularMeetingNotes(x))
						)
					});
					s.Update(n);
					tx.Commit();
					s.Flush();
				}
			}
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}




		#endregion

		[Access(AccessLevel.UserOrganization)]
		public JsonResult Members(long id) {
			var recurrenceId = id;
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
			var result = recur._DefaultAttendees.Select(x => new {
				id = x.User.Id,
				name = x.User.GetName(),
				imageUrl = x.User.ImageUrl(true, ImageSize._32)
			});
			return Json(ResultObject.SilentSuccess(result), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult MoveIssueToVTO(long id) {
			var issue_recurrence = id;
			var vto_issue = L10Accessor.MoveIssueToVto(GetUser(), issue_recurrence);
			return Json(ResultObject.SilentSuccess(vto_issue.Id), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> MoveIssueFromVto(long id) {
			var vtoIssue = id;
			var recurIssue = await L10Accessor.MoveIssueFromVto(GetUser(), vtoIssue);
			return Json(ResultObject.SilentSuccess(recurIssue.Id), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult StatsLine(long id) {
			//var o = new Dictionary<string, object>();
			//dynamic o = new ExpandoObject();

			//o["options"] = new Dictionary<string, object>();
			//o["options"]["margin"]

			var data = L10Accessor.GetStatsData(GetUser(), id);

			return Content(data.ToJson(), "application/json");
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult SetVideoProvider(long recur, long provider) {
			L10Accessor.SetVideoProvider(GetUser(), recur, provider);
			VideoProviderAccessor.StartMeeting(GetUser(), GetUser().User, provider);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult SetJoinedVideo(long recur, long provider) {
			L10Accessor.SetJoinedVideo(GetUser(), GetUser().Id, recur, provider);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
	}
}