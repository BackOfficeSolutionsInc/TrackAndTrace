using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers
{
    public class PaymentController : BaseController
    {
        [Access(AccessLevel.Any)]
        public ActionResult Index(int? count)
        {
            //GetUserModel();

            if (count == null)
                return RedirectToAction("Index", "Organization");

            ViewBag.Count = count;
            return View();
        }

        [Access(AccessLevel.UserOrganization)]
        public ActionResult SetCard()
        {
            _PermissionsAccessor.Permitted(GetUser(), x => x.EditCompanyPayment(GetUser().Organization.Id));

            return View();
        }

        [Access(AccessLevel.UserOrganization)]
        [HttpPost]
        public async Task<ActionResult> SetCard(bool submit)
        {
            await _PaymentAccessor.SetCard(
                GetUser(),
                GetUser().Organization.Id,
                Request.Form["id"],
                Request.Form["class"],
                Request.Form["card_type"],
                Request.Form["card_owner_name"],
                Request.Form["last_4"],
                Request.Form["card_exp_month"].ToInt(),
                Request.Form["card_exp_year"].ToInt(),
                Request.Form["address_1"],
                Request.Form["address_2"],
                Request.Form["city"],
                Request.Form["state"],
                Request.Form["zip"],
                Request.Form["phone"],
                Request.Form["website"],
                Request.Form["country"],
                true);

            return RedirectToAction("Advanced", "Manage");
        }

    }
}