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
            public long Id {get;set;}
            public List<SelectListItem> AdditionalUsers { get; set; }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Update(long id)
        {
            var review =_ReviewAccessor.GetReviewContainer(GetUser(),id,false);
            var users  =_ReviewAccessor.GetUsersInReview(GetUser(),id);

            var orgMembers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), review.ForOrganization.Id,false,false);

            var alsoExclude=_KeyValueAccessor.Get("AddToReviewReserved_" + id).Select(x => long.Parse(x.V)) ;

            var additionalMembers = orgMembers.Where(x => !users.Any(y => y.Id == x.Id)).Where(x=>!alsoExclude.Any(y=>y==x.Id)).ToListAlive();

            var model = new UpdateReviewsViewModel()
            {
                Id=id,
                AdditionalUsers=additionalMembers.ToSelectList(x=>x.GetNameAndTitle(youId:GetUser().Id),x=>x.Id)
            };
            return PartialView(model);
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult Update(UpdateReviewsViewModel model)
        {
            return Json(ResultObject.NoMessage());
        }


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
                new Task(() =>
                {
                    try
                    {
                        var result = _ReviewAccessor.AddUserToReviewContainer(user, id, userId);
                        Thread.Sleep(4000);
                        var hub = GlobalHost.ConnectionManager.GetHubContext<AlertHub>();
                        hub.Clients.User(userName).jsonAlert(result, true);
                        hub.Clients.User(userName).unhide("#ManageNotification");
                    }
                    catch (Exception e)
                    {
                        log.Error(e);
                    }
                    finally
                    {
                        _KeyValueAccessor.Remove(reservedId);
                    }
                }).Start();

                return Json(ResultObject.Create(false, "Adding user to review. This may take a few minutes..."));
            }
            catch (Exception e)
            {
                return Json(new ResultObject(e));
            }
        }

	}


}