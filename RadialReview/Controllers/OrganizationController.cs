using RadialReview.Accessors;
using RadialReview.Exceptions;
using RadialReview.Models;
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
    public class OrganizationController : BaseController
    {
        private OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        private static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
        private NexusAccessor _NexusAccessor = new NexusAccessor();

        //
        // GET: /Organization/
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create(int? count)
        {
            if (count == null)
                return RedirectToAction("Index");

            return View();
        }

        [HttpPost]
        public ActionResult Create(String name,Boolean managersCanEdit)
        {
            var user = GetUser();
            var basicPlan=_PaymentAccessor.BasicPaymentPlan();
            var localizedName=new LocalizedStringModel(){Default=new LocalizedStringPairModel(name)};
            var organization=_OrganizationAccessor.CreateOrganization(user, localizedName,managersCanEdit,basicPlan);
            return RedirectToAction("Manage", new { organizationId = organization.Id });
        }



        public ActionResult Join(String id)
        {
            var nexus = _NexusAccessor.Get(id);
            if (nexus.DateExecuted != null)
                throw new RedirectException(ExceptionStrings.AlreadyMember);
            var user = GetUser();
            var orgId = int.Parse(nexus.GetArgs()[0]);
            var placeholderUserId = long.Parse(nexus.GetArgs()[2]);
            if (user == null)
                return RedirectToAction("Login", "Account", new { returnUrl = "Organization/Join/" + id });
            try{
                var userOrg = GetOneUserOrganization(orgId);
                throw new RedirectException(ExceptionStrings.AlreadyMember);
            }
            catch (PermissionsException)
            {
                //We want to hit this exception.
                var org = _OrganizationAccessor.JoinOrganization(user, nexus.ByUserId, placeholderUserId);
                _NexusAccessor.Execute(nexus);
                return RedirectToAction("Index", "Home", new { message =String.Format(MessageStrings.SuccessfullyJoinedOrganization, org.Organization.Name)});
            }
        }

        public ActionResult ManageList()
        {
            var userOrgs = GetUserOrganizations();
            return View(userOrgs.Select(x => x.Organization).ToList());
        }


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
                    .Organization(questions:true,reviews:true)
                    .Reviews()
                    .Nexus()
                    .Execute();

                if (userOrg == null)
                    throw new PermissionsException();

                if (!userOrg.IsManager())
                    throw new PermissionsException();
                
                return View(new ManageViewModel(userOrg));
            }
        }

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

        public ActionResult Redirect(int organizationId, string returnUrl)
        {
            if (returnUrl.Contains("?"))
                return RedirectToLocal(returnUrl + "&organizationId=" + organizationId);
            else
                return RedirectToLocal(returnUrl + "?organizationId=" + organizationId);
        }
    }
}