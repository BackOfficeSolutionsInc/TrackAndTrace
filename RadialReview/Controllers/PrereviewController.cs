using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class PrereviewController : BaseController
    {
        //
        // GET: /Prereivew/
        [Access(AccessLevel.Manager)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Manager)]
        public ActionResult Customize(long id)
        {
            var prereviewId = id;
            var prereview = _PrereviewAccessor.GetPrereview(GetUser(), prereviewId);
            var review = _ReviewAccessor.GetReviewContainer(GetUser(), prereview.ReviewContainerId, false,false);

            if (prereview.PrereviewDue < DateTime.UtcNow)
            {
                throw new PermissionsException("The pre-review period has expired.");
            }

            //var membersAndSubordinates=_OrganizationAccessor.GetOrganizationMembersAndSubordinates(GetUser(),GetUser().Id,false);
            
            var teamId = review.ForTeamId;
            var customization = _ReviewEngine.GetCustomizeModel(GetUser(), teamId);
            var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

            var subordinates =GetUser().AsList().Union(_UserAccessor.GetDirectSubordinates(GetUser(),GetUser().Id)).ToList();
            customization.Subordinates = subordinates;
            customization.AllUsers = allUsers;
            ViewBag.PrereviewId = prereview.Id;            
            
            var selectedPrereivew = _PrereviewAccessor.GetCustomMatches(GetUser(), prereviewId);
            customization.Selected = selectedPrereivew;

            return View(customization);
        }

        [Access(AccessLevel.Manager)]
        [HttpPost]
        public ActionResult Customize(FormCollection form)
        {
            var whoReviewsWho = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x =>
            {
                var split = x.Split('_');
                return Tuple.Create(long.Parse(split[1]), long.Parse(split[2]));
            }).ToList();

            _PrereviewAccessor.ManagerCustomizePrereview(GetUser(), form["prereviewId"].ToLong(), whoReviewsWho);

            return RedirectToAction("Index", "Home");
        }
	}
}