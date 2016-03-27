using System.Threading.Tasks;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CsvHelper;
using RadialReview.Utilities;
using RadialReview.Models.Components;
using RadialReview.Models.L10;
using System.Net;
using System.Globalization;
using RadialReview.Utilities.DataTypes;
using RadialReview.Models;
using RadialReview.Models.Scorecard;
using Novacode;

namespace RadialReview.Controllers {
    public partial class UploadController : BaseController {
        //
        // GET: /Upload/
        [Access(AccessLevel.User)]
        public ActionResult Index()
        {
            return View();
        }
        #region Images
        [HttpPost]
        [Access(AccessLevel.User)]
        public async Task<JsonResult> Image(string id, HttpPostedFileBase file, String forType)
        {
            var userModel = GetUserModel();
            if (userModel == null)
                throw new PermissionsException();

            if (userModel.Id != id && !userModel.IsRadialAdmin)
                throw new PermissionsException("Id is not correct");

            if (userModel.IsRadialAdmin) {
                userModel = GetUser().User;
            }


            //you can put your existing save code here
            if (file != null && file.ContentLength > 0) {
                // extract only the fielname
                var uploadType = forType.Parse<UploadType>();
                var url = await _ImageAccessor.UploadImage(userModel, Server, file, uploadType);
                return Json(ResultObject.Create(url));
            }
            return Json(new ResultObject(true, ExceptionStrings.SomethingWentWrong));
        }

        [Access(AccessLevel.UserOrganization)]

        public ActionResult ProfilePicture()
        {
            return View();
        }
        #endregion

        private static Dictionary<string, string> Backup = new Dictionary<string, string>();
        [Access(AccessLevel.UserOrganization)]
        public ActionResult L10(string id = null, long recurrence = 0)
        {
            if (string.IsNullOrWhiteSpace(id)) {
                var recurs = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, false);
                return View(recurs);
            }

            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Recurrence(recurrence));
            ViewBag.RecurrenceId = recurrence;
            var title = id.ToTitleCase();
            ViewBag.Title = "Upload " + title;
            ViewBag.UploadScript = "Upload" + title + ".js";

