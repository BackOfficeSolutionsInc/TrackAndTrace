using Microsoft.Ajax.Utilities;
using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using RadialReview.Models;
using RadialReview.Utilities;
using System.Threading.Tasks;
using System.Threading;

namespace RadialReview.Controllers {
	public class NexusController : BaseController {
		public static NexusAccessor NexusAccessor = new NexusAccessor();
		public static OrganizationAccessor OrganizationAccessor = new OrganizationAccessor();


		private ActionResult MatchingNexus(NexusModel nexus, Func<ActionResult> otherwise) {
			try {
				if (!NexusAccessor.IsCorrectUser(GetUser(), nexus)) {
					ViewBag.Message = "Incorrect user.";
					throw new Exception();
				}
				return otherwise();
			} catch (Exception) {
#pragma warning disable CS0618 // Type or member is obsolete
				var u = _UserAccessor.GetUserOrganizationUnsafe(nexus.ForUserId);
#pragma warning restore CS0618 // Type or member is obsolete
				var username = u.GetUsername();
				try {
					SignOut();
					if (u.IsAttached())
						return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.AbsolutePath, username = username });
					else
						return RedirectToAction("Register", "Account", new { returnUrl = Request.Url.AbsolutePath });
				} catch (Exception) {
					return RedirectToAction("Login", "Account");
				}
			}
		}
		
		[Access(AccessLevel.Any)]
		[AsyncTimeout(60 * 60 * 1000)]
		[Obsolete("Fix for AC")]
		public async Task<ActionResult> Index(CancellationToken ct, String id) {
			var rethrow = false;
			try {
				if (id == null)
					throw new PermissionsException();
				var nexus = NexusAccessor.Get(id);
				switch (nexus.ActionCode) {
					case NexusActions.JoinOrganizationUnderManager: {
							return RedirectToAction("Join", "Organization", new { id = id });
						}
					case NexusActions.TakeReview: {
							//return MatchingNexus(nexus, () =>
							//{
							var review = nexus.GetArgs().FirstOrDefault();
							NexusAccessor.Execute(nexus);
							if (review == null)
								return RedirectToAction("Index", "Reviews");

							var reviewId = review.TryParseLong();
							if (reviewId == null)
								return RedirectToAction("Index", "Reviews");

							return RedirectToAction("Take", "Review", new { Id = reviewId });
							//});
						};
					case NexusActions.ResetPassword: {
							SignOut();
							return RedirectToAction("ResetPasswordWithToken", "Account", new { Id = id });
						};
					case NexusActions.Prereview: {
							return MatchingNexus(nexus, () => {
								NexusAccessor.Execute(nexus);
								return RedirectToAction("Customize", "Prereview", new { id = nexus.GetArgs()[1] });
							});
						};
					case NexusActions.CreateReview: {
							rethrow = true;
							Server.ScriptTimeout = 60 * 60;
							Session.Timeout = 60;

							if (nexus.DateExecuted != null)
								throw new PermissionsException("CreateReview already executed.");

							// HttpContext.Server.ScriptTimeout = 60*20;
							var sent = await _ReviewEngine.CreateReviewFromPrereview(System.Web.HttpContext.Current, nexus);
							NexusAccessor.Execute(nexus);
							return Content("Sent:" + sent);
						};
				}
			} catch (Exception e) {
				log.Error("Error executing nexus", e);
				//ViewBag.Message = "Could not access resource. Make sure you're logging in with the correct account.";
				ViewBag.Message = e.Message;
				if (rethrow)
					throw e;

				return RedirectToAction("Index", "Home");
				/*log.Error("Error executing nexus",e);
				ViewBag.Message = "There was an error in your request.";
				ViewBag.AlertType = "alert-danger";
                return Con("Index", "Home");*/
			}
			log.Fatal("Nexus fall-through");
			ViewBag.Message = "Action could not be performed. (2)";
			return View();
		}

		[HttpPost]
		[Access(AccessLevel.UserOrganization)]
		public async Task<JsonResult> AddManagedUserToOrganization(CreateUserOrganizationViewModel model, long? meeting = null) {
			try {
				var result = await _UserAccessor.CreateUser(GetUser(), model);
				var createdUser = result.Item2;

				if (meeting != null) {
					try {
						await L10Accessor.AddAttendee(GetUser(), meeting.Value, createdUser.Id);
					} catch (Exception) {
						throw new PermissionsException("Could not add to meeting.");
					}
				}
				if (model.NodeId != null) {
					AccountabilityAccessor.SetUser(GetUser(), model.NodeId.Value, createdUser.Id);
				}
				var message = "Successfully added " + model.FirstName + " " + model.LastName + ".";
				ResultObject res;
				if (model.SendEmail) {
					message += " An invitation has been sent to " + model.Email + ".";
					res = ResultObject.Create(null, message);
				} else {
					res = ResultObject.Create(null, message, StatusType.Success);
				}
				if (model.NodeId == null) {
					res.ForceRefresh();
					res.Message = "Successfully added " + model.FirstName + " " + model.LastName + ".";
				}
				return Json(res);
			} catch (RedirectException e) {
				return Json(new ResultObject(e));
			} catch (Exception e) {
				log.Error(e);
				return Json(new ResultObject(true, ExceptionStrings.AnErrorOccuredContactUs));
			}
		}

		[Access(AccessLevel.Manager)]
		public async Task<JsonResult> SendAllEmails() {
			var output = await JoinOrganizationAccessor.SendAllJoinEmails(GetUser(), GetUser().Organization.Id);

			var errors = output.Errors.Select(x => x.Message).GroupBy(x => x).Select(x => {
				var r = x.First();
				if (x.Count() > 1)
					r += " (x" + x.Count() + ")";
				return r;
			}).ToList();

			var sent = "Sent " + output.Sent + " email".Pluralize(output.Sent) + ".";
			errors.Insert(0, sent);
			return Json(ResultObject.Create(true, String.Join("<br/>", errors)), JsonRequestBehavior.AllowGet);
		}

		[Access(AccessLevel.Manager)]
		public async Task<JsonResult> ResendAllEmails() {
			var result = await JoinOrganizationAccessor.ResendAllEmails(GetUser(), GetUser().Organization.Id);
			return Json(result.ToResults("Successfully sent {0} out of {2}."), JsonRequestBehavior.AllowGet);
		}

	}
}