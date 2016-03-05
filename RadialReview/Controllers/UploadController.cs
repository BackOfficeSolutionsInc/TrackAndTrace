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

namespace RadialReview.Controllers
{
    public class UploadController : BaseController
    {
        //
        // GET: /Upload/
        [Access(AccessLevel.User)]
        public ActionResult Index()
        {
            return View();
        }

        /* [HttpPost]
         [Access(AccessLevel.User)]
         public ActionResult Image(HttpPostedFileBase file, String forType)
         {
             var user=GetUserModel();
             if (user == null)
                 throw new PermissionsException();

             //you can put your existing save code here
             if (file != null && file.ContentLength > 0)
             { 
                 // extract only the fielname
                 UploadType uploadType=forType.Parse<UploadType>();
                 _ImageAccessor.UploadImage(user, Server, file, uploadType);
                 return Redirect(Request.UrlReferrer.ToString());
             }
             ViewBag.AlertMessage = ExceptionStrings.SomethingWentWrong;
             return Redirect(Request.UrlReferrer.ToString());            
         }*/

        [HttpPost]
        [Access(AccessLevel.User)]
        public async Task<JsonResult> Image(string id, HttpPostedFileBase file, String forType)
        {
            var userModel = GetUserModel();
            if (userModel == null)
                throw new PermissionsException();

            if (userModel.Id != id && !userModel.IsRadialAdmin)
                throw new PermissionsException("Id is not correct");

            if (userModel.IsRadialAdmin)
            {
                userModel = GetUser().User;
            }


            //you can put your existing save code here
            if (file != null && file.ContentLength > 0)
            {
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

        [Access(AccessLevel.UserOrganization)]
        public ActionResult L10(string id = null, long recurrence = 0)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                var recurs = L10Accessor.GetVisibleL10Meetings(GetUser(), GetUser().Id, false);
                return View(recurs);
            }
            switch (id.ToLower())
            {
                case "scorecard":
                    {
                        _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Recurrence(recurrence));
                        return View("UploadScorecard", new UploadScorecardVM()
                        {
                            RecurrenceId = recurrence
                        });
                    }
                case "rocks": return View("UploadRocks");
                case "todos": return View("UploadTodos");
                case "issues": return View("UploadIssues");
            }
            return View();
        }


        public class UploadScorecardVM
        {
            public long RecurrenceId { get; set; }
            public HttpPostedFileBase File { get; set; }
            public Guid Guid { get; set; }
        }

        //public static Dictionary<Guid, String> CSVs = new Dictionary<Guid, String>();

        public class SelectScorecardVM
        {
            public List<List<String>> Rows { get; set; }
            public string MeasurableRect { get; set; }
            public string OwnerRect { get; set; }
            public string GoalRect { get; set; }
            public string DateRect { get; set; }
            public long RecurrenceId { get; set; }
            public string Path { get; set; }
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UploadScorecard(UploadScorecardVM model)
        {
            var file = model.File;

            _PermissionsAccessor.Permitted(GetUser(), x => x.ViewL10Recurrence(model.RecurrenceId));

            if (file != null && file.ContentLength > 0)
            {
                using (var ms = new MemoryStream())
                {
                    await file.InputStream.CopyToAsync(ms);

                    var csvData = CsvUtility.Load(ms);
                    var upload = await UploadAccessor.UploadFile(GetUser(), UploadType.Scorecard, file, ForModel.Create<L10Recurrence>(model.RecurrenceId));
                    var m = new SelectScorecardVM()
                    {
                        Rows = csvData,
                        Path = upload.GetPath(),
                        RecurrenceId = model.RecurrenceId
                    };
                    return PartialView("SelectScorecard", m);
                }
            }
            ViewBag.Message = "An error has occurred.";

            return Content("UploadScorecard");
        }

        public class UploadScorecardSelectedVM
        {
            public List<int> userRect { get; set; }
            public List<int> dateRect { get; set; }
            public List<int> measurableRect { get; set; }
            public List<int> goalRect { get; set; }
            public long RecurrenceId { get; set; }
            public string Path { get; set; }
        }

