﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MathNet.Numerics.Distributions;
using RadialReview.Exceptions.MeetingExceptions;
using RadialReview.Models;
using RadialReview.Models.Audit;
using RadialReview.Models.L10;
using RadialReview.Accessors;
using RadialReview.Models.L10.VM;
using RadialReview.Models.Permissions;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Json;
using RadialReview.Models.Timeline;
using RadialReview.Models.VideoConference;
using RadialReview.Utilities;
using RadialReview.Exceptions;
using RadialReview.Models.UserModels;

namespace RadialReview.Controllers
{
    public partial class L10Controller : BaseController
    {


        // GET: L10
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Index()
        {
            var recurrences = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, true);
            var model = new L10ListingVM()
            {
                Recurrences = recurrences,
            };
            return View(model);
        }

        // GET: L10

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Meeting(long? id = null)
        {
            if (id == null)
                return Content("Error: url requires a meeting Id");
            var recurrenceId = id.Value;
            var recurrence = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);

            ViewBag.VideoChatRoom = new VideoConferenceVM()
            {
                RoomId = recurrence.VideoId
            };

            var model = new L10MeetingVM()
            {
                Recurrence = recurrence,
                Meeting = L10Accessor.GetCurrentL10Meeting(GetUser(), recurrenceId, true, loadLogs: true),
                EnableTranscript = recurrence.EnableTranscription,
            };
            

            if (model.Meeting != null)
            {

                model.MemberPictures = recurrence._DefaultAttendees.Select(x => new ProfilePictureVM { UserId = x.User.Id, Url = x.User.ImageUrl(true, ImageSize._32), Name = x.User.GetName(), Initials = x.User.GetInitials() }).ToList();

                var transcript = TranscriptAccessor.GetMeetingTranscript(GetUser(), model.Meeting.Id);
                model.CurrentTranscript = transcript.Select(x => new MeetingTranscriptVM()
                {
                    Id = x.Id,
                    Message = x.Text,
                    Order = x.CreateTime.ToJavascriptMilliseconds(),
                    Owner = x._User.GetName()
                }).ToList();
            }

            if (model != null && model.Recurrence != null)
            {
                model.CanAdmin = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
                model.CanEdit = _PermissionsAccessor.IsPermitted(GetUser(), x => x.CanEdit(PermItem.ResourceType.L10Recurrence, model.Recurrence.Id));
                model.VtoId = model.Recurrence.VtoId;
            }

            return View(model);
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Create()
        {
            var m = new L10Recurrence();

            //var allMeasurables = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, true);
            //var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
            //var allRocks = RockAccessor.GetAllVisibleRocksAtOrganization(GetUser(), GetUser().Organization.Id, true);

            var model = new L10EditVM();


            AddExtras(0, model);


            ViewBag.InfoAlert = "You can use the same L10 meeting each week. No need to create a new on each week.";

            return View("Edit", model);
        }

        [Access(AccessLevel.UserOrganization)]
        public string Delete()
        {
            return "Are you sure you want to delete this meeting?";
        }

        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public JsonResult Delete(long id)
        {
            L10Accessor.DeleteL10(GetUser(), id);
            return Json(ResultObject.SilentSuccess(), JsonRequestBehavior.AllowGet);
        }

        private L10EditVM AddExtras(long recurrenceId, L10EditVM model)
        {
            var allMeasurables = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, true);
            var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
            var allRocks = RockAccessor.GetAllVisibleRocksAtOrganization(GetUser(), GetUser().Organization.Id, true);
            model.PossibleMeasurables = allMeasurables.Where(x => x != null).ToList();
            model.PossibleMembers = allMembers;
            model.PossibleRocks = allRocks;

