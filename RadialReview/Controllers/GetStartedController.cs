using RadialReview.Accessors;
using RadialReview.Hooks;
using RadialReview.Models;
using RadialReview.Models.Accountability;
using RadialReview.Models.Enums;
using RadialReview.Models.Json;
using RadialReview.NHibernate;
using RadialReview.Utilities;
using RadialReview.Utilities.Hooks;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RadialReview.Controllers {
    public class GetStartedController : UserManagementController {


        //public GetStartedController() : this(new NHibernateUserManager(new NHibernateUserStore())) //this(new UserManager<ApplicationUser>(new NHibernateUserStore<UserModel>(new ApplicationDbContext())))
        //{
        //}

        //public GetStartedController(NHibernateUserManager userManager)
        //{
        //    UserManager = userManager;
        //}

        // GET: Onboard
        [Access(AccessLevel.SignedOut)]
        public ActionResult Index(string id = "professional")
        {
            var type = id;
            var u = OnboardingAccessor.GetOrCreate(this, "TheBasics");
            OnboardingAccessor.Update(this, x => { x.PaymentPlan = type; });
            return View(u);
            //var u = OnboardingAccessor.GetOrCreate(this);
            //return RedirectToAction(u.CurrentPage);
        }

        [Access(AccessLevel.SignedOut)]
        public ActionResult StartOver()
        {
            OnboardingAccessor.Update(this, x => { x.DeleteTime = DateTime.UtcNow; },true);
            return RedirectToAction("Index");
        }
        
        [Access(AccessLevel.Any)]
        public JsonResult Data()
        {
            var pages = new[] { "Personal", "Organization", "Login", "Payment" };
            var sd =new Dictionary<string,object>[pages.Length];
            for (var i = 0; i < pages.Length; i++) {
                sd[i] = new Dictionary<string, object>();
                sd[i]["step"] = i + 1;
                sd[i]["disabled"] = false;
                sd[i]["optional"] = false;
                sd[i]["data"] = new Dictionary<string,object>();
                ((Dictionary<string,object>)sd[i]["data"])["page"] = pages[i];
            }
            var o = OnboardingAccessor.GetOrCreate(this);
            ((Dictionary<string,object>)sd[0]["data"])["fname"] = o.FirstName;
            ((Dictionary<string,object>)sd[0]["data"])["lname"] = o.LastName;
            ((Dictionary<string,object>)sd[0]["data"])["title"] = o.Position;
            ((Dictionary<string,object>)sd[0]["data"])["phone"] = o.Phone;

            ((Dictionary<string,object>)sd[1]["data"])["orgname"] = o.CompanyName;
            ((Dictionary<string,object>)sd[1]["data"])["eosduration"] = o.EosStartedAgo;
            ((Dictionary<string,object>)sd[1]["data"])["implementer"] = o.ImplementerName;
            ((Dictionary<string,object>)sd[1]["data"])["website"] = o.Website;

            ((Dictionary<string, object>)sd[2]["data"])["email"]     = o.Email;
            ((Dictionary<string, object>)sd[2]["data"])["password"]  = o.UserId != null ? "******" : null;
            ((Dictionary<string, object>)sd[2]["data"])["locked"]    = o.UserId != null;
            ((Dictionary<string,object>)sd[2]["data"])["profileUrl"] = o.ProfilePicture;
                                                     
            ((Dictionary<string,object>)sd[3]["data"])["address_1"] = o.Address_1;
            ((Dictionary<string,object>)sd[3]["data"])["address_2"] = o.Address_2;
            ((Dictionary<string,object>)sd[3]["data"])["city"] = o.City;
            ((Dictionary<string,object>)sd[3]["data"])["state"] = o.State;
            ((Dictionary<string,object>)sd[3]["data"])["zip"] = o.Zip;
            ((Dictionary<string,object>)sd[3]["data"])["country"] = o.Country;

           
            return Json(sd,JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Any)]
        [HttpPost]
        public JsonResult Personal(string fname = null, string lname = null, string title = null, string phone = null)
        {
            var o =OnboardingAccessor.Update(this, x => {
                x.ContactCompleteTime = DateTime.UtcNow;
                x.FirstName = fname ?? x.FirstName;
                x.LastName = lname ?? x.LastName;
                x.Position = title;
                x.Phone = phone;
				x.CurrentPage = "Personal";
            });

            OnboardingAccessor.TryUpdateUser(o);


            return Json(ResultObject.SilentSuccess());
        }


        [Access(AccessLevel.Any)]
        [HttpPost]
        public JsonResult Organization(string orgname = null, double? eosduration = null, string implementer = null, string website = null)
        {
            var o = OnboardingAccessor.Update(this, x => {
                x.OrganizationCompleteTime = DateTime.UtcNow;
                x.CompanyName = orgname ?? x.CompanyName;
                if (eosduration != null) {
                    if (eosduration >= 0) {
                        var eos = DateTime.UtcNow.AddDays(eosduration.Value * -30);
                        x.EosStartTime = eos;
                        x.EosStartedAgo = eosduration.Value;
                    } else {
                        x.EosStartTime = null;
                        x.EosStartedAgo = null;
                    }
                }
                x.ImplementerName = implementer;
                x.Website = website;
				x.CurrentPage = "Organization";
			});


            OnboardingAccessor.TryUpdateOrganizatoin(o);

            return Json(ResultObject.SilentSuccess());
        }


        [Access(AccessLevel.Any)]
        [HttpPost]
        public async Task<JsonResult> Login(string email, string password, HttpPostedFileBase file = null)
        {
            //THIS IS IN THE GETTING STARTED CONTROLLER (not the AccountController)
            var o = OnboardingAccessor.GetOrCreate(this);
            UserModel user;
            if (o.UserId == null) {
                user = await UserManager.FindAsync(email.ToLower(), password);
                if (user == null) {
                    user = new UserModel() { UserName = email, FirstName = o.FirstName, LastName = o.LastName };
                    var result = await UserAccessor.CreateUser(UserManager, user, password);

                    if (result.Errors.Any())
                        return Json(ResultObject.CreateError(string.Join(" ", result.Errors)));
                    //await SignInAsync(user,true);
                } else {
                    //await SignInAsync(user, true);
                }
            } else {
                user = o._User;
            }
            
            try {
                string imageUrl = null;

                if (file != null) {
                    imageUrl = await (new ImageAccessor()).UploadImage(user, Server, file, UploadType.ProfileImage);
                }

                var now = DateTime.UtcNow;
				UserOrganizationModel uOrg = null;
				AccountabilityNode uNode = null;
				OrganizationModel organization=null;
                if (o.OrganizationId == null) {
                    var paymentPlanType = PaymentAccessor.GetPlanType(o.PaymentPlan ?? "professional");
                    organization = _OrganizationAccessor.CreateOrganization(user, o.CompanyName, paymentPlanType, now, out uOrg,out uNode, true, false,startDeactivated:true);

                }

                OnboardingAccessor.Update(this, x => {
                    x.Email = email ?? x.Email;
                    x.CreateOrganizationTime = now;
                    x.ProfilePicture = imageUrl ?? x.ProfilePicture;
                    if (organization!=null)
                        x.OrganizationId = organization.Id;
                    if (uOrg != null)
                        x.UserId = uOrg.Id;
					x.CurrentPage = "Login";
				});


			} catch (Exception) {
                //probably should try to delete the user here.
                throw;
            }

            return Json(ResultObject.SilentSuccess());
        }

        [Access(AccessLevel.Any)]
        [HttpPost]
        public async Task<JsonResult> Payment()//string address_1 = null, string address_2 = null, string city = null, string state = null, string zip = null, string country = null)
        {
            var o=OnboardingAccessor.Update(this, x => {
                x.CreditCardCompleteTime = DateTime.UtcNow;
                x.Address_1 = Request.Form["address_1"] ?? x.Address_1;
                x.Address_2 = Request.Form["address_2"];
                x.City = Request.Form["city"] ?? x.City;
                x.State = Request.Form["state"] ?? x.State;
                x.Zip = Request.Form["zip"] ?? x.Zip;
                x.Country = Request.Form["country"] ?? x.Country;
				x.CurrentPage = "Payment";
			});
            
            if (o._UserOrg == null)
                throw new Exception("User was not created. Could not complete setup.");
            if (o.OrganizationId == null)
                throw new Exception("Organization was not created. Could not complete setup.");

            await PaymentAccessor.SetCard(
                o._UserOrg,
                o.OrganizationId.Value,
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
                Request.Form["email"],
                true);

            

            var user = OnboardingAccessor.TryActivateOrganization(o);

            if (user!=null)
                await SignInAsync(user, true);

            return Json(ResultObject.SilentSuccess());
        }

      //  protected NHibernateUserManager UserManager { get; set; }
    }
}