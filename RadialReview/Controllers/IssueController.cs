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
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class IssueReviewModel {
		public String OrganizationName { get; set; }
		public List<SelectListItem> Teams { get; set; }
		public long SelectedTeam { get; set; }
	}

	public class CreateReviewModel {
		public long TeamId { get; set; }
		public String TeamName { get; set; }
		public bool EmailManagers { get; set; }
		public DateTime SelectedDate { get; set; }
		public String Name { get; set; }
		public bool ManagersCanCustomize { get; set; }
	}

	public class IssueController : BaseController {

		private List<SelectListItem> _GenerateTeamList(UserOrganizationModel user) {
			return TeamAccessor.GetTeamsDirectlyManaged(user, user.Id)
						.Select(x => {
							var name = x.Name;
							if (x.Type == TeamType.AllMembers) {
								name = "All of " + name;
							}
							if (x.Type == TeamType.Managers) {
								name = "All " + name;
							}

							return new SelectListItem() {
								Text = name,
								Value = "" + x.Id
							};
						}).ToList();
		}


		//
		// GET: /Issue/
		[Access(AccessLevel.Manager)]
		public ActionResult Index() {
			var teams = _GenerateTeamList(GetUser());

			var model = new IssueReviewModel() {
				OrganizationName = GetUser().Organization.GetName(),
				Teams = teams
			};
			ViewBag.Page = "Generate";


			return View(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		[Obsolete("Fix for AC")]
		[AsyncTimeout(60000 * 30)]//20 minutes..
		public async Task<ActionResult> Index(CancellationToken ct, FormCollection form) {
			Server.ScriptTimeout = 60*30;
			if (form["review"] == "issueReview") {
				var customized = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x => {
					var split = x.Split('_');
					var acNodeId = split.Length > 3?(split[3]).TryParseLong():null;

					//make sure that acNodeId is not null
					var wrw = new WhoReviewsWho() {
						Reviewer = new Reviewer(long.Parse(split[1])),
						Reviewee = new Reviewee(long.Parse(split[2]), acNodeId)
					};
					return wrw;
				});

				await _ReviewAccessor.CreateReviewFromCustom(
					System.Web.HttpContext.Current,
					GetUser(),
					form["TeamId"].ToLong(),
					form["DueDate"].ToDateTime("MM-dd-yyyy HH:mm:ss").Date.AddHours(form["TimeZoneOffset"].ToDouble() + 24),
					form["ReviewName"],
					form["SendEmails"].ToBooleanJS(),//.ToBoolean(),
					form["Anonymous"].ToBooleanJS(),
					customized.ToList()//,
									   //form["SessionId"].ToLong(),
									   //form["NextSessionId"].ToLong()

					);
				return RedirectToAction("Index", "Reviews");
			} else if (form["review"] == "issuePrereview") {
				await _PrereviewAccessor.CreatePrereview(
					GetUser(),
					form["TeamId"].ToLong(),
					form["ReviewName"],
					true,//form["SendEmails"].ToLower().Contains("true"),
					form["DueDate"].ToDateTime("MM-dd-yyyy HH:mm:ss").AddHours(form["TimeZoneOffset"].ToDouble() + 24),
					form["PrereviewDate"].ToDateTime("MM-dd-yyyy HH:mm:ss").AddHours(form["TimeZoneOffset"].ToDouble() + 24),
					form["EnsureDefault"].ToBooleanJS(),
					form["Anonymous"].ToBooleanJS()//,
												   //form["SessionId"].ToLong(),
												   //form["NextSessionId"].ToLong()
					);

			} else {
				throw new PermissionsException("Review type is not recognized");
			}

			return RedirectToAction("Index", "Home");
		}


		[Access(AccessLevel.Manager)]
		public PartialViewResult Customize(long id) {
			var teamId = id;

			var model = _ReviewEngine.GetCustomizeModel(GetUser(), teamId, false);

			//var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).ToList();
			//var plist = periods.ToSelectList(x => x.Name, x => x.Id);
			//plist.Add(new SelectListItem() { Text = "<Create New>", Value = "-3" });
			//
			//model.Periods = plist;

			//TODO should the date range be null?
			//var allUsers
			//var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false)
			//	.Select(x=>new Reviewee(x)).ToList();
			//model.AllReviewees.Add(GetUser().Organization);

			//model.AllReviewees  = ReviewAccessor.GetPossibleOrganizationReviewees(GetUser(),GetUser().Organization.Id, null);//= allUsers;

			return PartialView(model);

		}

		public class IssueOptions {
			public List<SelectListItem> Periods { get; set; }
		}


		[Access(AccessLevel.Manager)]
		public PartialViewResult IssueOrganization() {
			var orgTeam = TeamAccessor.GetOrganizationTeams(GetUser(), GetUser().Organization.Id).FirstOrDefault(x => x.Type == TeamType.AllMembers);
			ViewBag.OrganizationId = orgTeam.Id;
			return PartialView();
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult IssueTeam() {
			var teams = _GenerateTeamList(GetUser());
			//var teamSelects = teams.Select(x => new SelectListItem() { Text = x.GetName(), Value = "" + x.Id }).ToList();
			teams.Insert(0, new SelectListItem() { Selected = true, Text = "", Value = "" });
			return PartialView(teams);
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult ManagersCustomize() {
			var periods = PeriodAccessor.GetPeriods(GetUser(), GetUser().Organization.Id).Where(x => x.EndTime > DateTime.UtcNow).ToList();
			var plist = periods.ToSelectList(x => x.Name, x => x.Id);
			plist.Add(new SelectListItem() { Text = "<Create New>", Value = "-3" });
			var options = new IssueOptions() {
				Periods = plist,
			};
			return PartialView(options);
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult SelfCustomize() {
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