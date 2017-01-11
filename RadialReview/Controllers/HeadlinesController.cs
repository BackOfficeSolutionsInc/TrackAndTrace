using RadialReview.Accessors;
using RadialReview.Models.Json;
using RadialReview.Models.L10;
using RadialReview.Models.L10.VM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
	public class HeadlinesController : BaseController {
		public class HeadlineVM {
			public long[] RecurrenceIds { get; set; }
			public long CreatedBy { get; set; }
			public string Message { get; set; }
			public List<L10VM> PossibleRecurrences { get; set; }
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
                

				return RecurrenceIds.Select(x=>new PeopleHeadline() {
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
			

			model.PossibleRecurrences = (_listRecur == true) 
				? L10Accessor.GetVisibleL10Recurrences(GetUser(), GetUser().Id, false)
					.Where(x=>x.Recurrence.HeadlineType==Model.Enums.PeopleHeadlineType.HeadlinesList)
					.ToList() 
				: new List<Models.L10.VM.L10VM>();

			if (_listRecur && model.PossibleRecurrences.Any()) {
				model.RecurrenceIds = new[] { model.PossibleRecurrences.First().Recurrence.Id };
			}

			if (recurrenceId != null && _listOwners && !_listRecur) {
				model.PossibleOwners = L10Accessor.GetAttendees(GetUser(), recurrenceId.Value).ToSelectList(x => x.GetName(), x => x.Id, GetUser().Id);
			} else if (_listOwners) {
				model.PossibleOwners = TinyUserAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id).ToSelectList(x => x.GetName(), x => x.UserOrgId);
			}

			model.OwnerId = owner ?? GetUser().Id;


			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> Modal(HeadlineVM model) {
			ValidateValues(model,x => x.CreatedBy, x => x.MeetingId);
            if (model.AboutId < 0){
                model.AboutId = null;
            }
			var phs = model.ToPeopleHeadline();
			foreach (var ph in phs) {
				ph.OrganizationId = GetUser().Organization.Id;
				await HeadlineAccessor.CreateHeadline(GetUser(), ph);
			}
			return Json(ResultObject.SilentSuccess());
		}
	}
}