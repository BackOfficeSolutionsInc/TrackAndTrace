using RadialReview.Accessors;
using RadialReview.Models.Application;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class HeadlinesController : BaseController {
		public class HeadlineVM {
			public long[] RecurrenceIds { get; set; }
			public long CreatedBy { get; set; }
			public string Message { get; set; }
			public List<NameId> PossibleRecurrences { get; set; }
			public List<SelectListItem> PossibleOwners { get; set; }
			public long? AboutId { get; set; }
			public string AboutIdText { get; set; }
			public long? MeetingId { get; set; }
			public bool ShowRecurrences { get; set; }
			public bool ShowOwners { get; set; }
			public long OwnerId { get; set; }
			public string Details { get; set; }

			public HeadlineVM() {
				RecurrenceIds = new long[0];
			}

			public List<PeopleHeadline> ToPeopleHeadline() {


				return RecurrenceIds.Select(x => new PeopleHeadline() {
					AboutId = AboutId,
					AboutName = AboutIdText,
					Message = Message,
					CreatedDuringMeetingId = MeetingId,
					OwnerId = OwnerId,
					RecurrenceId = x,
					_Details = Details,
				}).ToList();
			}

		}


		[Access(AccessLevel.UserOrganization)]
		public async Task<ActionResult> Pad(long id, bool showControls = true,bool readOnly = false) {
			try {
				var headline = HeadlineAccessor.GetHeadline(GetUser(), id);
				var padId = headline.HeadlinePadId;
				if (readOnly || !_PermissionsAccessor.IsPermitted(GetUser(), x => x.EditHeadline(id))) {
					padId = await PadAccessor.GetReadonlyPad(headline.HeadlinePadId);
				}
                //this is to choose what to use between Noteserves or firepad
                var firePadRef = PadAccessor.getFirePadRef(padId);
                if (firePadRef == null){
                    return Redirect(Config.NotesUrl("p/" + padId + "?showControls=" + (showControls ? "true" : "false") + "&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
                }
                else{
                    
                    return Redirect("~/FirePad/FirePad?PadId=" + padId);
                }
                
			} catch (Exception e) {
				Response.StatusCode = 400;
				return Content("<span class='error'>Could not load.</span>");
			}
		}

		[Access(AccessLevel.UserOrganization)]
		public PartialViewResult Modal(long? recurrenceId = null, bool? listRecurrences = null, bool? listOwners = null, long? owner = null, long? meetingId = null) {
			var _listRecur = listRecurrences ?? true;

			if (recurrenceId != null && listRecurrences == null)
				_listRecur = false;

			var _listOwners = listOwners ?? !_listRecur;

			var model = new HeadlineVM {
				CreatedBy = GetUser().Id,
				ShowRecurrences = _listRecur,
				MeetingId = meetingId,
				ShowOwners = _listOwners
			};
			if (recurrenceId != null)
				model.RecurrenceIds = new[] { recurrenceId.Value };


			//L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id, false);

			model.PossibleRecurrences = (_listRecur == true)
				? HeadlineAccessor.GetRecurrencesWithHeadlines(GetUser(), GetUser().Id)
				: new List<NameId>();

			if (recurrenceId == null && _listRecur && model.PossibleRecurrences.Any()) {
				model.RecurrenceIds = new[] { model.PossibleRecurrences.First().Id };
			}

			if (recurrenceId != null && _listOwners && !_listRecur) {
				model.PossibleOwners = L10Accessor.GetAttendees(GetUser(), recurrenceId.Value).ToSelectList(x => x.GetName(), x => x.Id, GetUser().Id);
			} else if (_listOwners) {
				model.PossibleOwners = TinyUserAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.GetName(), x => x.UserOrgId);
			}

			model.OwnerId = owner ?? GetUser().Id;


			return PartialView(model);
		}


		[Access(AccessLevel.UserOrganization)]
		public ActionResult EditModal(long id) {
			var headline = HeadlineAccessor.GetHeadline(GetUser(), id);
			return PartialView(headline);
		}


		[Access(AccessLevel.UserOrganization)]
		[HttpPost]
		public async Task<JsonResult> EditModal(PeopleHeadline model) {
			await HeadlineAccessor.UpdateHeadline(GetUser(), model.Id, model.Message, model.AboutId, model.AboutIdText);
			return Json(ResultObject.SilentSuccess());
		}


		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(HeadlineVM model) {
			ValidateValues(model, x => x.CreatedBy, x => x.MeetingId);
			if (model.AboutId < 0) {
				model.AboutId = null;
			}
			var phs = model.ToPeopleHeadline();
			foreach (var ph in phs) {
				ph.OrganizationId = GetUser().Organization.Id;
				await HeadlineAccessor.CreateHeadline(GetUser(), ph);
			}
			return Json(ResultObject.SilentSuccess());
		}

		public class CopyHeadlineVM {
			public long HeadlineId { get; set; }
			public long CopyIntoRecurrenceId { get; set; }

			public List<L10Recurrence> PossibleRecurrences { get; set; }

			public String Message { get; set; }
			public string Details { get; set; }
		}

		[Access(AccessLevel.UserOrganization)]
		public async Task<PartialViewResult> CopyModal(long headlineid, long? copyto = null) {
			var i = HeadlineAccessor.GetHeadline(GetUser(), headlineid);

			copyto = copyto ?? i.RecurrenceId;
			var details = "";
			try {
				details = await PadAccessor.GetText(i.HeadlinePadId);
			} catch (Exception) {
			}

			var model = new CopyHeadlineVM() {
				HeadlineId = i.Id,
				Message = i.Message,
				Details = details,
				CopyIntoRecurrenceId = copyto.Value,
				PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), i.RecurrenceId)
			};
			return PartialView("CopyHeadline", model);
		}
		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> CopyModal(CopyHeadlineVM model) {
			var copied = await HeadlineAccessor.CopyHeadline(GetUser(), model.HeadlineId, model.CopyIntoRecurrenceId);
			model.PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), copied.RecurrenceId);

			//L10Accessor.UpdateHeadline(GetUser(), model.ParentIssue_RecurrenceId, DateTime.UtcNow, complete: true, connectionId: "");
			return Json(ResultObject.Success("Copied People Headline").NoRefresh());
		}

	}
}