using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
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

        [HttpPost]
        public JsonResult AddManagedUserToOrganization(String emailAddress, Boolean isManager, String jobTitle, int organizationId)
        {
            try
            {
                var user = GetOneUserOrganization(organizationId).Hydrate().Organization().Execute();
                var org = user.Organization;
                if (org == null)
                    throw new PermissionsException();
                if (org.Id != organizationId)
                    throw new PermissionsException();

                var nexusId = NexusAccessor.JoinOrganizationUnderManager(user, org, isManager, jobTitle, emailAddress);

                return Json(new JsonObject(false,"Success"));
            }
            catch (RedirectException e)
            {
                return Json(new JsonObject(e));
            }
            catch (Exception)
            {
                return Json(new JsonObject(true, ExceptionStrings.AnErrorOccuredContactUs));
            }
        }



        public ActionResult Index(String id)
        {
            if (id == null)
                throw new PermissionsException();
            var nexus=NexusAccessor.Get(id);
            switch(nexus.ActionCode)
            {
                case NexusActions.JoinOrganizationUnderManager: return RedirectToAction("Join", "Organization", new { id = id });
                case NexusActions.TakeReview: {
                    SignOut();
                    NexusAccessor.Execute(nexus);
                    return RedirectToAction("Index", "Review");
                };
            }

            return View();
        }
    }
}