            return View("UploadL10");
        }


        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<JsonResult> UploadRecurrenceFile(long recurrenceId, HttpPostedFileBase file, UploadType type, bool csv = false)
        {
            _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrenceId));
            // _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Recurrence(model.RecurrenceId));
            try {
                var upload = await UploadAccessor.UploadAndParse(GetUser(), type, file, ForModel.Create<L10Recurrence>(recurrenceId));
                if (csv && upload.GetLikelyFileType() != FileType.CSV)
                    throw new FileNotFoundException("File must be a csv.");
                var table = HtmlUtility.Table(upload.Csv, new TableOptions<string>() {
                    CellClass = (x => "tdItem"),
                    TableClass = "table table-bordered table-condensed noselect",
                    Responsive = true
                });
                var data = new Dictionary<string, string> { { "Path", upload.Path }, { "UseAWS", upload.UseAWS + "" }, { "FileType", upload.GetLikelyFileType() + "" } };
                return Json(ResultObject.CreateHtml(table, data));

            } catch (FileNotFoundException e) {
                return Json(ResultObject.CreateError("An error has occurred. " + e.Message));
            }
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpGet]
        public ActionResult UploadVTO(long recurrenceId)
        {
            ViewBag.RecurrenceId = recurrenceId;
            return View();
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<ActionResult> UploadVTO(long recurrenceId, HttpPostedFileBase file)
        {
            using (var ms = new MemoryStream()) {
                await file.InputStream.CopyToAsync(ms);
                file.InputStream.Seek(0, SeekOrigin.Begin);
                _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrenceId));
                var upload = await UploadAccessor.UploadFile(GetUser(), UploadType.VTO, file, ForModel.Create<L10Recurrence>(recurrenceId));
                var doc = DocX.Load(ms);
                var sections = doc.GetSections();


                var vto = VtoAccessor.UploadVtoForRecurrence(GetUser(), doc, recurrenceId);


                var a = 0;
               

                return RedirectToAction("Edit","VTO",new{id=vto.Id});
            }




        }


        #region Comments
        //[Access(AccessLevel.UserOrganization)]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<ActionResult> UploadScorecard(UploadScorecardVM model)
        //{
        //    var file = model.File;

        //    _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(model.RecurrenceId));
        //    // _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Recurrence(model.RecurrenceId));

        //    if (file != null && file.ContentLength > 0) {
        //        using (var ms = new MemoryStream()) {
        //            await file.InputStream.CopyToAsync(ms);

        //            var csvData = CsvUtility.Load(ms);
        //            var useAws = true;
        //            string path = null;
        //            try {
        //                var upload = await UploadAccessor.UploadFile(GetUser(), UploadType.Scorecard, file, ForModel.Create<L10Recurrence>(model.RecurrenceId));
        //                path = upload.GetPath();
        //            } catch (Exception e) {
        //                useAws = false;
        //                ms.Seek(0, SeekOrigin.Begin);
        //                var read = ms.ReadToEnd();
        //                path = Guid.NewGuid().ToString();
        //                Backup[path] = read;
        //            }
        //            var m = new SelectScorecardVM() {
        //                Rows = csvData,
        //                Path = path,
        //                RecurrenceId = model.RecurrenceId,
        //                UseAWS = useAws,
        //            };
        //            return PartialView("SelectScorecard", m);
        //        }
        //    }
        //    ViewBag.Message = "An error has occurred.";

        //    return Content("UploadScorecard");
        //}
        //[Access(AccessLevel.UserOrganization)]
        //[HttpPost]
        //public ActionResult UploadScorecardSelected(UploadScorecardSelectedVM model)
        //{
        //    Stream ms;
        //    if (model.UseAWS) {
        //        ms = new MemoryStream(new WebClient().DownloadData("https://s3.amazonaws.com/" + model.Path));
        //    } else {
        //        ms = Backup[model.Path].ToStream();
        //    }
        //    var csvData = CsvUtility.Load(ms);

        //    var userRect = new Rect(model.userRect);
        //    var measurableRect = new Rect(model.measurableRect);
        //    var goalsRect = new Rect(model.goalRect);

        //    userRect.EnsureRowOrColumn();
        //    userRect.EnsureSameRangeAs(measurableRect);
        //    userRect.EnsureSameRangeAs(goalsRect);

        //    var dateRect = new Rect(model.dateRect);

        //    if (userRect.GetType() != dateRect.GetType())
        //        throw new ArgumentOutOfRangeException("rect", "Date selection and owner selection must be of different selection types (either row or column)");

        //    var userStrings = userRect.GetArray1D(csvData);
        //    var measurableStrings = measurableRect.GetArray1D(csvData);
        //    var goals = goalsRect.GetArray1D(csvData, x => x.TryParseDecimal() ?? 0m);

        //    var dateStrings = dateRect.GetArray1D(csvData);
        //    var dates = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));


        //    var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
        //    var userLookups = DistanceUtility.TryMatch(userStrings, allUsers);

        //    Rect scoreRect = null;
        //    if (dateRect.GetRectType() == RectType.Row) {
        //        scoreRect = new Rect(dateRect.MinX, userRect.MinY, dateRect.MaxX, userRect.MaxY);
        //    } else {
        //        scoreRect = new Rect(userRect.MinX, dateRect.MinY, userRect.MaxX, dateRect.MaxY);
        //    }

        //    var scores = scoreRect.GetArray(csvData, x => x.TryParseDecimal());

        //    var m = new UploadScorecardSelectedDataVM() {
        //        Rows = csvData,
        //        Users = userStrings,
        //        UserLookup = userLookups,
        //        Measurables = measurableStrings,
        //        Dates = dates,
        //        Goals = goals,
        //        Scores = scores,
        //        RecurrenceId = model.RecurrenceId,
        //        Path = model.Path,
        //        UseAWS = model.UseAWS,
        //        ScoreRange = string.Join(",",scoreRect.ToString()),
        //        MeasurableRectType = ""+dateRect.GetRectType(),
        //        DateRange = string.Join(",", dateRect.ToString()),
        //    };
        //    return PartialView(m);
        //}
        //[Access(AccessLevel.UserOrganization)]
        //[HttpPost]
        //public ActionResult SubmitScorecard(FormCollection model)
        //{
        //    var useAws = model["UseAWS"].ToBoolean();
        //    var path = model["Path"].ToString();
        //    var recurrence = model["RecurrenceId"].ToLong();
        //    var measurableRectType = model["MeasurableRectType"].ToString();
        //    var scoreRect = new Rect(model["ScoreRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());
        //    var dateRect = new Rect(model["DateRange"].ToString().Split(',').Select(x => x.ToInt()).ToList());

        //    _PermissionsAccessor.Permitted(GetUser(), x => x.AdminL10Recurrence(recurrence));
        //    Stream ms;
        //    if (useAws) {
        //        ms = new MemoryStream(new WebClient().DownloadData("https://s3.amazonaws.com/" + path));
        //    } else {
        //        ms = Backup[path].ToStream();
        //    }
        //    var csvData = CsvUtility.Load(ms);

        //    var keys = model.Keys.OfType<string>();
        //    var measurables = keys.Where(x => x.StartsWith("m_measurable_"))
        //        .ToDictionary(x => x.SubstringAfter("m_measurable_").ToInt(), x => (string)model[x]);
        //    var goals = keys.Where(x => x.StartsWith("m_goal_"))
        //        .ToDictionary(x => x.SubstringAfter("m_goal_").ToInt(), x => model[x].TryParseDecimal(0));
        //    var users = keys.Where(x => x.StartsWith("m_user_"))
        //        .ToDictionary(x => x.SubstringAfter("m_user_").ToInt(), x => model[x].ToLong());
        //    var goalDirs = keys.Where(x => x.StartsWith("m_goaldir_"))
        //        .ToDictionary(x => x.SubstringAfter("m_goaldir_").ToInt(), x => (LessGreater)Enum.Parse(typeof(LessGreater), model[x]));

        //    var scores = scoreRect.GetArray(csvData, (x,c) => x.TryParseDecimal());


        //    var dateStrings = dateRect.GetArray1D(csvData);
        //    var dates = TimingUtility.FixOrderedDates(dateStrings, new CultureInfo("en-US"));

        //    var caller = GetUser();
        //    var now = DateTime.UtcNow;
        //    var measurableLookup = new Dictionary<int, MeasurableModel>();
        //    using (var s = HibernateSession.GetCurrentSession()) {
        //        using (var tx = s.BeginTransaction()) {
        //            var org = s.Get<L10Recurrence>(recurrence).Organization;
        //            var perms = PermissionsUtility.Create(s, caller).ViewOrganization(org.Id);
        //            foreach (var m in measurables) {
        //                var ident = m.Key;
        //                var owner = users[ident];
        //                var goal = goals[ident];
        //                var goaldir = goalDirs[ident];
        //                var measurable = new MeasurableModel() {
        //                    Title = m.Value,
        //                    OrganizationId = org.Id,
        //                    Goal = goal,
        //                    GoalDirection = goaldir,
        //                    AccountableUserId = owner,
        //                    AdminUserId = owner,
        //                    CreateTime = now
        //                };

        //                L10Accessor.AddMeasurable(s, perms, recurrence, L10Controller.AddMeasurableVm.CreateNewMeasurable(recurrence, measurable),skipRealTime:true);
        //                measurableLookup[ident]=measurable;
        //                var scoreRow = measurableRectType == "Row" 
        //                    ? new Rect(scoreRect.MinX, scoreRect.MinY + ident, scoreRect.MaxX, scoreRect.MinY + ident) 
        //                    : new Rect(scoreRect.MinX+ ident, scoreRect.MinY , scoreRect.MinX+ident, scoreRect.MaxY);

        //                var scoresFound = scoreRow.GetArray1D(csvData, x => x.TryParseDecimal());

        //                for (var i = 0; i < dates.Count; i++) {
        //                    var week = TimingUtility.GetWeekSinceEpoch(dates[i].AddDays(7).AddDays(6).StartOfWeek(DayOfWeek.Sunday));
        //                    var score = scoresFound[i];
        //                    L10Accessor._UpdateScore(s,perms, measurable.Id, week, score, null,noSyncException:true,skipRealTime:true);
        //                }



        //            } 
        //            var existing = s.QueryOver<L10Recurrence.L10Recurrence_Attendee>()
        //                .Where(x => x.DeleteTime == null && x.L10Recurrence.Id==recurrence)
        //                .Select(x => x.User.Id)
        //                .List<long>().ToList();

        //            foreach(var u in users.Where(x=>!existing.Any(y=>y==x.Value)).Select(x=>x.Value).Distinct()){
        //                    s.Save(new L10Recurrence.L10Recurrence_Attendee(){
        //                        User= s.Load<UserOrganizationModel>(u),
        //                        L10Recurrence = s.Load<L10Recurrence>(recurrence),
        //                        CreateTime = now,
        //                    });
        //                }
        //            tx.Commit();
        //            s.Flush();
        //        }
        //    }

        //    ShowAlert("Uploaded Scorecard", AlertType.Success);

        //    return RedirectToAction("Index", "Upload");
        //}
        #endregion

    }
}