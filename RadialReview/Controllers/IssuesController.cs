using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Amazon.ElasticTranscoder.Model;
using Amazon.IdentityManagement.Model;
using RadialReview.Accessors;
using RadialReview.Models.Issues;
using RadialReview.Models.Json;
using RadialReview.Models.Scorecard;
using RadialReview.Utilities;

namespace RadialReview.Controllers {
    public class IssuesController : BaseController {

        [Access(AccessLevel.UserOrganization)]
        public async Task<ActionResult> Pad(long id) {
            try {
                var issue = IssuesAccessor.GetIssue(GetUser(), id);
                var padId = issue.PadId;
                if (!_PermissionsAccessor.IsPermitted(GetUser(), x => x.EditIssue(id))) {
                    padId = await PadAccessor.GetReadonlyPad(issue.PadId);
                }
                return Redirect(Config.NotesUrl("p/" + padId + "?showControls=true&showChat=false&showLineNumbers=false&useMonospaceFont=false&userName=" + Url.Encode(GetUser().GetName())));
            } catch (Exception) {
                return RedirectToAction("Index", "Error");
            }
        }

        #region From Todo
        [Access(AccessLevel.UserOrganization)]
        public async Task<PartialViewResult> IssueFromTodo(long recurrence, long todo, long meeting) {
            //var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);
            //copyto = copyto ?? i.Recurrence.Id;
            _PermissionsAccessor.Permitted(GetUser(), x =>
                x.ViewL10Meeting(meeting)
                 .ViewL10Recurrence(recurrence)
            );

            var todoModel = TodoAccessor.GetTodo(GetUser(), todo);
            var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
            var possible = recur._DefaultAttendees
                .Select(x => x.User)
                .Select(x => new IssueVM.AccountableUserVM() {
                    id = x.Id,
                    imageUrl = x.ImageUrl(true, ImageSize._32),
                    name = x.GetName()
                }).ToList();

            var model = new IssueVM() {
                //IssueId = i.Issue.Id,
                RecurrenceId = recurrence,
                Message = await todoModel.NotNull(async x => await x.GetIssueMessage()),
                Details = await todoModel.NotNull(async x => await x.GetIssueDetails()),
                ByUserId = GetUser().Id,
                MeetingId = meeting,
                ForId = todo,
                PossibleUsers = possible,
                OwnerId = GetUser().Id,
            };
            return PartialView("CreateIssueModal", model);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> IssueFromTodo(IssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.ForId);
            _PermissionsAccessor.Permitted(GetUser(), x =>
                x.ViewL10Meeting(model.MeetingId)
                 .ViewL10Recurrence(model.RecurrenceId)
            );


            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message ?? "",
                Description = model.Details ?? "",
                ForModel = "TodoModel",
                ForModelId = model.ForId,
                Organization = GetUser().Organization,
                _Priority = model.Priority
            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
        }
        #endregion


