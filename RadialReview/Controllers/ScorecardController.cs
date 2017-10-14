using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Accessors;
using RadialReview.Models.Scorecard;
using RadialReview.Models.Angular.Scorecard;

namespace RadialReview.Controllers
{
    public class ScorecardController : BaseController{

		[Access(AccessLevel.UserOrganization)]
		public FileContentResult Listing() {
			var csv = _ScorecardAccessor.Listing(GetUser(), GetUser().Organization.Id);
			return File(csv.ToBytes(), "text/csv", "" + DateTime.UtcNow.ToJavascriptMilliseconds() + "_" + csv.Title + ".csv");
		}

		#region Deleted

		//  public class ScoreVM
		//  {
		//public DateTime Start { get; set; }
		//public DateTime End { get; set; }
		//   public List<List<ScoreModel>> ScoreModels { get; set; }

		//   public ScoreVM()
		//   {
		//	ScoreModels = new List<List<ScoreModel>>();
		//   }

		//  }
		// GET: Scorecard
		/* [Access(AccessLevel.UserOrganization)]
		 public ActionResult List(long? start = null, long? end = null)
		 {
			 var model = new ScoreVM();
			 return View();
		 }*/

		// GET: Scorecard
		//[Access(AccessLevel.UserOrganization)]
		//public ActionResult Edit(long? start = null, long? end = null)
		//{
		//	DateTime sd, ed;
		//	StartEnd(out sd, out ed, start, end);
		//	var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, sd, ed);
		//	var incomplete = ScorecardAccessor.GetUserScoresIncomplete(GetUser(), GetUser().Id);
		//	var combine = new List<ScoreModel>();
		//	combine.AddRange(scores);

		//	foreach (var s in incomplete){
		//		if (combine.Any(x => x.Id != s.Id)){
		//			combine.Add(s);
		//		}
		//	}
		//	var groupByDueDate = combine.GroupBy(x => x.DateDue).Select(x=>x.ToList()).ToList();

		//	return View(new ScoreVM
		//	{
		//		Start = sd,
		//		End = ed,
		//		ScoreModels = groupByDueDate,
		//	});
		//}

		//// GET: Scorecard
		//[Access(AccessLevel.UserOrganization)]
		//[HttpPost]
		//public ActionResult Edit(ScoreVM model)
		//{
		//	ScorecardAccessor.EditUserScores(GetUser(), model.ScoreModels.SelectMany(x=>x).ToList());
		//	return RedirectToAction("Index","Home");
		//}

		//   // GET: Scorecard
		//[Access(AccessLevel.UserOrganization)]
		//public ActionResult Index(long? start = null, long? end = null)
		//{
		//	DateTime sd, ed;
		//	StartEnd(out sd, out ed, start, end);

		//	//var start = start?? ;
		//	//var end = DateTime.UtcNow.EndOfWeek(DayOfWeek.Monday);

		//	//var scores = ScorecardAccessor.GetScores(GetUser(), GetUser().Organization.Id, sd, ed,true);

		//	var scores = ScorecardAccessor.GetUserScores(GetUser(), GetUser().Id, sd, ed);
		//	var incomplete = ScorecardAccessor.GetUserScoresIncomplete(GetUser(), GetUser().Id);
		//	var combine = new List<ScoreModel>();
		//	combine.AddRange(scores);

		//	foreach (var s in incomplete)
		//	{
		//		if (combine.Any(x => x.Id != s.Id))
		//		{
		//			combine.Add(s);
		//		}
		//	}
		//	var groupByDueDate = combine.GroupBy(x => x.DateDue).Select(x => x.ToList()).ToList();

		//	return View(new ScoreVM() { End = ed, Start = sd, ScoreModels = groupByDueDate });
		////      }


		//private void StartEnd(out DateTime sd,out DateTime ed, long? start, long? end)
		//{
		//	if (start == null && end == null)
		//	{
		//		sd = DateTime.UtcNow.StartOfWeek(DayOfWeek.Sunday);
		//		ed = sd.EndOfWeek(DayOfWeek.Sunday);
		//	}
		//	else if (end != null && start != null)
		//	{
		//		ed = end.Value.ToDateTime();
		//		sd = start.Value.ToDateTime();
		//	}
		//	else if (end != null)
		//	{
		//		ed = end.Value.ToDateTime(); //.StartOfWeek(DayOfWeek.Monday);
		//		sd = ed.StartOfWeek(DayOfWeek.Monday);
		//	}
		//	else
		//	{
		//		sd = start.Value.ToDateTime(); //.StartOfWeek(DayOfWeek.Monday);
		//		ed = sd.EndOfWeek(DayOfWeek.Monday);
		//	}
		//} 
		#endregion
	}
}