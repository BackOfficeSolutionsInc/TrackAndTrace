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
using RadialReview.Models.L10;
using RadialReview.Models.Angular.Headlines;
using RadialReview.Models.Angular.Notifications;
using RadialReview.Notifications;
using RadialReview.Models.Angular.Rocks;
using RadialReview.Models.Enums;
using RadialReview.Models.Angular.CoreProcess;
using RadialReview.Areas.CoreProcess.Accessors;

namespace RadialReview.Controllers {
	public partial class L10Controller : BaseController {

		#region Ancillary
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularBasics(AngularBasics model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularMeetingNotes(AngularMeetingNotes model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		#endregion

		#region Scorecard
		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		[Untested("Test me")]
		public async Task<JsonResult> AddAngularMeasurable(long id) {
			var recurrenceId = id;
			var creator = MeasurableCreation.CreateMeasurable(null, 0, UnitType.None, LessGreater.GreaterThan, GetUser().Id, GetUser().Id);
			//await L10Accessor.CreateMeasurable(GetUser(), recurrenceId, AddMeasurableVm.CreateMeasurableViewModel(recurrenceId, new MeasurableModel() {
			//             OrganizationId = GetUser().Organization.Id,
			//             AdminUserId = GetUser().Id,
			//             AccountableUserId = GetUser().Id,
			//         }, true));
			await ScorecardAccessor.CreateMeasurable(GetUser(), creator);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularMeasurable(long recurrenceId, AngularMeasurable model, string connectionId = null) {
			//var recurrenceId = id;
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult OrderAngularMeasurable(long id, long recurrence, int oldOrder, int newOrder) {
			L10Accessor.OrderAngularMeasurable(GetUser(), id, recurrence, oldOrder, newOrder);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		[Untested("test me")]
		public async Task<JsonResult> UpdateAngularMeasurable(AngularMeasurable model, string connectionId = null, bool historical = false, decimal? lower = null, decimal? upper = null, bool? enableCumulative = null, DateTime? cumulativeStart = null/*, UnitType? Modifier=null*/) {
			var target = model.Target;
			var altTarget = (decimal?)null;
			if (model.Direction == Models.Enums.LessGreater.Between) {
				target = lower;
				altTarget = upper;
			}


			await ScorecardAccessor.UpdateMeasurable(GetUser(), model.Id, model.Name, model.Direction, target,
				model.Owner.NotNull(x => x.Id), model.Admin.NotNull(x => x.Id), connectionId, !historical, altTarget, enableCumulative, cumulativeStart, model.Modifiers);


			//L10Accessor.UpdateArchiveMeasurable(GetUser(),
			//	model.Id, model.Name, model.Direction, target,
			//	model.Owner.NotNull(x => x.Id), model.Admin.NotNull(x => x.Id),
			//	connectionId, !historical, altTarget, enableCumulative, cumulativeStart, model.Modifiers);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularScore(AngularScore model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		#endregion

		#region Rocks
		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddAngularRock(long id) {
			var recurrenceId = id;
			//var rock = new RockModel() {
			//	OrganizationId = GetUser().Organization.Id,
			//	ForUserId = GetUser().Id,
			//};
			var rock = await L10Accessor.CreateAndAttachRock(GetUser(), recurrenceId, GetUser().Id, null);
			return Json(ResultObject.SilentSuccess(new AngularRock(rock, false)), JsonRequestBehavior.AllowGet);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularRock(AngularRock model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularRock(long recurrenceId, AngularRock model, string connectionId = null) {
			//var recurrenceId = id;
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion

		#region Todos
		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddAngularTodo(long id) {
			var recurrenceId = id;
			//await TodoAccessor.CreateTodo(GetUser(), recurrenceId, new TodoModel() {
			//    ForRecurrenceId=id
			//});

			var todoModel = TodoCreation.CreateL10Todo(null, null, GetUser().Id, null, recurrenceId);
			await TodoAccessor.CreateTodo(GetUser(), todoModel);

			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularTodo(AngularTodo model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularTodo(long recurrenceId, AngularTodo model, string connectionId = null) {
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion

		#region Issues
		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddAngularIssue(long id) {
			var recurrenceId = id;
			var creation = IssueCreation.CreateL10Issue(null, null, GetUser().Id, recurrenceId);
			await IssuesAccessor.CreateIssue(GetUser(), creation);// recurrenceId, GetUser().Id, new IssueModel());
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularIssue(AngularIssue model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularIssue(long recurrenceId, AngularIssue model, string connectionId = null) {
			//var recurrenceId = id;
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion

		#region Headline

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularHeadline(AngularHeadline model, string connectionId = null) {
			await L10Accessor.Update(GetUser(), model, connectionId);
			return Json(ResultObject.SilentSuccess());
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularHeadline(long recurrenceId, AngularHeadline model, string connectionId = null) {
			//var recurrenceId = id;
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		//[HttpGet]
		//[Access(AccessLevel.UserOrganization)]
		//public async Task<JsonResult> AddAngularHeadline(long id) {
		//	var recurrenceId = id;
		//	await HeadlineAccessor.CreateHeadline(GetUser(), new PeopleHeadline() {

		//	});
		//	return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		//}
		#endregion

		#region Attendeees

		[HttpGet]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddAngularUser(long id, long userid) {
			var recurrenceId = id;
			await L10Accessor.AddAttendee(GetUser(), recurrenceId, userid);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> RemoveAngularUser(long recurrenceId, AngularUser model, string connectionId = null) {
			//var recurrenceId = id;
			await L10Accessor.Remove(GetUser(), model, recurrenceId, connectionId);
			return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
		}
		#endregion

		#region Notifications		
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public JsonResult UpdateAngularNotification(AngularNotification model, string connectionId = null) {
			if (model.Seen != null) {
				var status = model.Seen.Value;
				PubSub.SetSeenStatus(GetUser(), GetUser().Id, model.Id, status, connectionId);
				if (model.DetailsList != null) {
					foreach (var d in model.DetailsList)
						PubSub.SetSeenStatus(GetUser(), GetUser().Id, d.Id, status, connectionId);
				}
			}
			return Json(ResultObject.SilentSuccess());
		}

		#endregion

		#region CoreProcess
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> UpdateAngularTask(AngularTask model, string connectionId = null) {
			if (model.CompleteTime != null)
				await new ProcessDefAccessor().TaskComplete(GetUser(), model.Id);
			else if (model.Assigned != null)
				await new ProcessDefAccessor().TaskClaimOrUnclaim(GetUser(), model.Id, GetUser().Id, model.Assigned.Value);

			return Json(ResultObject.SilentSuccess());
		}
		#endregion
	}
}