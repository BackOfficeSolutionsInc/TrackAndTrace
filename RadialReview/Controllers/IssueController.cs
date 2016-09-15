using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Periods;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class IssueReviewModel
    {
        public String OrganizationName { get; set; }
        public List<SelectListItem> Teams { get; set; }
        public long SelectedTeam { get; set; }
    }

    public class CreateReviewModel
    {
        public long TeamId { get; set; }
        public String TeamName { get; set; }
        public bool EmailManagers { get; set; }
        public DateTime SelectedDate { get; set; }
        public String Name { get; set; }
        public bool ManagersCanCustomize { get; set; }
    }
    
    public class IssueController : BaseController
    {

        //
        // GET: /Issue/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            var model = new IssueReviewModel()
            {
                OrganizationName = GetUser().Organization.GetName(),
                Teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id).Select(x => new SelectListItem() { Text = x.Name, Value = "" + x.Id }).ToList(),
            };
            ViewBag.Page = "Generate";
            return View(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public async Task<ActionResult> Index(FormCollection form)
        {
            if (form["review"] == "issueReview")
            {
                var customized = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x =>
                {
                    var split = x.Split('_');
                    return Tuple.Create(long.Parse(split[1]), long.Parse(split[2]));
                });

                await _ReviewAccessor.CreateReviewFromCustom(
					System.Web.HttpContext.Current,
                    GetUser(),
                    form["TeamId"].ToLong(),
                    form["DueDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24),
                    form["ReviewName"],
                    form["SendEmails"].ToBooleanJS(),//.ToBoolean(),
					form["Anonymous"].ToBooleanJS(),
					customized.ToList()//,
                    //form["SessionId"].ToLong(),
                    //form["NextSessionId"].ToLong()

                    );
            }
            else if (form["review"] == "issuePrereview")
            {
                await _PrereviewAccessor.CreatePrereview(
                    GetUser(),
                    form["TeamId"].ToLong(),
                    form["ReviewName"],
                    true,//form["SendEmails"].ToLower().Contains("true"),
                    form["DueDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24),
                    form["PrereviewDate"].ToDateTime("MM-dd-yyyy", form["TimeZoneOffset"].ToDouble() + 24),
                    form["EnsureDefault"].ToBooleanJS(),
					form["Anonymous"].ToBooleanJS()//,
					//form["SessionId"].ToLong(),
					//form["NextSessionId"].ToLong()
                    );

            }
            else
            {
                throw new PermissionsException("Review type is not recognized");
            }

            return RedirectToAction("Index", "Home");
        }


        [Access(AccessLevel.Manager)]
        public PartialViewResult Customize(long id)
        {
            var teamId = id;

            var model = _ReviewEngine.GetCustomizeModel(GetUser(), teamId, false);

			//var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToList();
			//var plist = periods.ToSelectList(x => x.Name, x => x.Id);
			//plist.Add(new SelectListItem() { Text = "<Create New>", Value = "-3" });
            //
	        //model.Periods = plist;

			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false)
				.Cast<ResponsibilityGroupModel>().ToList();


            model.AllReviewees = allUsers;
			model.AllReviewees.Add( GetUser().Organization);

            return PartialView(model);
            
        }

		public class IssueOptions
		{
			public List<SelectListItem> Periods { get; set; } 
		}


        [Access(AccessLevel.Manager)]
        public PartialViewResult IssueOrganization()
        {
            var orgTeam = _TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id).FirstOrDefault(x => x.Type == TeamType.AllMembers);
            ViewBag.OrganizationId = orgTeam.Id;
            return PartialView();
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult IssueTeam()
        {
            var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id);
            var teamSelects = teams.Select(x => new SelectListItem() { Text = x.GetName(), Value = "" + x.Id }).ToList();
            teamSelects.Insert(0, new SelectListItem() { Selected = true, Text = "", Value = "" });
            return PartialView(teamSelects);
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult ManagersCustomize()
        {
	        var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).Where(x=>x.EndTime>DateTime.UtcNow).ToList();
	        var plist = periods.ToSelectList(x => x.Name, x => x.Id);
			plist.Add(new SelectListItem(){Text="<Create New>",Value = "-3"});
			var options = new IssueOptions(){
				Periods = plist,
	        };
            return PartialView(options);
        }

        [Access(AccessLevel.Manager)]
        public PartialViewResult SelfCustomize()
        {
            return PartialView();
        }


		/*
        [HttpPost]
        [Access(AccessLevel.Manager)]
        public ActionResult IssueOrganization(CreateReviewModel model)
        {
            throw new Exception("Implement me");

            //return View();
        }*/
	}
}