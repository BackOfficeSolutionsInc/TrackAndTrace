using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Json;
using RadialReview.Models.UserModels;
using RadialReview.Models.ViewModels;
using RadialReview.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class OrganizationController : BaseController
    {
        //
        // GET: /Organization/
        [Access(AccessLevel.Any)]
        public ActionResult Index()
        {
            return View();
        }

        [Access(AccessLevel.Any)]
        public ActionResult Create(int? count)
        {
            var user = GetUserModel();
            if (count == null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        [Access(AccessLevel.Any)]
        public ActionResult Create(String name)
        {
            Boolean managersCanEdit = false;
            var user = GetUserModel();
            var basicPlan=_PaymentAccessor.BasicPaymentPlan();
            var localizedName=new LocalizedStringModel(){Standard=name};
            long newRoleId;
            var organization = _OrganizationAccessor.CreateOrganization(user, localizedName, managersCanEdit, basicPlan,DateTime.UtcNow,out newRoleId);
            return RedirectToAction("SetRole", "Account", new { id = newRoleId });
        }

        [Access(AccessLevel.Any)]
        public ActionResult Join(String id)
        {
            var nexus = _NexusAccessor.Get(id);
            if (nexus.DateExecuted != null)
                throw new RedirectException(ExceptionStrings.AlreadyMember);
            var user = GetUserModel();
            var orgId = int.Parse(nexus.GetArgs()[0]);
            var placeholderUserId = long.Parse(nexus.GetArgs()[2]);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "Organization/Join/" + id });
            try{
                var userOrg = GetUser(placeholderUserId);
                if (!user.IsRadialAdmin)
                {
                    throw new RedirectException(ExceptionStrings.AlreadyMember);
                }
                else
                {
                    throw new OrganizationIdException();
                }
            }
            catch (OrganizationIdException)
            {
                //We want to hit this exception.
                Session["OrganizationId"] = null;
                var org = _OrganizationAccessor.JoinOrganization(user, nexus.ByUserId, placeholderUserId);
                _NexusAccessor.Execute(nexus);
                return RedirectToAction("Index", "Home", new { message =String.Format(MessageStrings.SuccessfullyJoinedOrganization, org.Organization.Name)});
            }
        }
        /*
        [Access(AccessLevel.UserOrganization)]
        public ActionResult ManageList()
        {
        }*/

        /*
        public ActionResult Manage(int? organizationId)
        {
            if (organizationId == null)
            {
                var userOrgs = GetUserOrganizations();
                var managing = userOrgs.Where(x => x.IsManager());
                var count = managing.Count();
                if (count == 0)
                    throw new PermissionsException();
                else if (count == 1)
                    return RedirectToAction("Manage", new { organizationId = managing.First().Organization.Id });
                else
                    return View("ManageList", managing.Select(x => x.Organization).ToList());
            }
            else
            {
                var userOrg = GetOneUserOrganization(organizationId.Value)
                    .Hydrate()
                    .ManagingGroups(questions:true)
                    .ManagingUsers(subordinates:true)
                    .Organization(questions:true)
                    .Reviews(answers:true)
                    .Nexus()
                    .Execute();

                if (userOrg == null)
                    throw new PermissionsException();

                if (!userOrg.IsManager())
                    throw new PermissionsException();
                
                return View(new ManageViewModel(userOrg));
            }
        }*/

        [Access(AccessLevel.Any)]
        public ActionResult Begin(int? count = null)
        {
            ViewBag.Count = count;
            int[] roundUp = new int[] { 10, 15, 25, 50, 100, 500 };
            double[] prices = new double[] { 0, 199, 499, 999, 1999, 3999, Double.MaxValue };

            if (count != null)
            {
                ViewBag.Price = prices[0];
                for (int i = 0; i < roundUp.Length; i++)
                {
                    if (count > roundUp[i])
                    {
                        ViewBag.Price = prices[i + 1];
                    }
                }
            }
            return View();
        }

        [Access(AccessLevel.Any)]
        public ActionResult Redirect(int organizationId, string returnUrl)
        {
            if (returnUrl.Contains("?"))
                return RedirectToLocal(returnUrl + "&organizationId=" + organizationId);
            else
                return RedirectToLocal(returnUrl + "?organizationId=" + organizationId);
        }


        [Access(AccessLevel.UserOrganization)]
        public ActionResult Tree(string type = "cartesian")
        {
            if (type.ToLower() == "radial")
            {
                return View("RadialTree", GetUser().Organization.Id);
            }
            else if (type.ToLower() == "forcedirected")
            {
                return View("ForceDirected", GetUser().Organization.Id);
            }
            else
            {
                return View("Tree", GetUser().Organization.Id);
            }
        }

        [Access(AccessLevel.Manager)]
        public ActionResult ResendJoin(long id)
        {
            var found = _UserAccessor.GetUserOrganization(GetUser(), id, true, false);

            if (found.TempUser == null)
                throw new PermissionsException("User is already a part of the organization");

            return PartialView(found.TempUser);
        }

        [Access(AccessLevel.Manager)]
        [HttpPost]
        public async Task<JsonResult> ResendJoin(long id,TempUserModel model,long TempId)
        {
            var found = _UserAccessor.GetUserOrganization(GetUser(), id, true, false);
            if (found.TempUser==null)
                throw new PermissionsException("User is already a part of the organization");

            _UserAccessor.UpdateTempUser(GetUser(), id, model.FirstName, model.LastName, model.Email, model.LastSent);
            model.Id = TempId;
            var result = await Emailer.SendEmail(_NexusAccessor.CreateJoinEmailToGuid(GetUser(), model));

            return Json(result.ToResults("Resent invite to "+model.Name()+"."));
        }
    }
}