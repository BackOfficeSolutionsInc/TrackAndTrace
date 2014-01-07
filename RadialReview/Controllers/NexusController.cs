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

namespace RadialReview.Controllers
{
    public class NexusController : BaseController
    {
        public static NexusAccessor NexusAccessor = new NexusAccessor();
        public static OrganizationAccessor OrganizationAccessor = new OrganizationAccessor();

        [HttpPost]
        [Access(AccessLevel.Manager)]
        public JsonResult AddManagedUserToOrganization(CreateUserOrganizationViewModel model)
        {
            try
            {
                var user = GetUser().Hydrate().Organization().Execute();
                var org = user.Organization;
                if (org == null)
                    throw new PermissionsException();
                if (org.Id != model.OrgId)
                    throw new PermissionsException();

                if (model.Position.CustomPosition!=null)
                {
                    var newPosition = OrganizationAccessor.EditOrganizationPosition(user,0, user.Organization.Id, model.Position.CustomPositionId, model.Position.CustomPosition);
                    model.Position.PositionId = newPosition.Id;
                }

                var nexusId = NexusAccessor.JoinOrganizationUnderManager(user,model.ManagerId, model.IsManager, model.Position.PositionId, model.Email,model.FirstName,model.LastName);

                var message="Successfully added "+model.FirstName+" "+model.LastName+".";
                if(GetUser().Organization.SendEmailImmediately)
                {
                    message += " An invitation has been sent to " + model.Email + ".";
                    return Json(ResultObject.CreateMessage(StatusType.Success, message));
                }else{
                    message+=" The invitation has NOT been sent. To send, click \"Send Invites\" below.";
                    return Json(ResultObject.CreateMessage(StatusType.Warning, message));
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
        public JsonResult SendAllEmails()
        {
            var count=NexusAccessor.SendAllJoinEmails(GetUser(), GetUser().Organization.Id);
            return Json(ResultObject.Create(true,"Sent "+count+" email".Pluralize(count)+"."),JsonRequestBehavior.AllowGet);
        }
        
        [Access(AccessLevel.Any)]
        public ActionResult Index(String id)
        {
            try
            {
                if (id == null)
                    throw new PermissionsException();
                var nexus = NexusAccessor.Get(id);
                switch (nexus.ActionCode)
                {
                    case NexusActions.JoinOrganizationUnderManager: return RedirectToAction("Join", "Organization", new { id = id });
                    case NexusActions.TakeReview:
                        {
                            SignOut();
                            NexusAccessor.Execute(nexus);
                            return RedirectToAction("Index", "Review");
                        };
                }

            return View();
            }catch(Exception e)
            {
                return RedirectToAction("Index", "Home");
            }
        }
    }
}