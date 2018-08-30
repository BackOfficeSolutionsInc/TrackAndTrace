using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Accessors.TodoIntegrations;
using RadialReview.Models.Angular.Base;
using RadialReview.Models.Angular.Todos;
using RadialReview.Models.Enums;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.Permissions;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Todo;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models.Angular.Meeting;
using RadialReview.Utilities;
using RadialReview.Exceptions;

namespace RadialReview.Controllers {
	public class TodoController : BaseController {
		//[Access(AccessLevel.UserOrganization)]
		//public ActionResult Previous(long id) {
		//    var recurrenceId = id;
		//    var model = L10Accessor.GetPreviousTodos(GetUser(), recurrenceId);
		//    return View(model);
		//}

		public class MeetingVm {
			public long id { get; set; }
			public string name { get; set; }
		}
		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTodoRecurrence(long? id = null) {

			var model = new TodoVM(GetUser().Id, GetUser()) {
				ByUserId = GetUser().Id,
				AccountabilityId = new[] { GetUser().Id },
				MeetingId = -1,
				RecurrenceId = id ?? -2
			};

			var meetings = L10Accessor.GetVisibleL10Recurrences(GetUser(), GetUser().Id, false)
				.Where(x => x.IsAttendee == true)
				.Select(x => new MeetingVm { name = x.Recurrence.Name, id = x.Recurrence.Id })
				.OrderBy(x => x.name)
				.ToList();

			meetings.Add(new MeetingVm() {
				name = "Personal To-do list",
				id = -2 // Personal todo list
			});
			ViewBag.PossibleMeetings = meetings;

			return PartialView("CreateTodoRecurrence", model);
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pad(long id, bool showControls = true, bool readOnly = false) {
			try {
				var todo = TodoAccessor.GetTodo(GetUser(), id);
				var padId = todo.PadId;
                
                if (readOnly || !_PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTodo(id))) {
					padId = await PadAccessor.GetReadonlyPad(todo.PadId);
				}
                //this is to choose what to use between Noteserves or firepad
                return Redirect(PadAccessor.GetNotesURL(padId, showControls,GetUser().GetName()));
            } catch (Exception e) {
				return RedirectToAction("Index", "Error");
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult EditModal(long id, bool showMeeting = true) {

			if (id > 0) {
				var todo = TodoAccessor.GetTodo(GetUser(), id);

				var meetings = L10Accessor.GetVisibleL10Recurrences(GetUser(), GetUser().Id, false)
				.Where(t => t.IsAttendee == true)
			   .Select(x => new MeetingVm { name = x.Recurrence.Name, id = x.Recurrence.Id })
			   .OrderBy(x => x.name)
			   .ToList();

				ViewBag.Originating = "";
				ViewBag.ShowMeeting = showMeeting;

				meetings.Add(new MeetingVm() {
					name = "Individual To-do List",
					id = -2 // Personal todo list
				});


				if (todo.TodoType == TodoType.Personal) {
					ViewBag.Originating = "Individual To-do List";

					todo.ForRecurrenceId = -2;
				} else {
					ViewBag.Originating = todo.ForRecurrence.Name;
				}

				ViewBag.PossibleMeetings = meetings;

				ViewBag.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTodo(id));

                //start Creating firepad
                var padId = todo.PadId;
                ViewBag.firePadRef = PadAccessor.GetFirePadRef(padId);
                //end creating firepad
                return PartialView(todo);
			} else {
				return RedirectToAction("Modal", "Milestone", new { id = -id });
			}
		}

		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> EditModal(TodoModel model, string completed = null) {

			if (model.ForRecurrenceId == -2) {
				model.ForRecurrenceId = null;
				model.ForRecurrence = null;
				model.TodoType = TodoType.Personal;
			}

			await TodoAccessor.UpdateTodo(GetUser(), model.Id, model.Message, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate), model.AccountableUserId, completed.ToBooleanJS(), source: model.GetListSource());
			return Json(ResultObject.SilentSuccess());
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateTodoRecurrence(TodoVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.AccountabilityId);
			if (model.MeetingId != -1 && model.MeetingId != -2)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			TodoCreation todo;
			if (model.RecurrenceId == -2) {
				todo = TodoCreation.CreatePersonalTodo(model.Message ?? "", model.Details, GetUser().Id, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate)/*.AddMinutes(adjust)*/);
			} else {
				todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message ?? "", model.Details, GetUser().Id, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate)/*.AddMinutes(adjust)*/, model.MeetingId);
			}
			//await TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, todoModel);
			await TodoAccessor.CreateTodo(GetUser(), todo);

			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult CreateTodo(long recurrence, long meeting = -1, string todo = null, long? modelId = null, string modelType = null,string details=null) {
			if (meeting != -1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();
			var model = new TodoVM(recur.GetDefaultTodoOwner(GetUser()), GetUser()) {
				ForModelId = modelId,
				ForModelType = modelType,
				Message = todo,
				ByUserId = GetUser().Id,
				Details = details,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView("CreateTodo", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateTodo(TodoVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId);
			if (model.MeetingId != -1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			foreach (var a in model.AccountabilityId) {
				var todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message, model.Details, a, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate), model.MeetingId, model.ForModelType ?? "TodoModel", model.ForModelId ?? -1);
				await TodoAccessor.CreateTodo(GetUser(), todo);
			}
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateScorecardTodo(long meeting, long recurrence, long measurable, long score, long? accountable = null) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			ScoreModel s = null;
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			try {
				if (score == 0 && accountable.HasValue) {
					var shift = System.TimeSpan.FromDays((recur.CurrentWeekHighlightShift + 1) * 7 - .0001);
					var week = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrence, true, false, false).CreateTime.Add(shift).StartOfWeek(DayOfWeek.Sunday);
					var scores = (await L10Accessor.GetOrGenerateScoresForRecurrence(GetUser(), recurrence)).Where(x => x.MeasurableId == measurable && x.AccountableUserId == accountable.Value && x.ForWeek == week);
					s = scores.FirstOrDefault();
					if (s == null) {
						//s = ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrence, 0, week, measurable, null, null, null);
						s = await ScorecardAccessor.UpdateScore(GetUser(), 0, measurable, week, null);
					}

				} else {
					s = ScorecardAccessor.GetScore(GetUser(), score);
					//s = ScorecardAccessor.GetScoreInMeeting(GetUser(), score, recurrence);
				}
			} catch (Exception e) {
				log.Error("Todo/Modal", e);
			}

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			bool useMessage = true;

			if (s != null && score == 0 && accountable.HasValue) {
				s.AccountableUser = people.FirstOrDefault(x => x.Id == s.AccountableUserId);
				s.Measurable.AccountableUser = s.AccountableUser;
				s.Measurable.AdminUser = s.AccountableUser;
			}

			string message = null;
			try {
				if (s != null && useMessage) {
					message = await s.GetTodoMessage();
				}
			} catch (Exception) { }

			string details = null;
			try {
				if (s != null && useMessage) {
					details = await s.GetTodoDetails();
				}
			} catch (Exception) { }



			var model = new ScoreCardTodoVM(recur.GetDefaultTodoOwner(GetUser()), GetUser()) {
				ByUserId = GetUser().Id,
				Message = message,
				Details = details,
				MeasurableId = measurable,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = new[] { accountable ?? recur.GetDefaultTodoOwner(GetUser()) },
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView("ScorecardTodoModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateScorecardTodo(ScoreCardTodoVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId, x => x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			foreach (var m in model.AccountabilityId) {
				var todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message, model.Details, m, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate), model.MeetingId, "MeasurableModel", model.MeasurableId);
				await TodoAccessor.CreateTodo(GetUser(), todo);

			}
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateRockTodo(long meeting, long recurrence, long? rock = null, long? rockId = null, long? accountable = null) {

			//Supply either rock or rockId
			var rr = rock ?? rockId;
			if (!rr.HasValue) {
				throw new PermissionsException("Rock Id blank");
			}
			var r = rr.Value;
			
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = RockAccessor.GetRockInMeeting(GetUser(), r, meeting);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var model = new RockTodoVM(recur.GetDefaultTodoOwner(GetUser()), GetUser()) {
				ByUserId = GetUser().Id,
				Message = await s.NotNull(async x => await x.GetTodoMessage()),
				Details = await s.NotNull(async x => await x.GetTodoDetails()),
				RockId = r,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = new[] { accountable ?? recur.GetDefaultTodoOwner(GetUser()) },
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView("RockTodoModal", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateRockTodo(RockTodoVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RockId, x => x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));


			foreach (var m in model.AccountabilityId) {
				//await TodoAccessor.CreateTodo(GetUser(), model.RecurrenceId, new TodoModel() {
				//    CreatedById = GetUser().Id,
				//    ForRecurrenceId = model.RecurrenceId,
				//    CreatedDuringMeetingId = model.MeetingId,
				//    Message = model.Message ?? "",
				//    Details = model.Details ?? "",
				//    ForModel = "RockModel",
				//    ForModelId = model.RockId,
				//    Organization = GetUser().Organization,
				//    AccountableUserId = m,
				//    DueDate = model.DueDate
				//});
				var todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message ?? "", model.Details, m, model.DueDate, model.MeetingId, "RockModel", model.RockId);
				await TodoAccessor.CreateTodo(GetUser(), todo);
			}
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateTodoFromHeadline(long meeting, long recurrence, long headline, long? accountable = null) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

			var s = HeadlineAccessor.GetHeadline(GetUser(), headline);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			//get Notes
			s._Notes = await PadAccessor.GetText(s.HeadlinePadId);

			var model = new HeadlineTodoVm(recur.GetDefaultTodoOwner(GetUser()), GetUser()) {
				ByUserId = GetUser().Id,
				Message = await s.NotNull(async x => await x.GetTodoMessage()),
				Details = await s.NotNull(async x => await x.GetTodoDetails()),
				HeadlineId = headline,
				MeetingId = meeting,
				RecurrenceId = recurrence,
				AccountabilityId = new[] { accountable ?? recur.GetDefaultTodoOwner(GetUser()) },
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView("HeadlineTodoModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CreateTodoFromHeadline(HeadlineTodoVm model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.HeadlineId, x => x.RecurrenceId);
			_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			foreach (var m in model.AccountabilityId) {
				var todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message, model.Details, m, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate), model.MeetingId, "PeopleHeadline", model.HeadlineId);
				await TodoAccessor.CreateTodo(GetUser(), todo);
			}
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}



		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CreateTodoFromIssue(long issue, long recurrence, long? meeting = null) {
			if (meeting != null)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting.Value));
			var i = IssuesAccessor.GetIssue(GetUser(), issue);
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var model = new TodoFromIssueVM(recur.GetDefaultTodoOwner(GetUser()), GetUser()) {
				Message = await i.NotNull(async x => await x.GetTodoMessage()),
				Details = await i.NotNull(async x => await x.GetTodoDetails()),
				ByUserId = GetUser().Id,
				MeetingId = meeting ?? -1,
				IssueId = issue,
				RecurrenceId = recurrence,
				AccountabilityId = new[] { L10Accessor.GuessUserId(i, recur.GetDefaultTodoOwner(GetUser())) },
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView("CreateTodoFromIssue", model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		//[Untested("Create Todo")]
		public async Task<JsonResult> CreateTodoFromIssue(TodoFromIssueVM model) {
			ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.IssueId);
			if (model.MeetingId != -1)
				_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

			foreach (var m in model.AccountabilityId) {
				var todo = TodoCreation.CreateL10Todo(model.RecurrenceId, model.Message, model.Details, m, GetUser().GetTimeSettings().ConvertToServerTime(model.DueDate), model.MeetingId, "IssueModel", model.IssueId);
				await TodoAccessor.CreateTodo(GetUser(), todo);
			}
			return Json(ResultObject.SilentSuccess().NoRefresh());
		}

		public class LinkExternalTodo {
			[Display(Name = "For User")]
			public long UserId { get; set; }
			[Display(Name = "Account Type")]
			public ExternalTodoType Account { get; set; }
			public List<AccountableUserVM> PossibleUsers { get; set; }
			public long RecurrenceId { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult LinkToExternal(long recurrence, long user = 0) {
			var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
			var people = recur._DefaultAttendees.Select(x => x.User).ToList();
			people.Add(GetUser());
			people = people.Distinct(x => x.Id).ToList();

			var model = new LinkExternalTodo() {
				RecurrenceId = recurrence,
				UserId = user,
				PossibleUsers = people.Select(x => new AccountableUserVM() {
					id = x.Id,
					imageUrl = x.ImageUrl(true, ImageSize._32),
					name = x.GetName()
				}).ToList(),
			};
			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult LinkToExternal(LinkExternalTodo model) {
			ValidateValues(model, x => x.RecurrenceId);
			switch (model.Account) {
				case ExternalTodoType.Trello:
					return Json(ResultObject.SilentSuccess(TrelloAccessor.AuthUrl(GetUser(), model.RecurrenceId, model.UserId)));
				case ExternalTodoType.Basecamp:
					return Json(ResultObject.SilentSuccess(BaseCampAccessor.AuthUrl(GetUser(), model.RecurrenceId, model.UserId)));
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
		[Access(AccessLevel.UserOrganization)]
		public JsonResult DetachLink(long id) {
			ExternalTodoAccessor.DetatchLink(GetUser(), id);
			return Json(ResultObject.SilentSuccess(true), JsonRequestBehavior.AllowGet);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult List(long? id = null) {
			return View(id ?? GetUser().Id);
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult AjaxList(long? id = null) {
			return PartialView("~/Views/Todo/Partial/list.cshtml", id ?? GetUser().Id);
		}

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = TodoAccessor.Listing(GetUser(), GetUser().Organization.Id);
			return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}

		[Access(AccessLevel.UserOrganization)]
		public JsonResult ForUser(long? id = null, long? start = null, long? end = null) {

			DateRange range = null;
			if (!(start == null && end == null)) {
				range = new DateRange(start, end);
			}
			IEnumerable<AngularTodo> todos;
			if (id == null) {
				todos = TodoAccessor.GetMyTodosAndMilestones(GetUser(), id ?? GetUser().Id, range: range);//.Select(x => new AngularTodo(x));
			} else {
				todos = TodoAccessor.GetTodosForUser(GetUser(), id ?? GetUser().Id, range: range);//.Select(x => new AngularTodo(x));
			}

			var angular = new AngularRecurrence(-1) {
				Todos = todos
			};

			return Json(angular, JsonRequestBehavior.AllowGet);
		}




	}
}