        public class UploadScorecardSelectedDataVM
        {
            public List<string> Measurables { get; set; }
            public List<string> Users { get; set; }
            public List<decimal> Goals { get; set; }
            public List<DateTime> Dates { get; set; }
            
            public List<List<String>> Rows { get; set; }

            public List<Tuple<long, long>> Errors { get; set; }

            public Dictionary<string, DiscreteDistribution<Tuple<string,string,long>>> UserLookup { get; set; }

            public List<List<decimal?>> Scores { get; set; }
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public ActionResult UploadScorecardSelected(UploadScorecardSelectedVM model)
        {
            var ms = new MemoryStream(new WebClient().DownloadData("https://s3.amazonaws.com/"+model.Path));
            var csvData = CsvUtility.Load(ms);

            var userRect = new Rect(model.userRect);
            var measurableRect = new Rect(model.measurableRect);
            var goalsRect = new Rect(model.goalRect);

            userRect.EnsureRowOrColumn();
            userRect.EnsureSameRange(measurableRect);
            userRect.EnsureSameRange(goalsRect);

            var dateRect = new Rect(model.dateRect);

            if (userRect.GetType() != dateRect.GetType())
                throw new ArgumentOutOfRangeException("rect", "Date selection and owner selection must be of different selection types (either row or column)");

            var dateStrings = dateRect.GetArray1D(csvData);
            var userStrings = userRect.GetArray1D(csvData);
            var measurableStrings = measurableRect.GetArray1D(csvData);
            var goals = goalsRect.GetArray1D(csvData, x => x.TryParseDecimal() ?? 0m);

            var dates = TimingUtility.FixOrderedDates(dateStrings,new CultureInfo("en-US"));


            var allUsers = OrganizationAccessor.GetMembers_Tiny(GetUser(), GetUser().Organization.Id);
            var userLookups = DistanceUtility.TryMatch(userStrings,allUsers);

            Rect scoreRect=null;
            if (dateRect.GetRectType() == RectType.Row) {
                scoreRect = new Rect(dateRect.MinX, userRect.MinY, dateRect.MaxX, userRect.MaxY);
            } else {
                scoreRect = new Rect(userRect.MinX, dateRect.MinY, userRect.MaxX, dateRect.MaxY);
            }

            var scores = scoreRect.GetArray(csvData, x => x.TryParseDecimal());

            var m = new UploadScorecardSelectedDataVM()
            {
                Rows = csvData,
                Users = userStrings,
                UserLookup = userLookups,
                Measurables = measurableStrings,
                Dates = dates,
                Goals = goals,
                Scores = scores
            };

           



            return PartialView(m);
        }

        public class UploadMeasurableVM{
            public MeasurableModel Measurable { get;set;}
            public List<ScoreModel> Scores { get; set; }

            public List<SelectListItem> PossibleUsers { get; set; }

            public LessGreater GoalDirection { get; set; }
        }

        //private  EnsureRowOrColumn(List<int> rect)
        //{
        //    if (model.userRect[0] == model.userRect[2]  || model.userRect[1] == model.userRect[3]) {
        //        return;
        //    }
        //    throw new ArgumentOutOfRangeException("Must be a row or column.");
        //}

        //private static List<T> getRectData<T>(List<List<string>> csv, List<int> rect, Func<String, T> func)
        //{
        //    if (rect[0] == rect[2])
        //    {
        //        return Enumerable.Range(rect[1], rect[3] - rect[1] + 1).Select(x => func(csv[x][rect[0]])).ToList();
        //    }
        //    else if (rect[1] == rect[3])
        //    {
        //        return Enumerable.Range(rect[0], rect[2] - rect[0] + 1).Select(x => func(csv[rect[1]][x])).ToList();
        //    }
        //    else
        //    {
        //        throw new PermissionsException("Selection invalid");
        //    }
        //}
    }
}