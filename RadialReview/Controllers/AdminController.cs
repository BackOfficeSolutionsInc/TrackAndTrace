using RadialReview.Accessors;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RadialReview.Controllers
{
    public partial class AccountController : BaseController
    {
        private static OrganizationAccessor _OrganizationAccessor = new OrganizationAccessor();
        private static PaymentAccessor _PaymentAccessor = new PaymentAccessor();
        private static PositionAccessor _PositoinAccessor = new PositionAccessor();
        private static ReviewAccessor _ReviewAccessor = new ReviewAccessor();

        /*
        [Access(AccessLevel.Radial)]
        public async Task<String> TempCreate()
        {
            var lines = Regex.Replace(Data.people,"\r","").Split('\n');// = System.IO.File.ReadAllLines(file);
            //var accountController = new AccountController();
            //Column Ids
            var FIRST_NAME      =0;
            var LAST_NAME       =1;
            var POSITION        =2;
            var EMAIL           =3;
            var MANAGER_EMAIL   =4;
            var IS_MANAGER      =5;
            //Other variables
            var ORGANIZATION    ="Cornerstone";

            String firstEmail = null;

            var members = lines.Skip(1).Select(x=>{ 
                var split= x.Split(',');
                return new {
                        Email       = split[EMAIL],
                        FirstName   = split[FIRST_NAME],
                        LastName    = split[LAST_NAME],
                        Password    = "`123qwer",
                        Position    = split[POSITION],
                        ManagerEmail= split[MANAGER_EMAIL],
                        IsManager   = split[IS_MANAGER],
                        Created     = false
                    };
            });

            //Create org
            var orgCreatorData=members.First(x=>String.IsNullOrWhiteSpace(x.ManagerEmail));
            /*await accountController.Register(new RegisterViewModel(){
                Email=orgCreatorData.Email,
                fname=orgCreatorData.FirstName,
                lname=orgCreatorData.LastName,
                Password=orgCreatorData.Password,
            });*


            foreach(var m in members)
            {
                await Register(new RegisterViewModel(){
                    Email=m.Email,
                    fname=m.FirstName,
                    lname=m.LastName,
                    Password=m.Password,
                });

            }


            var orgCreator = _UserAccessor.GetUserByEmail(orgCreatorData.Email);
            var basicPlan=_PaymentAccessor.BasicPaymentPlan();
            var org=_OrganizationAccessor.CreateOrganization(orgCreator,new LocalizedStringModel(ORGANIZATION),true,basicPlan);


            var orgCreatorUO = _UserAccessor.GetUserByEmail(orgCreatorData.Email).UserOrganization.First();

            var posDict = new Dictionary<String,long>();

            foreach(var position in members.UnionBy(x=>x.Position).Select(x=>x.Position))
            {                
                posDict[position]=_OrganizationAccessor.EditOrganizationPosition(orgCreatorUO,0,org.Id,_PositoinAccessor.AllPositions().FirstOrDefault().Id,position).Id;
            }

            var errors =true;
            var notCreated = members.Where(x=>!x.Created && !String.IsNullOrWhiteSpace(x.ManagerEmail)).ToList();
            while(errors)
            {
                errors=false;
                foreach (var m in notCreated.ToList())
                {
                    try
                    {
                        var manager = _UserAccessor.GetUserByEmail(m.ManagerEmail).UserOrganization.First();
                        var tempUser=_NexusAccessor.CreateUserUnderManager(orgCreatorUO, manager.Id, bool.Parse(m.IsManager), posDict[m.Position], "clay.upton@gmail.com", m.FirstName, m.LastName);
                        var nexus = _NexusAccessor.Get(tempUser.Guid);
                        //[organizationId,EmailAddress,userOrgId,Firstname,Lastname]
                        _OrganizationAccessor.JoinOrganization(_UserAccessor.GetUserByEmail(m.Email), manager.Id, long.Parse(nexus.GetArgs()[2]));
                        notCreated.Remove(m);
                    }catch{
                        errors=true;
                    }
                }
            }

            return "Success";
        }*/


        private RadialReview.Controllers.ReviewController.ReviewDetailsViewModel GetReviewDetails(ReviewModel review)
        {
            var categories = _OrganizationAccessor.GetOrganizationCategories(GetUser(), GetUser().Organization.Id);
            var answers = _ReviewAccessor.GetAnswersForUserReview(GetUser(), review.ForUserId, review.ForReviewsId);
            var model = new RadialReview.Controllers.ReviewController.ReviewDetailsViewModel()
            {
                Review = review,
                Axis = categories.ToSelectList(x => x.Category.Translate(), x => x.Id),
                xAxis = categories.FirstOrDefault().NotNull(x => x.Id),
                yAxis = categories.Skip(1).FirstOrDefault().NotNull(x => x.Id),
                AnswersAbout = answers,
                Categories = categories.ToDictionary(x => x.Id, x => x.Category.Translate()),
            };
            return model;
        }



        [Access(AccessLevel.Any)]
        public ActionResult TestChart(long id,long reviewsId)
        {
            var review = _ReviewAccessor.GetReview(GetUser(), id);

            var model = GetReviewDetails(review);
            return View(model);
        }

	}
}