            if (recurrenceId != 0)
            {
                var r = L10Accessor.GetL10Recurrence(GetUser(), recurrenceId, true);
                allRocks.AddRange(r._DefaultRocks.Where(x => x.Id > 0 && allRocks.All(y => y.Id != x.ForRock.Id)).Select(x => x.ForRock));
                allMeasurables.AddRange(r._DefaultMeasurables.Where(x => x.Id > 0 && allMeasurables.All(y => y != null && y.Id != x.Measurable.NotNull(z => z.Id))).Select(x => x.Measurable));
                model.Recurrence = r;
                model.SelectedMeasurables = model.SelectedMeasurables ??r._DefaultMeasurables.Where(x => x.Measurable != null).Select(x => x.Measurable.Id).ToArray();
                model.SelectedMembers = model.SelectedMembers??r._DefaultAttendees.Select(x => x.User.Id).ToArray();
                model.SelectedRocks = model.SelectedRocks??r._DefaultRocks.Select(x => x.ForRock.Id).ToArray();
            }
            else
            {
                model.Recurrence = model.Recurrence??new L10Recurrence()
                {
                    CreateTime = DateTime.UtcNow,
                    OrganizationId = GetUser().Organization.Id,
                    VideoId = Guid.NewGuid().ToString(),
                    EnableTranscription = false,
                    HeadlinesId = Guid.NewGuid().ToString(),
                    CountDown = true,
                };
                
                model.SelectedMeasurables = model.SelectedMeasurables??new long[0];
                model.SelectedMembers = model.SelectedMembers??new long[0];
                model.SelectedRocks = model.SelectedRocks??new long[0];
            }

