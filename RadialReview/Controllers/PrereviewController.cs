using FluentNHibernate.Utils;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Reviews;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Utilities;

namespace RadialReview.Controllers
{
    public class PrereviewController : BaseController
    {
		protected CustomizeModel CreateCustomize(long prereviewId)
	    {

			var prereview = _PrereviewAccessor.GetPrereview(GetUser(), prereviewId);
			var review = _ReviewAccessor.GetReviewContainer(GetUser(), prereview.ReviewContainerId, false, false);

			if (prereview.PrereviewDue < DateTime.UtcNow)
			{
				throw new PermissionsException("The pre-review period has expired.");
			}

			//var membersAndSubordinates=_OrganizationAccessor.GetOrganizationMembersAndSubordinates(GetUser(),GetUser().Id,false);

			var teamId = review.ForTeamId;
			var customization = _ReviewEngine.GetCustomizeModel(GetUser(), teamId,true);
			var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			var subordinates = GetUser().AsList().Union(_UserAccessor.GetDirectSubordinates(GetUser(), GetUser().Id)).ToList();
			customization.Subordinates = subordinates;
			customization.AllReviewees = allUsers.Cast<ResponsibilityGroupModel>().ToList();
			ViewBag.PrereviewId = prereview.Id;

			var selectedPrereivew = _PrereviewAccessor.GetCustomMatches(GetUser(), prereviewId);
			customization.Selected = selectedPrereivew;

			customization.AllReviewees.Add(GetUser().Organization);

			return customization;
	    }

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
	        var customization = CreateCustomize(prereviewId);
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


	    public class UpdateVM
	    {
			public long ForUserId { get; set; }
			public ReviewModel Review { get; set; }
		    public List<UserOrganizationModel> AllUsers { get; set; }
			public List<long> Selected { get; set; } 
	    }

	    [Access(AccessLevel.Manager)]
	    public ActionResult Update(long id)
	    {
		    var reviewId = id;
		    var review =_ReviewAccessor.GetReview(GetUser(), id, true);

			var teamId = review.ForReviewContainer.ForTeamId;
			var customization = _ReviewEngine.GetCustomizeModel(GetUser(), teamId,false);

			customization.AllReviewees = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false)
				.Cast<ResponsibilityGroupModel>().ToList(); ;
		    customization.Subordinates = review.ForUser.AsList();
	
			customization.Selected= review.Answers.GroupBy(x => x.AboutUserId).Select(x => Tuple.Create(review.ForUserId,x.Key)).ToList();
			ViewBag.ReviewContainerId = review.ForReviewsId;
			ViewBag.ReviewId = review.Id;
			return View(customization);
	    }


	    [Access(AccessLevel.Manager)]
		[HttpPost]
	    public ActionResult Update(FormCollection form)
		{
			var newVals = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x =>
			{
				var split = x.Split('_');
				return Tuple.Create(long.Parse(split[1]), long.Parse(split[2]));
			}).ToList();

			var oldVals = form.AllKeys.Where(x => x.StartsWith("originalCustomize_")).Select(x =>
			{
				var split = x.Split('_');
				return Tuple.Create(long.Parse(split[1]), long.Parse(split[2]));
			}).ToList();
			
		    //IEnumerable<Tuple<long, long>> added;
			//IEnumerable<Tuple<long, long>> removed;

			var ar = SetUtility.AddRemove(oldVals, newVals);
		    var added = ar.AddedValues;
		    var removed = ar.RemovedValues;

		    var reviewContainerId = form["reviewContainerId"].ToLong();

			foreach (var a in added){
				_ReviewAccessor.AddToReview(GetUser(), a.Item1, reviewContainerId, a.Item2);
			}

			foreach (var r in removed){
				_ReviewAccessor.RemoveFromReview(GetUser(), r.Item1, reviewContainerId, r.Item2);
			}
			
			return RedirectToAction("Index", "Reports", new { id = reviewContainerId });
		}
	}
}

