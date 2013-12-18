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

                return Json(new ResultObject(false,"Success"));
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
        
        [Access(AccessLevel.Any)]
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