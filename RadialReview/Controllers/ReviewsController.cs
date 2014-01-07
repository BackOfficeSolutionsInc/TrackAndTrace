using Microsoft.AspNet.SignalR;
using RadialReview.Accessors;
using RadialReview.Hubs;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class ReviewsController : BaseController
    {
        protected static ReviewAccessor _ReviewAccessor = new ReviewAccessor();
        protected static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        protected static KeyValueAccessor _KeyValueAccessor = new KeyValueAccessor();
        protected static ResponsibilitiesAccessor _ResponsibilitiesAccessor = new ResponsibilitiesAccessor();


        [Access(AccessLevel.Manager)]
        public ActionResult Details(long id)
        {
            //var reviewContainerAnswers = _ReviewAccessor.GetReviewContainerAnswers(GetUser(), id);

            //var reviewContainerPeople = reviewContainerAnswers.GroupBy(x => x.AboutUserId);
            var user = GetUser().Hydrate().ManagingUsers(true).Execute();

            var reviewContainer = _ReviewAccessor.GetReviewContainer(user, id, true);
            //reviewContainer.Reviews = _ReviewAccessor.GetReviewsForReviewContainer(GetUser(), id);

            
            foreach (var r in reviewContainer.Reviews)
            {
                if (r.ForUser.Id == GetUser().Id && reviewContainer.CreatedById == GetUser().Id)
                    r.ForUser.SetPersonallyManaging(true);
                else
                    r.ForUser.PopulatePersonallyManaging(user, user.AllSubordinates);
            }

            var model = new ReviewsViewModel(reviewContainer);

            return View(model);
        }

        public class UpdateReviewsViewModel
        {
            public long ReviewId {get;set;}
            public List<SelectListItem> AdditionalUsers { get; set; }
            public long SelectedUserId { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Update(long id)
        {
            var review =_ReviewAccessor.GetReviewContainer(GetUser(),id,false);
            var users  =_ReviewAccessor.GetUsersInReview(GetUser(),id);

            var orgMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), review.ForOrganization.Id,false,false);
            //Add to review
            var alsoExclude=_KeyValueAccessor.Get("AddToReviewReserved_" + id).Select(x => long.Parse(x.V)) ;
            var additionalMembers = orgMembers.Where(x => !users.Any(y => y.Id == x.Id)).Where(x=>!alsoExclude.Any(y=>y==x.Id)).ToListAlive();
            
            var model = new UpdateReviewsViewModel()
            {
                ReviewId=id,
                AdditionalUsers=additionalMembers.ToSelectList(x=>x.GetNameAndTitle(youId:GetUser().Id),x=>x.Id)
            };
            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> Update(UpdateReviewsViewModel model)
        {
            var reservedId = _KeyValueAccessor.Put("AddToReviewReserved_" + model.ReviewId, "" + model.SelectedUserId);
            var userName = GetUserModel().UserName;

            var user = GetUser();
            try
            {
                //TODO HERE
                var result = await Task.Run<ResultObject>(() =>
                {
                    ResultObject output;
                    try
                    {
                        output = _ReviewAccessor.AddUserToReviewContainer(user, model.ReviewId, model.SelectedUserId);
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                        output = new ResultObject(e);
                    }
                    finally
                    {
                        _KeyValueAccessor.Remove(reservedId);
                    }
                    return output;
                });

                new Thread(() =>
                {
                    Thread.Sleep(4000);
                    var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                    hub.Clients.User(userName).jsonAlert(result, true);
                    hub.Clients.User(userName).unhide("#ManageNotification");
                }).Start();

            }
            catch (Exception e)
            {
                log.Error(e);
                new Thread(() =>
                {
                    var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                    hub.Clients.User(userName).jsonAlert(new ResultObject(e));
                    hub.Clients.User(userName).unhide("#ManageNotification");
                }).Start();
            }
            return Json(ResultObject.SilentSuccess());
        }

        /*
        [HttpPost]
        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> AddToReview(long id, long userId)
        {
            var reservedId=_KeyValueAccessor.Put("AddToReviewReserved_" + id,""+ userId);
            var userName = GetUserModel().UserName;

            var user=GetUser();
            try
            {
                //TODO HERE
                var result = await Task.Run<ResultObject>(() =>
                {
                    ResultObject output;
                    try
                    {
                        output = _ReviewAccessor.AddUserToReviewContainer(user, id, userId);
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                        output = new ResultObject(e);
                    }
                    finally
                    {
                        _KeyValueAccessor.Remove(reservedId);
                    }
                    return output;
                });

                new Thread(() =>
                {
                    Thread.Sleep(4000);
                    var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                    hub.Clients.User(userName).jsonAlert(result, true);
                    hub.Clients.User(userName).unhide("#ManageNotification");
                }).Start();

            }
            catch (Exception e)
            {
                log.Error(e);
                new Thread(() =>
                {
                    var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                    hub.Clients.User(userName).jsonAlert(new ResultObject(e));
                    hub.Clients.User(userName).unhide("#ManageNotification");
                }).Start();
            }
            return Json(ResultObject.SilentSuccess());

        }*/

        #region Individual Question
        public class RemoveQuestionVM
        {
            public long ReviewContainerId { get; set; }
            public List<SelectListItem> Users { get; set; }
            public long SelectedUserId { get; set; }
            public long SelectedQuestionId { get; set; }
        }
        public class AddQuestionVM
        {
            public long ReviewContainerId { get; set; }
            public List<SelectListItem> Users { get; set; }
            public long SelectedUserId { get; set; }
            public long SelectedQuestionId { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public JsonResult PopulateAnswersForUser(long reviewId,long userId)
        {
            var answers = _ReviewAccessor.GetDistinctQuestionsAboutUserFromReview(GetUser(), userId, reviewId);

            return Json(answers.ToSelectList(x => x.Askable.GetQuestion(), x => x.Askable.Id), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public JsonResult PopulatePossibleAnswersForUser(long reviewId, long userId)
        {
            var existing = _ReviewAccessor.GetDistinctQuestionsAboutUserFromReview(GetUser(), userId, reviewId);
            var answers=_ResponsibilitiesAccessor.GetResponsibilityGroupsForUser(GetUser(),userId).SelectMany(x=>x.Responsibilities).Where(x=>!existing.Any(y=>y.Askable.Id==x.Id)).ToListAlive();

            return Json(answers.ToSelectList(x => x.GetQuestion(), x => x.Id), JsonRequestBehavior.AllowGet);

        }

  
        [Access(AccessLevel.Manager)]
        public ActionResult RemoveQuestion(long id)
        {
            var users = _ReviewAccessor.GetUsersInReview(GetUser(), id);
            var usersSelect= users.ToSelectList(x=>x.GetNameAndTitle(),x=>x.Id);

            usersSelect.Insert(0, new SelectListItem() {Selected=true,Text="Select User...",Value="-1"});

            var model = new RemoveQuestionVM()
            {
                Users =usersSelect,
                SelectedQuestionId = -1,
                SelectedUserId = -1,
                ReviewContainerId=id,
            };

            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult RemoveQuestion(RemoveQuestionVM model)
        {
            _ReviewAccessor.RemoveQuestionFromReviewForUser(GetUser(), model.ReviewContainerId, model.SelectedUserId, model.SelectedQuestionId);
            return Json(ResultObject.Success("Removed question."));
        }

        [Access(AccessLevel.Manager)]
        public ActionResult AddQuestion(long id)
        {
            var users = _ReviewAccessor.GetUsersInReview(GetUser(), id);

            var userSelect=users.ToSelectList(x => x.GetNameAndTitle(), x => x.Id);
            userSelect.Insert(0,new SelectListItem(){Text="Select a user...",Value="-1",Selected=true});

            var model = new AddQuestionVM()
            {
                ReviewContainerId = id,
                Users = userSelect,
            };

            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult AddQuestion(AddQuestionVM model)
        {
            _ReviewAccessor.AddResponsibilityAboutUserToReview(GetUser(), model.ReviewContainerId,model.SelectedUserId,model.SelectedQuestionId);
            return Json(ResultObject.Success("Added question."));
        }

        #endregion

	}


}