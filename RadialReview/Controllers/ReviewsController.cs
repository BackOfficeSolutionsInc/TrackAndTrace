using Microsoft.AspNet.SignalR;
using NHibernate.Dialect.Function;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Hubs;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Utilities;
using RadialReview.Utilities.DataTypes;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
	public class ReviewsController : BaseController {
		public static OrgReviewsViewModel GenerateReviewVM(UserOrganizationModel caller, DateTime date, int page) {
			var selfId = caller.Id;
			var pullData = _ReviewAccessor.GetUsefulReviewFaster(caller, selfId, date);
			var usefulReviews = pullData.Item2;
			var reviewContainers = pullData.Item1;//usefulReviews.Select(x => x.ForReviewContainer).Distinct(x => x.Id).ToList();

			var subordinates = DeepAccessor.Users.GetSubordinatesAndSelf(caller, caller.Id);

			var editable = reviewContainers.Where(x => caller.IsManagingOrganization() || subordinates.Any(y => y == x.CreatedById)).Select(x => x.Id).ToList();
			var takabled = usefulReviews.Where(x => x.ForUserId == selfId).Distinct(x => x.ForReviewsId).ToDictionary(x => x.ForReviewsId, x => (long?)x.Id);


			var reviewsVM = reviewContainers.Select(x => new ReviewsViewModel(x) {
				Editable = editable.Any(y => y == x.Id),
				Viewable = true,
				TakableId = takabled.GetOrDefault(x.Id, null),
				UserReview = usefulReviews.FirstOrDefault(y => y.ForReviewsId == x.Id)

			}).Where(x => x.Editable || x.TakableId != null || x.UserReview != null).OrderByDescending(x => x.Review.DateCreated).ToList();

			var resultPerPage = 10;

			var model = new OrgReviewsViewModel() {
				Reviews = reviewsVM.Paginate(page, resultPerPage).ToList(),
				NumPages = reviewsVM.PageCount(resultPerPage),
				AllowEdit = true,
				Page = page,

			};

			foreach (var x in model.Reviews) {

				if (x.UserReview != null && x.UserReview.ClientReview.Visible)
					x.AddLink("/Review/Plot/" + x.UserReview.Id, "View Report", "glyphicon glyphicon-file");
				else if (x.TakableId == null) {
					x.AddLink("#", "Your report is not available yet", iconClass: "glyphicon glyphicon-file", linkClass: "gray noclick");
				}

				if (caller.IsManagingOrganization() || (x.Editable && model.AllowEdit)) {
					x.AddDivider();
					x.AddLink("/Reports/PeopleAnalyzer/" + x.Review.Id , "People Analyzer", "glyphicon glyphicon-list-alt");
				}

				if (caller.IsManager()) {
					x.AddDivider();
					x.AddLink("/Reports/Index/" + x.Review.Id + "#Reports", "Report Builder", "glyphicon glyphicon-file");
					x.AddLink("/Reports/Index/" + x.Review.Id + "#Reminders", "Send Reminders", "glyphicon glyphicon-send");
					x.AddLink("/Reports/Index/" + x.Review.Id + "#Stats", "Stats", "glyphicon glyphicon-stats");
				}
				if (x.Editable && model.AllowEdit) {
					x.AddDivider();
					x.AddLink("/Reviews/Edit/" + x.Review.Id, "Admin Settings", "glyphicon glyphicon-cog", linkClass: "advanced-link");
				}
			}

			return model;
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Index(int page = 0) {
			var model = GenerateReviewVM(GetUser(), DateTime.MinValue, page);



			ViewBag.Page = "Reviews";
			ViewBag.Title = "Reviews";
			ViewBag.Subheading = "Reviews";
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult Outstanding(int page = 0) {
			var model = GenerateReviewVM(GetUser(), DateTime.UtcNow, page);
			ViewBag.Page = "Outstanding";
			ViewBag.Title = "Outstanding";
			ViewBag.Subheading = "Reviews in progress.";
			return View(model);
		}

		[Access(AccessLevel.UserOrganization)]
		public ActionResult History(int page = 0) {
			var model = GenerateReviewVM(GetUser(), DateTime.MinValue, page);
			ViewBag.Page = "History";
			ViewBag.Title = "History";
			ViewBag.Subheading = "All reviews.";
			return View("Outstanding", model);
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Edit(long id) {
			var user = GetUser().Hydrate().ManagingUsers(true).Execute();
			var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, false, false, populateReview: true);
			if (reviewContainer.DeleteTime != null)
				throw new PermissionsException("This review has been deleted.");

			var allDirectSubs = user.ManagingUsers.Select(x => x.Subordinate).ToList();
			foreach (var r in reviewContainer.Reviews) {
				if (r.ForUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id) {
					r.ForUser.SetPersonallyManaging(true);
				} else {
					r.ForUser.PopulatePersonallyManaging(user, user.AllSubordinates);
					r.ForUser.PopulateDirectlyManaging(user, allDirectSubs);
				}
			}
			var model = new ReviewsViewModel(reviewContainer);
			return View(model);
		}

		public class UpdateReviewsViewModel {
			public long ReviewId { get; set; }
			public List<SelectListItem> AdditionalUsers { get; set; }
			public long SelectedUserId { get; set; }
			public bool SendEmail { get; set; }
		}

		public class DueDateVM {
			public long RReviewId { get; set; }
			public long[] reviews { get; set; }
			public DateTime DueDate { get; set; }
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult EditDueDateModal(long id) {
			var review = _ReviewAccessor.GetReview(GetUser(), id);

			var model = new DueDateVM() {
				RReviewId = review.Id,
				DueDate = review.DueDate,
			};
			return PartialView("EditDueDateModal", model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult EditDueDateModal(DueDateVM model) {
			var ids = new List<long>();
			var adjDate = GetUser().Organization.ConvertToUTC(model.DueDate);
			if (model.RReviewId != 0) {
				ids.Add(model.RReviewId);
				_ReviewAccessor.UpdateDueDate(GetUser(), model.RReviewId, adjDate);
			}
			if (model.reviews != null) {
				foreach (var m in model.reviews) {
					_ReviewAccessor.UpdateDueDate(GetUser(), m, adjDate);
					ids.Add(m);
				}
			}
			return Json(ResultObject.Create(new { due = model.DueDate.ToString(GetUser().Organization.Settings.GetDateFormat()), ids = ids }, "Updated Due Date"));
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult EditNameModal(long id) {
			var review = _ReviewAccessor.GetReviewContainer(GetUser(), id, false, false, false);

			return PartialView(review);
		}
		[Access(AccessLevel.Manager)]
		[HttpPost]
		public ActionResult EditNameModal(ReviewsModel model) {
			_ReviewAccessor.EditReviewName(GetUser(), long.Parse(Request.Form["IID"]), model.ReviewName);
			return Json(ResultObject.Success("Updated review name."));

		}


		public ActionResult Issue() {
			throw new PermissionsException("Deprecated");
			/*
            var today = DateTime.UtcNow.ToLocalTime();
            var user = GetUser().Hydrate().ManagingUsers(subordinates: true).Organization().Execute();
            var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), user.Id).ToSelectList(x => x.Name, x => x.Id).ToList();

            var model = new IssueReviewViewModel()
            {
                Today = today,
                ForUsers = user.AllSubordinates,
                PotentialTeams = teams
            };
            return View(model);*/
		}

		public class IssueDetailsViewModel {
			public Dictionary<long, List<long>> ReviewWho { get; set; }

			public Dictionary<long, UserOrganizationModel> AvailableUsers { get; set; }
		}


		[HttpPost]
		public ActionResult IssueDetailsSubmit(IssueReviewViewModel model) {
			return View();
			/*var teams = _TeamAccessor.GetTeamsDirectlyManaged(GetUser(), GetUser().Id).ToList();
            if (!teams.Any(x => x.Id == model.ForTeamId)){
                throw new PermissionsException("You do not have access to that team.");
            }
            using(var s = HibernateSession.GetCurrentSession())
            {
	            using(var tx=s.BeginTransaction())
	            {
                    var orgId=GetUser().Organization.Id;

                    var allOrgTeams = s.QueryOver<OrganizationTeamModel>().Where(x => x.Organization.Id == orgId).List();
                    var allTeamDurations = s.QueryOver<TeamDurationModel>().JoinQueryOver(x => x.Team).Where(x => x.Organization.Id == orgId).List();
                    var allMembers = s.QueryOver<UserOrganizationModel>().Where(x => x.Organization.Id == orgId).List();
                    var allManagerSubordinates = s.QueryOver<ManagerDuration>().JoinQueryOver(x => x.Manager).Where(x => x.Organization.Id == orgId).List();
                    var allPositions = s.QueryOver<PositionDurationModel>().JoinQueryOver(x => x.Position).Where(x => x.Organization.Id == orgId).List();
                    var applicationQuestions = s.QueryOver<QuestionModel>().Where(x => x.OriginId == ApplicationAccessor.APPLICATION_ID && x.OriginType == OriginType.Application).List();
                    var application = s.QueryOver<ApplicationWideModel>().Where(x => x.Id == ApplicationAccessor.APPLICATION_ID).List();

                    var reviewWhoSettings = s.QueryOver<ReviewWhoSettingsModel>().Where(x => x.OrganizationId == orgId).List();
                    
                    var queryProvider = new IEnumerableQuery(true);
                    queryProvider.AddData(allOrgTeams);
                    queryProvider.AddData(allTeamDurations);
                    queryProvider.AddData(allMembers);
                    queryProvider.AddData(allManagerSubordinates);
                    queryProvider.AddData(allPositions);
                    queryProvider.AddData(applicationQuestions);
                    queryProvider.AddData(application);

                    queryProvider.AddData(reviewWhoSettings);

                    var d=new DataInteraction(queryProvider,s.ToUpdateProvider());
                    
                    var perms=PermissionsUtility.Create(s,GetUser());
                    var teamMembers=TeamAccessor.GetTeamMembers(queryProvider,perms,GetUser(),model.ForTeamId).Select(x=>x.User);
                    
                    var reviewParams=new ReviewParameters(){
                        ReviewManagers=model.ReviewManagers,
                        ReviewSelf=model.ReviewSelf,
                        ReviewSubordinates=model.ReviewSubordinates,
                        ReviewPeers=model.ReviewPeers,
                        ReviewTeammates=model.ReviewTeammates,
                    };

                    var team = teams.First(x=>x.Id==model.ForTeamId);

                    var reviewWhoDictionary=new Dictionary<long,HashSet<long>>();

                    foreach(var member in teamMembers)
                    {
                        var reviewing=queryProvider.Get<UserOrganizationModel>(member.Id);
                        var usersTheyReview=ReviewAccessor.GetUsersThatReviewUser(GetUser(),perms,d,reviewing,reviewParams,team,teamMembers.ToList()).ToList();
                        reviewWhoDictionary[member.Id]=new HashSet<long>(usersTheyReview.Select(x=>x.Key.Id));
                        var reviewWho=queryProvider.Where<ReviewWhoSettingsModel>(x=>x.ByUserId==member.Id).ToList();
                        foreach(var r in reviewWho){
                            if(r.ForceState){
                                reviewWhoDictionary[member.Id].Add(r.ForUserId);
                            }else{
                                reviewWhoDictionary[member.Id].Remove(r.ForUserId);
                            }
                        }
                    }

	            }
            }


            
            

            var teamMembers = _TeamAccessor.GetTeamMembers(GetUser(), model.ForTeamId);



            var checks=new bool[][];*/

		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult Update(long id) {
			var review = _ReviewAccessor.GetReviewContainer(GetUser(), id, false, false);
			var users = _ReviewAccessor.GetUsersInReview(GetUser(), id);

			var orgMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), review.ForOrganization.Id, false, false);
			//Add to review
			var alsoExclude = _KeyValueAccessor.Get("AddToReviewReserved_" + id).Select(x => long.Parse(x.V));
			var additionalMembers = orgMembers.Where(x => !users.Any(y => y.Id == x.Id)).Where(x => !alsoExclude.Any(y => y == x.Id)).ToListAlive();

			var model = new UpdateReviewsViewModel() {
				ReviewId = id,
				AdditionalUsers = additionalMembers.ToSelectList(x => x.GetNameAndTitle(youId: GetUser().Id), x => x.Id),
				SendEmail = true
			};
			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public async Task<JsonResult> Update(UpdateReviewsViewModel model) {
			var reservedId = _KeyValueAccessor.Put("AddToReviewReserved_" + model.ReviewId, "" + model.SelectedUserId);
			var userName = GetUserModel().UserName;

			var user = GetUser();
			try {
				//TODO HERE
				var result = await Task.Run<ResultObject>(async () => {
					ResultObject output;
					try {
						output = await _ReviewAccessor.AddUserToReviewContainer(System.Web.HttpContext.Current, user, model.ReviewId, model.SelectedUserId, model.SendEmail);
					} catch (Exception e) {
						log.Error(e);
						output = new ResultObject(e);
					} finally {
						_KeyValueAccessor.Remove(reservedId);
					}
					return output;
				});

				new Thread(() => {
					Thread.Sleep(4000);
					var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
					hub.Clients.User(userName).jsonAlert(result, true);
					hub.Clients.User(userName).unhide("#ManageNotification");
				}).Start();

			} catch (Exception e) {
				log.Error(e);
				new Thread(() => {
					var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
					hub.Clients.User(userName).jsonAlert(new ResultObject(e));
					hub.Clients.User(userName).unhide("#ManageNotification");
				}).Start();
			}
			return Json(ResultObject.SilentSuccess());
		}

		public class RemoveUserVM {
			public long ReviewContainerId { get; set; }
			public long SelectedUser { get; set; }

			public List<SelectListItem> PossibleUsers { get; set; }
		}

		public class DeleteReview {
			public long ReviewContainerId { get; set; }
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public JsonResult Delete(DeleteReview model) {
			_ReviewAccessor.DeleteReviewContainer(GetUser(), model.ReviewContainerId);
			ViewBag.Success = "Removed review.";
			return Json(ResultObject.Success("Deleted Review"), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		[HttpGet]
		public PartialViewResult Delete(long id) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditReviewContainer(id));
			var model = new DeleteReview() { ReviewContainerId = id };
			return PartialView(model);
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult RemoveUser(long id) {
			var usersInReview = _ReviewAccessor.GetUsersInReview(GetUser(), id);
			var model = new RemoveUserVM() {
				ReviewContainerId = id,
				PossibleUsers = usersInReview.ToSelectList(x => x.GetNameAndTitle(youId: GetUser().Id), x => x.Id)
			};

			return PartialView(model);
		}
		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult RemoveUser(RemoveUserVM model) {
			var result = _ReviewAccessor.RemoveUserFromReview(GetUser(), model.ReviewContainerId, model.SelectedUser);
			return Json(result);
		}

		#region Individual Question
		public class RemoveQuestionVM {
			public long ReviewContainerId { get; set; }
			public List<SelectListItem> Users { get; set; }
			public long SelectedUserId { get; set; }
			public long SelectedQuestionId { get; set; }
		}
		public class AddQuestionVM {
			public long ReviewContainerId { get; set; }
			public List<SelectListItem> Users { get; set; }
			public long SelectedUserId { get; set; }
			public long SelectedQuestionId { get; set; }
		}

		[Access(AccessLevel.Manager)]
		public JsonResult PopulateAnswersForUser(long reviewId, long userId) {
			var answers = _ReviewAccessor.GetDistinctQuestionsAboutUserFromReview(GetUser(), userId, reviewId);

			return Json(answers.ToSelectList(x => x.Askable.GetQuestion(), x => x.Askable.Id), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public JsonResult PopulatePossibleAnswersForUser(long reviewId, long userId) {
			var existing = _ReviewAccessor.GetDistinctQuestionsAboutUserFromReview(GetUser(), userId, reviewId);
			//var period =PeriodAccessor.GetPeriodForReviewContainer(GetUser(), reviewId);

			var review = _ReviewAccessor.GetReviewContainer(GetUser(), reviewId, false, false, false);

			var range = new DateRange(review.DateCreated, DateTime.UtcNow);
			var answers = _AskableAccessor.GetAskablesForUser(GetUser(), userId, /*period.NotNull(x=>x.Id),*/ range).Where(x => existing.All(y => y.Askable.Id != x.Id)).ToListAlive();

			return Json(answers.ToSelectList(x => x.GetQuestion(), x => x.Id), JsonRequestBehavior.AllowGet);

		}


		[Access(AccessLevel.Manager)]
		public PartialViewResult RemoveQuestion(long id) {
			var users = _ReviewAccessor.GetUsersInReview(GetUser(), id);
			var usersSelect = users.ToSelectList(x => x.GetNameAndTitle(), x => x.Id);

			usersSelect.Insert(0, new SelectListItem() { Selected = true, Text = "Select User...", Value = "-1" });

			var model = new RemoveQuestionVM() {
				Users = usersSelect,
				SelectedQuestionId = -1,
				SelectedUserId = -1,
				ReviewContainerId = id,
			};

			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult RemoveQuestion(RemoveQuestionVM model) {
			_ReviewAccessor.RemoveQuestionFromReviewForUser(GetUser(), model.ReviewContainerId, model.SelectedUserId, model.SelectedQuestionId);
			return Json(ResultObject.Success("Removed question."));
		}

		[Access(AccessLevel.Manager)]
		public PartialViewResult AddQuestion(long id) {
			var users = _ReviewAccessor.GetUsersInReview(GetUser(), id);

			var userSelect = users.ToSelectList(x => x.GetNameAndTitle(), x => x.Id);
			userSelect.Insert(0, new SelectListItem() { Text = "Select a user...", Value = "-1", Selected = true });

			var model = new AddQuestionVM() {
				ReviewContainerId = id,
				Users = userSelect,
			};

			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult AddQuestion(AddQuestionVM model) {
			_ReviewAccessor.AddResponsibilityAboutUserToReview(GetUser(), model.ReviewContainerId, model.SelectedUserId, model.SelectedQuestionId);
			return Json(ResultObject.Success("Added question."));
		}

		public class EditDueDate {
			public long ReviewContainerId { get; set; }
			public DateTime ReviewDueDate { get; set; }
			public DateTime ReportDueDate { get; set; }
			public DateTime PrereviewDueDate { get; set; }
			public bool HasPrereview { get; set; }
			public double Offset { get; set; }
		}

		[HttpGet]
		[Access(AccessLevel.Manager)]
		public PartialViewResult DueDate(long id) {
			_PermissionsAccessor.Permitted(GetUser(), x => x.EditReviewContainer(id));
			var review = _ReviewAccessor.GetReviewContainer(GetUser(), id, false, false, false);

			var maxDate = DateTime.UtcNow;
			var minDate = DateTime.UtcNow;
			if (review.DueDate > maxDate) {
				maxDate = review.DueDate;
			}

			if (minDate > review.DueDate) {
				minDate = review.DueDate;
			}

			var model = new EditDueDate() {
				ReviewContainerId = id,
				PrereviewDueDate = (review.PrereviewDueDate ?? minDate),
				ReviewDueDate = review.DueDate,
				ReportDueDate = (review.ReportsDueDate ?? maxDate),
				HasPrereview = review.HasPrereview,
			};

			return PartialView(model);
		}

		[HttpPost]
		[Access(AccessLevel.Manager)]
		public JsonResult DueDate(EditDueDate model) {
			DateTime? prereview, report;
			try {
				prereview = GetUser().Organization.ConvertToUTC(model.PrereviewDueDate);//model.PrereviewDueDate.ToDateTime("MM-dd-yyyy", model.Offset + 24);
			} catch (FormatException) {
				prereview = null;
			}
			DateTime review = GetUser().Organization.ConvertToUTC(model.ReviewDueDate);// model.ReviewDueDate.ToDateTime("MM-dd-yyyy", model.Offset + 24);
			try {
				report = GetUser().Organization.ConvertToUTC(model.ReportDueDate);//model.ReportDueDate.ToDateTime("MM-dd-yyyy", model.Offset + 24);
			} catch (FormatException) {
				report = null;
			}

			_ReviewAccessor.UpdateDueDates(GetUser(), model.ReviewContainerId, prereview, review, report);
			return Json(ResultObject.Success("Due date changed."), JsonRequestBehavior.AllowGet);
		}
		#endregion
	}


}