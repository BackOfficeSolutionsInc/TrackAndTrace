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

namespace RadialReview.Controllers {
	public class PrereviewController : BaseController {
		protected CustomizeModel CreateCustomize(long prereviewId) {

			var prereview = _PrereviewAccessor.GetPrereview(GetUser(), prereviewId);
			var review = _ReviewAccessor.GetReviewContainer(GetUser(), prereview.ReviewContainerId, false, false);

			if (prereview.PrereviewDue < DateTime.UtcNow) {
				throw new PermissionsException("The pre-review period has expired.");
			}

			//var membersAndSubordinates=_OrganizationAccessor.GetOrganizationMembersAndSubordinates(GetUser(),GetUser().Id,false);

			var teamId = review.ForTeamId;
			var customization = _ReviewEngine.GetCustomizeModel(GetUser(), teamId, true, review.GetDateRange(true));
			//var allUsers = _OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false);

			//var allReviewees = ReviewAccessor.GetPossibleOrganizationReviewees(GetUser(), review.OrganizationId, review.GetDateRange(true));

			var subordinates = GetUser().AsList().Union(_UserAccessor.GetDirectSubordinates(GetUser(), GetUser().Id)).ToList();
			customization.Reviewers = subordinates.Select(x=>new Reviewer(x)).Distinct().ToList();
			//customization.AllReviewees = allReviewees;
			ViewBag.PrereviewId = prereview.Id;

			var selectedPrereivew = _PrereviewAccessor.GetCustomMatches(GetUser(), prereviewId);
			customization.Selected = selectedPrereivew;

			//customization.AllReviewees.Add(GetUser().Organization);

			return customization;
		}

		//
		// GET: /Prereivew/
		[Access(AccessLevel.Manager)]
		public ActionResult Index() {
			return View();
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Customize(long id) {
			var prereviewId = id;
			var customization = CreateCustomize(prereviewId);
			return View(customization);
		}

		[Access(AccessLevel.Manager)]
		[HttpPost]
		public ActionResult Customize(FormCollection form) {
			var whoReviewsWho = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x => {
				var split = x.Split('_');
				long? acNodeId = null;

				if (split.Length > 3) {
					var temp = 0L;
					if (long.TryParse(split[3], out temp))
						acNodeId = temp;
				}
				return new WhoReviewsWho(new Reviewer(long.Parse(split[1])), new Reviewee(long.Parse(split[2]),acNodeId));
			}).ToList();

			_PrereviewAccessor.ManagerCustomizePrereview(GetUser(), form["prereviewId"].ToLong(), whoReviewsWho);

			return RedirectToAction("Index", "Home");
		}


		public class UpdateVM {
			public long ForUserId { get; set; }
			public ReviewModel Review { get; set; }
			public List<UserOrganizationModel> AllUsers { get; set; }
			public List<long> Selected { get; set; }
		}

		[Access(AccessLevel.Manager)]
		public ActionResult Update(long id) {
			var reviewId = id;
			var review = _ReviewAccessor.GetReview(GetUser(), id, true);

			var teamId = review.ForReviewContainer.ForTeamId;
			var customization = _ReviewEngine.GetCustomizeModel(GetUser(), teamId, false);

				
				//_OrganizationAccessor.GetOrganizationMembers(GetUser(), GetUser().Organization.Id, false, false)
				//.Cast<ResponsibilityGroupModel>().ToList();

			customization.Reviewers = new Reviewer(review.ReviewerUser).AsList();

			customization.Selected = review.Answers.GroupBy(x => Tuple.Create(x.RevieweeUserId, x.RevieweeUser_AcNodeId))
												   .Select(x => new WhoReviewsWho(new Reviewer(review.ReviewerUserId), new Reviewee(x.Key.Item1, x.Key.Item2)))
												   .ToList();

			ViewBag.ReviewContainerId = review.ForReviewContainerId;
			ViewBag.ReviewId = review.Id;
			return View(customization);
		}


		[Access(AccessLevel.Manager)]
		[HttpPost]
		[Obsolete("Fix for AC")]
		public ActionResult Update(FormCollection form) {
			var newVals = form.AllKeys.Where(x => x.StartsWith("customize_")).Select(x => {
				var split = x.Split('_');
				var acNodeId = split.Length > 3 ? split[3].TryParseLong() : null;
				return new WhoReviewsWho(new Reviewer(long.Parse(split[1])), new Reviewee(long.Parse(split[2]), acNodeId));
			}).ToList();

			var oldVals = form.AllKeys.Where(x => x.StartsWith("originalCustomize_")).Select(x => {
				var split = x.Split('_');
				var acNodeId = split.Length > 3 ? split[3].TryParseLong() : null;
				return new WhoReviewsWho(new Reviewer(long.Parse(split[1])), new Reviewee(long.Parse(split[2]), acNodeId));
			}).ToList();

			//IEnumerable<Tuple<long, long>> added;
			//IEnumerable<Tuple<long, long>> removed;

			var ar = SetUtility.AddRemove(oldVals, newVals);
			var added = ar.AddedValues;
			var removed = ar.RemovedValues;

			var reviewContainerId = form["reviewContainerId"].ToLong();

			foreach (var a in added) {
				_ReviewAccessor.AddToReview(GetUser(), a.Reviewer, reviewContainerId, a.Reviewee);
			}

			foreach (var r in removed) {
				_ReviewAccessor.RemoveFromReview(GetUser(), r.Reviewer, reviewContainerId, r.Reviewee);
			}
			return new RedirectResult(Url.Action("Index", "Reports",new { id = reviewContainerId }) + "#ReviewerReviewee");
			//return RedirectToAction("Index", "", );
		}
	}
}

