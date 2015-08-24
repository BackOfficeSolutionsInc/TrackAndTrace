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

namespace RadialReview.Controllers
{
    public class NexusController : BaseController
    {
        public static NexusAccessor NexusAccessor = new NexusAccessor();
        public static OrganizationAccessor OrganizationAccessor = new OrganizationAccessor();

       
        private ActionResult MatchingNexus(NexusModel nexus,Func<ActionResult> otherwise)
        {
            try
            {
                if (GetUser().Id != nexus.ForUserId)
                    throw new Exception();
                return otherwise();
            }
            catch (Exception)
            {
                var u = _UserAccessor.GetUserOrganizationUnsafe(nexus.ForUserId);
                var username = u.GetUsername();
                try
                {
                    SignOut();
                    if (u.IsAttached())
                        return RedirectToAction("Login", "Account", new { returnUrl = Request.Url.AbsolutePath, username = username });
                    else
                        return RedirectToAction("Register", "Account", new { returnUrl = Request.Url.AbsolutePath });
                }
                catch (Exception)
                {
                    return RedirectToAction("Login", "Account");
                }
            }
        }
        
        [Access(AccessLevel.Any)]
        public async Task<ActionResult> Index(String id)
        {
            try
            {
                if (id == null)
                    throw new PermissionsException();
                var nexus = NexusAccessor.Get(id);
                switch (nexus.ActionCode)
                {
                    case NexusActions.JoinOrganizationUnderManager:
                        {
                            return RedirectToAction("Join", "Organization", new { id = id });
                        }
                    case NexusActions.TakeReview:
                        {
                            return MatchingNexus(nexus, () =>
                            {
                                NexusAccessor.Execute(nexus);
                                return RedirectToAction("Index", "Tasks");
                            });
                        };
                    case NexusActions.ResetPassword:
                        {
                            SignOut();
                            return RedirectToAction("ResetPasswordWithToken", "Account", new { Id = id });
                        };
                    case NexusActions.Prereview:
                        {
                            return MatchingNexus(nexus, () =>{
                                NexusAccessor.Execute(nexus);
                                return RedirectToAction("Customize", "Prereview", new { id = nexus.GetArgs()[1] });
                            });
                        };
                    case NexusActions.CreateReview:
                        {
							var sent = await _ReviewEngine.CreateReviewFromPrereview(System.Web.HttpContext.Current, nexus);
							NexusAccessor.Execute(nexus);
                            return Content("Sent:"+ sent);
                        };
                }
            }
            catch (Exception e){
				log.Error("Error executing nexus", e);
	            throw;
				/*log.Error("Error executing nexus",e);
				ViewBag.Message = "There was an error in your request.";
				ViewBag.AlertType = "alert-danger";
                return Con("Index", "Home");*/
            }
            log.Fatal("Nexus fall-through");
            return View();
        }

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> AddManagedUserToOrganization(CreateUserOrganizationViewModel model)
        {
            UserOrganizationModel createdUser=null;
            try
            {
				//var user = GetUser().Hydrate().Organization().Execute();
				//var org = user.Organization;
				//if (org == null)
				//	throw new PermissionsException();
				//if (org.Id != model.OrgId)
				//	throw new PermissionsException();

				//if (model.Position.CustomPosition != null)
				//{
				//	var newPosition = OrganizationAccessor.EditOrganizationPosition(user, 0, user.Organization.Id, model.Position.CustomPositionId, model.Position.CustomPosition);
				//	model.Position.PositionId = newPosition.Id;
				//}

				//var nexusIdandUser = await NexusAccessor.JoinOrganizationUnderManager(
				//		user, model.ManagerId, model.IsManager,
				//		model.Position.PositionId, model.Email,
				//		model.FirstName, model.LastName
				//	);
				//createdUser=nexusIdandUser.Item2;
				//var nexusId=nexusIdandUser.Item1;

	            await _UserAccessor.CreateUser(GetUser(), model);


                var message = "Successfully added " + model.FirstName + " " + model.LastName + ".";
                if (GetUser().Organization.SendEmailImmediately)
                {
                    message += " An invitation has been sent to " + model.Email + ".";
					return Json(ResultObject.Create(null, message).ForceRefresh());
                }
                else
                {
                    message += " The invitation has NOT been sent. To send, click \"Send Invites\" below.";
                    return Json(ResultObject.Create(null, message, StatusType.Warning).ForceRefresh());
                }
            }
            catch (RedirectException e)
            {
                return Json(new ResultObject(e));
            }
            catch (Exception)
            {
                return Json(new ResultObject(true, ExceptionStrings.AnErrorOccuredContactUs));
            }
        }

        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> SendAllEmails()
        {
            var output = await NexusAccessor.SendAllJoinEmails(GetUser(), GetUser().Organization.Id);

	        var errors = output.Errors.Select(x => x.Message).GroupBy(x => x).Select(x =>{
		        var r = x.First();
		        if (x.Count() > 1)
			        r += " (x" + x.Count() + ")";
		        return r;
	        }).ToList();

	        var sent = "Sent " + output.Sent + " email".Pluralize(output.Sent) + ".";
			errors.Insert(0,sent);
            return Json(ResultObject.Create(true, String.Join("<br/>",errors)), JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Manager)]
        public async Task<JsonResult> ResendAllEmails()
        {
            var result = await NexusAccessor.ResendAllEmails(GetUser(), GetUser().Organization.Id);
            return Json(result.ToResults("Successfully sent {0} out of {2}."), JsonRequestBehavior.AllowGet);
        }

    }
}