        /// <summary>
        /// Copy an issue
        /// </summary>
        /// <param name="copyto"></param>
        /// <param name="recurrence_issue"></param>
        /// <returns></returns>
        [Access(AccessLevel.UserOrganization)]
        public async Task<PartialViewResult> CopyModal(long recurrence_issue, long? copyto = null) {
            var i = IssuesAccessor.GetIssue_Recurrence(GetUser(), recurrence_issue);

            copyto = copyto ?? i.Recurrence.Id;
            var details = "";
            try {
                details = await PadAccessor.GetText(i.Issue.PadId);
            } catch (Exception) {
            }

            var model = new CopyIssueVM() {
                IssueId = i.Issue.Id,
                Message = i.Issue.Message,
                Details = details,//i.Issue.Description,
                ParentIssue_RecurrenceId = i.Id,
                CopyIntoRecurrenceId = copyto.Value,
                PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), i.Recurrence.Id)
            };
            return PartialView("CopyIssueModal", model);
        }


        //[Access(AccessLevel.UserOrganization)]
        //public PartialViewResult EditModal(long id) {
        //	var todo = TodoAccessor.GetTodo(GetUser(), id);

        //	ViewBag.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditTodo(id));
        //	return PartialView(todo);
        //}

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult EditModal(long id) {

            var issueRecurrence = IssuesAccessor.GetIssue_Recurrence(GetUser(), id);

            ViewBag.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.EditIssueRecurrence(id));
            return PartialView(new IssueVM() {
                Priority = issueRecurrence.Priority,
                Message = issueRecurrence.Issue.Message,
                OwnerId = issueRecurrence.Owner.Id,
                IssueId = issueRecurrence.Issue.Id,
                IssueRecurrenceId = issueRecurrence.Id,
            });
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<JsonResult> EditModal(IssueVM model) {

            await L10Accessor.UpdateIssue(GetUser(), model.IssueRecurrenceId, DateTime.UtcNow, model.Message, null, null, null, model.OwnerId, model.Priority);

            //var todo = IssuesAccessor.EditIssue(GetUser(), model.IssueRecurrenceId, model.Message, model.OwnerId,model.Priority);
            return Json(ResultObject.SilentSuccess());
        }



        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> CopyModal(CopyIssueVM model) {
            ValidateValues(model, x => x.ParentIssue_RecurrenceId, x => x.IssueId);
            var issue = IssuesAccessor.CopyIssue(GetUser(), model.ParentIssue_RecurrenceId, model.CopyIntoRecurrenceId);
            model.PossibleRecurrences = L10Accessor.GetAllConnectedL10Recurrence(GetUser(), issue.Recurrence.Id);

            await L10Accessor.UpdateIssue(GetUser(), model.ParentIssue_RecurrenceId, DateTime.UtcNow, awaitingSolve: true, connectionId: "");
            return Json(ResultObject.SilentSuccess().NoRefresh());
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateIssue(long recurrence, long meeting = -1, string issue = null, long? modelId = null, string modelType = null) {
            if (meeting != -1)
                _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

            var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);
            var people = recur._DefaultAttendees.Select(x => x.User).ToList();
            people.Add(GetUser());
            people = people.Distinct(x => x.Id).ToList();

            var possible = people.Select(x => new IssueVM.AccountableUserVM() {
                id = x.Id,
                imageUrl = x.ImageUrl(true, ImageSize._32),
                name = x.GetName()
            }).ToList();


            var prior = recur.Prioritization;
            var showPriority = false;
            if (/*prior == Models.L10.PrioritizationType.Invalid ||*/ prior == Models.L10.PrioritizationType.Priority)
                showPriority = true;


            var model = new IssueVM() {
                Message = issue,
                ByUserId = GetUser().Id,
                MeetingId = meeting,
                RecurrenceId = recurrence,
                PossibleUsers = possible,
                OwnerId = GetUser().Id,
                ForModelId = modelId,
                ForModelType = modelType,
                ShowPriority = showPriority
            };
            return PartialView("CreateIssueModal", model);
        }

        public class MeetingVm {
            public long id { get; set; }

            public string name { get; set; }
        }

        [Access(AccessLevel.UserOrganization)]
        public PartialViewResult CreateIssueRecurrence() {
            //if (meeting != -1)
            //	_PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

            ViewBag.PossibleMeetings = L10Accessor.GetVisibleL10Meetings_Tiny(GetUser(), GetUser().Id, true)
                .Select(x => new MeetingVm { name = x.Name, id = x.Id })
                .ToList();

            var model = new IssueVM() {
                ByUserId = GetUser().Id,
                MeetingId = -1,
                PossibleUsers = null,
                OwnerId = GetUser().Id
            };
            return PartialView("CreateIssueRecurrenceModal", model);
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> CreateIssueRecurrence(IssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.OwnerId, x => x.ForId);
            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message ?? "",
                Description = model.Details ?? "",
                ForModel = "IssueModel",
                ForModelId = -1,
                Organization = GetUser().Organization,
                _Priority = model.Priority

            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> CreateIssue(IssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RecurrenceId, x => x.ForId);
            if (model.MeetingId != -1)
                _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message ?? "",
                Description = model.Details ?? "",
                ForModel = model.ForModelType ?? "IssueModel",
                ForModelId = model.ForModelId ?? -1,
                Organization = GetUser().Organization,
                _Priority = model.Priority,

            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
        }

        [Access(AccessLevel.UserOrganization)]
        public async Task<PartialViewResult> Modal(long meeting, long recurrence, long measurable, long score, long? userid = null) {
            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

            ScoreModel s = null;
            var recur = L10Accessor.GetL10Recurrence(GetUser(), recurrence, true);

            try {
                if (score == 0 && userid.HasValue) {
                    var shift = System.TimeSpan.FromDays((recur.CurrentWeekHighlightShift + 1) * 7 - .0001);

                    var week = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrence, true, false, false).CreateTime.Add(shift).StartOfWeek(DayOfWeek.Sunday);
                    if (measurable > 0) {
                        var scores = L10Accessor.GetScoresForRecurrence(GetUser(), recurrence).Where(x => x.MeasurableId == measurable && x.AccountableUserId == userid.Value && x.ForWeek == week);
                        s = scores.FirstOrDefault();
                        //TODO actually just create the score
                        if (s == null) {
                            s = ScorecardAccessor.UpdateScoreInMeeting(GetUser(), recurrence, 0, week, measurable, null, null, null);
                        }
                    } else {
                        var scores = L10Accessor.GetScoresForRecurrence(GetUser(), recurrence).Where(x => x.Id == score && x.AccountableUserId == userid.Value && x.ForWeek == week);
                        s = scores.FirstOrDefault();
                    }
                } else {
                    s = ScorecardAccessor.GetScoreInMeeting(GetUser(), score, recurrence);
                }
            } catch (Exception e) {
                log.Error("Issues/Modal", e);
            }

            // var possibleUsers = recur._DefaultAttendees.Select(x => x.User).ToList();

            var people = recur._DefaultAttendees.Select(x => x.User).ToList();
            people.Add(GetUser());
            people = people.Distinct(x => x.Id).ToList();

            var possible = people
                .Select(x => new IssueVM.AccountableUserVM() {
                    id = x.Id,
                    imageUrl = x.ImageUrl(true, ImageSize._32),
                    name = x.GetName()
                }).ToList();

            bool useMessage = true;

            if (s != null && score == 0 && userid.HasValue) {
                s.AccountableUser = people.FirstOrDefault(x => x.Id == s.AccountableUserId);
                s.Measurable.AccountableUser = s.AccountableUser;
                s.Measurable.AdminUser = s.AccountableUser;
                //if (s.Measured == null)
                // useMessage = false;
            }

            string message = null;
            try {
                if (s != null && useMessage) {
                    message = await s.GetIssueMessage();
                }
            } catch (Exception) { }

            string details = null;
            try {
                if (s != null && useMessage) {
                    details = await s.GetIssueDetails();
                }
            } catch (Exception) { }

            var model = new ScoreCardIssueVM() {
                ByUserId = GetUser().Id,
                Message = message,
                Details = details,
                MeasurableId = measurable,
                MeetingId = meeting,
                RecurrenceId = recurrence,
                PossibleUsers = possible,
                OwnerId = s.NotNull(x => x.AccountableUserId)
            };
            return PartialView("ScorecardIssueModal", model);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> Modal(ScoreCardIssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.MeasurableId, x => x.RecurrenceId);
            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message,
                Description = model.Details,
                ForModel = "MeasurableModel",
                ForModelId = model.MeasurableId,
                Organization = GetUser().Organization,
                _Priority = model.Priority
            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
            //return PartialView("ScorecardIssueModal", model);
        }

        [Access(AccessLevel.UserOrganization)]
        public async Task<PartialViewResult> CreateRockIssue(long meeting, long rock) {
            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

            var s = RockAccessor.GetRockInMeeting(GetUser(), rock, meeting);
            var recur = L10Accessor.GetCurrentL10RecurrenceFromMeeting(GetUser(), meeting);

            var people = recur._DefaultAttendees.Select(x => x.User).ToList();
            people.Add(GetUser());
            people = people.Distinct(x => x.Id).ToList();

            var possible = people.Select(x => new IssueVM.AccountableUserVM() {
                id = x.Id,
                imageUrl = x.ImageUrl(true, ImageSize._32),
                name = x.GetName()
            }).ToList();


            var model = new RockIssueVM() {
                ByUserId = GetUser().Id,
                Message = await s.NotNull(async x => await x.GetIssueMessage()),
                Details = await s.NotNull(async x => await x.GetIssueDetails()),
                MeetingId = meeting,
                RockId = rock,
                RecurrenceId = s.ForRecurrence.Id,
                PossibleUsers = possible,
                OwnerId = s.ForRock.ForUserId
            };
            return PartialView("RockIssueModal", model);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> CreateRockIssue(RockIssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.RockId, x => x.RecurrenceId);
            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message,
                Description = model.Details,
                ForModel = "RockModel",
                ForModelId = model.RockId,
                Organization = GetUser().Organization,
                _Priority = model.Priority
            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
            //return PartialView("ScorecardIssueModal", model);
        }


        [Access(AccessLevel.UserOrganization)]

        public async Task<PartialViewResult> CreateHeadlineIssue(long meeting, long headline, long? recurrence = null) {

            //Sometimes this method is called with -1 for meetingId,
            if (meeting == -1 && recurrence != null) {
                meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrence.Value, true).NotNull(x => x.Id);
            }

            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(meeting));

            var s = HeadlineAccessor.GetHeadline(GetUser(), headline);
            var recur = L10Accessor.GetCurrentL10RecurrenceFromMeeting(GetUser(), meeting);

            var people = recur._DefaultAttendees.Select(x => x.User).ToList();
            people.Add(GetUser());
            people = people.Distinct(x => x.Id).ToList();

            var possible = people.Select(x => new IssueVM.AccountableUserVM() {
                id = x.Id,
                imageUrl = x.ImageUrl(true, ImageSize._32),
                name = x.GetName()
            }).ToList();


            var model = new HeadlineIssueVM() {
                ByUserId = GetUser().Id,
                Message = await s.NotNull(async x => await x.GetIssueMessage()),
                Details = await s.NotNull(async x => await x.GetIssueDetails()),
                MeetingId = meeting,
                HeadlineId = headline,
                RecurrenceId = s.RecurrenceId,
                PossibleUsers = possible,
                OwnerId = s.OwnerId
            };
            return PartialView("HeadlineIssueModal", model);
        }
        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public async Task<JsonResult> CreateHeadlineIssue(HeadlineIssueVM model) {
            ValidateValues(model, x => x.ByUserId, x => x.MeetingId, x => x.HeadlineId, x => x.RecurrenceId);
            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Meeting(model.MeetingId));

            await IssuesAccessor.CreateIssue(GetUser(), model.RecurrenceId, model.OwnerId, new IssueModel() {
                CreatedById = GetUser().Id,
                //MeetingRecurrenceId = model.RecurrenceId,
                CreatedDuringMeetingId = model.MeetingId,
                Message = model.Message,
                Description = model.Details,
                ForModel = "PeopleHeadline",
                ForModelId = model.HeadlineId,
                Organization = GetUser().Organization,
                _Priority = model.Priority
            });
            return Json(ResultObject.SilentSuccess().NoRefresh());
            //return PartialView("ScorecardIssueModal", model);
        }


        [Access(AccessLevel.UserOrganization)]
        public FileContentResult Listing() {
            var csv = IssuesAccessor.Listing(GetUser(), GetUser().Organization.Id);
            return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
        }
    }
}