            return model;
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Edit(long? id = null, string @return = null)
        {
            if (id == null)
                return RedirectToAction("Create");

            var recurrenceId = id.Value;

            _PermissionsAccessor.Permitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, recurrenceId));

            var model=AddExtras(recurrenceId, new L10EditVM(){Return = @return});
            return View("Edit", model);
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult Wizard(long? id = null, string @return = null)
        {
            if (id == null) {
                //var m = new L10Recurrence();
                //var model = new L10EditVM();
                //AddExtras(0, model);
                //ViewBag.InfoAlert = "You can use the same L10 meeting each week. No need to create a new on each week.";

                var l10 = L10Accessor.CreateBlankRecurrence(GetUser(),GetUser().Organization.Id);
                return RedirectToAction("Wizard", new { id = l10.Id, tname = Request["tname"], tmethod = Request["tmethod"] });
            } else {
                //var recurrenceId = id.Value;
                _PermissionsAccessor.Permitted(GetUser(), x => x.CanAdmin(PermItem.ResourceType.L10Recurrence, id.Value));
                //var model = AddExtras(recurrenceId, new L10EditVM() { Return = @return });
                return View("Wizard", id.Value);
            }
        }


        [HttpPost]
        [Access(AccessLevel.UserOrganization)]
        public ActionResult Edit(L10EditVM model)
        {
            ValidateValues(model,
                x => x.Recurrence.Id,
                x => x.Recurrence.CreateTime,
                x => x.Recurrence.OrganizationId,
                x => x.Recurrence.MeetingInProgress,
                x => x.Recurrence.CreatedById,
                x => x.Recurrence.VideoId,
                x => x.Recurrence.HeadlinesId,
                x => x.Recurrence.OrderIssueBy,
                x => x.Recurrence.VtoId);

            if (model.Recurrence == null)
                throw new PermissionsException("Recurrence was null");

            if (String.IsNullOrWhiteSpace(model.Recurrence.Name))
            {
                ModelState.AddModelError("Name", "Meeting name is required");
               // AddExtras(model.Recurrence.Id, model);
              //  return View("Edit", model);
            }
            if (model.SelectedMembers == null || model.SelectedMembers.Length == 0)
                ModelState.AddModelError("PossibleMembers", "At least one attendee is required");

            var allRocks = RockAccessor.GetAllVisibleRocksAtOrganization(GetUser(), GetUser().Organization.Id, true);
            var allMeasurables = ScorecardAccessor.GetVisibleMeasurables(GetUser(), GetUser().Organization.Id, true);
            var allMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);
            model.SelectedRocks = model.SelectedRocks ?? new long[0];
            model.SelectedMembers = model.SelectedMembers ?? new long[0];
            model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];

            if (model.Recurrence.Id != 0)
            {
                var existing = L10Accessor.GetL10Recurrence(GetUser(), model.Recurrence.Id, true);
                allRocks.AddRange(existing._DefaultRocks.Where(x => x.Id > 0 && allRocks.All(y => y.Id != x.ForRock.Id)).Select(x => x.ForRock));
                allMeasurables.AddRange(existing._DefaultMeasurables.Where(x => x.Id > 0 && allMeasurables.All(y => y != null && y.Id != x.Measurable.NotNull(z => z.Id))).Select(x => x.Measurable));
            }
            else
            {
                _PermissionsAccessor.Permitted(GetUser(), x => x.CreateL10Recurrence(model.Recurrence.OrganizationId));
                ViewBag.InfoAlert = "You only need to create one L10 meeting per weekly meeting. In other words, you don't need to create a new L10 each week.";
            }
            if (ModelState.IsValid)
            {
                model.Recurrence.OrganizationId = GetUser().Organization.Id;
                model.Recurrence._DefaultAttendees = allMembers.Where(x => model.SelectedMembers.Any(y => y == x.Id))
                .Select(x => new L10Recurrence.L10Recurrence_Attendee()
                {
                    L10Recurrence = model.Recurrence,
                    User = x
                }).ToList();
                //model.SelectedMeasurables=model.SelectedMeasurables??new long[0]{};
                model.Recurrence._DefaultMeasurables = allMeasurables.Where(x => model.SelectedMeasurables.Any(y => x != null && y == x.Id))
                .Select(x => new L10Recurrence.L10Recurrence_Measurable()
                {
                    L10Recurrence = model.Recurrence,
                    Measurable = x
                }).ToList();

                //model.SelectedRocks = model.SelectedRocks ?? new long[0] { };
                model.Recurrence._DefaultRocks = allRocks.Where(x => model.SelectedRocks.Any(y => y == x.Id))
                    .Select(x => new L10Recurrence.L10Recurrence_Rocks()
                    {
                        L10Recurrence = model.Recurrence,
                        ForRock = x
                    }).ToList();



                L10Accessor.EditL10Recurrence(GetUser(), model.Recurrence);


                if (model.Return == "meeting")
                    return RedirectToAction("meeting", new { id = model.Recurrence.Id });

                return RedirectToAction("Index");
            }

            model.PossibleRocks = allRocks;
            model.PossibleMembers = allMembers;
            model.PossibleMeasurables = allMeasurables.Where(x => x != null).ToList();


            //model.SelectedRocks = model.SelectedRocks ?? new long[0];
            //model.SelectedMembers = model.SelectedMembers ?? new long[0];
            //model.SelectedMeasurables = model.SelectedMeasurables ?? new long[0];

            return View("Edit", model);
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult External(long id)
        {
            var recurrence = id;
            var links = L10Accessor.GetExternalLinksForRecurrence(GetUser(), id);
            ViewBag.Recurrence = recurrence;
            return View(links);
        }




        [Access(AccessLevel.UserOrganization)]
        public ActionResult Timeline(long id)
        {
            var recurrence = id;
            var audits = L10Accessor.GetL10Audit(GetUser(), recurrence);
            var transcripts = TranscriptAccessor.GetRecurrenceTranscript(GetUser(), recurrence);
            var meetings = L10Accessor.GetL10Meetings(GetUser(), id, false);
            var list = new List<MeetingTimeline>();
            var user = GetUser();
            foreach (var m in meetings)
            {

                var curAudits = audits.Where(x => m.CreateTime <= x.CreateTime && (m.CompleteTime == null || x.CreateTime <= m.CompleteTime)).ToList();
                var curTranscripts = transcripts.Where(x => m.CreateTime <= x.CreateTime && (m.CompleteTime == null || x.CreateTime <= m.CompleteTime)).ToList();

                var allItems = new List<TimelineItem>();
                allItems.AddRange(curAudits.Select(x => TimelineItem.Create(user, x)));
                allItems.AddRange(curTranscripts.Select(x => TimelineItem.Create(user, x)));

                list.Add(new MeetingTimeline()
                {
                    Meeting = m,
                    Items = allItems
                });
            }
            return View(list);
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Printout(long id)
        {
            var recur = L10Accessor.GetAngularRecurrence(GetUser(), id);
            var d = L10Accessor.GetLastMeetingEndTime(GetUser(), id);

            var doc = PdfAccessor.CreateDoc(GetUser(), "THE LEVEL 10 MEETING");

            PdfAccessor.AddL10(doc, recur, d);

            var now = DateTime.UtcNow.ToJavascriptMilliseconds() + "";
            return Pdf(doc, now + "_" + recur.Basics.Name + "_L10Meeting.pdf", true);
        }

        #region Error
        [Access(AccessLevel.Any)]
        public PartialViewResult Error(MeetingException e)
        {
            return PartialView("Error", e);
        }
        [Access(AccessLevel.Any)]
        public PartialViewResult ErrorMessage(String message = null, MeetingExceptionType? type = null)
        {
            return PartialView("Error", new MeetingException(message ?? "An error has occurred.", type ?? MeetingExceptionType.Error));
        }
        #endregion
    }
}