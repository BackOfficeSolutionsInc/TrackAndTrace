using FluentNHibernate.Utils;
using Microsoft.AspNet.SignalR;
using NHibernate.Hql.Ast.ANTLR;
using RadialReview.Accessors;
using RadialReview.Hubs;
using RadialReview.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using RadialReview.Models.Askables;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Reviews;
using RadialReview.Models.Tasks;
using RadialReview.Models.Application;
using RadialReview.Utilities;
using RadialReview.Utilities.Query;
using RadialReview.Models.UserModels;
using WebGrease.Css.Extensions;

namespace RadialReview.Controllers
{
    public partial class AccountController : BaseController
    {
        [Access(AccessLevel.Radial)]
        public String TempDeep(long id)
        {
	        var now = DateTime.UtcNow;
            var count = _UserAccessor.CreateDeepSubordinateTree(GetUser(), id,now);

	        var o = "TempDeep - " + now.Ticks + " - " + count;
			log.Info(o);
			return o;
        }

        /*[Access(AccessLevel.Radial)]
        public String FixManagerGroups()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {

                    tx.Commit();
                    s.Flush();
                }
            }
        }*/


	    [Access(AccessLevel.Radial)]
	    public int FixTeams()
	    {
		    var count = 0;
		    using (var s = HibernateSession.GetCurrentSession()){
			    using (var tx = s.BeginTransaction()){
				    var teams = s.QueryOver<OrganizationTeamModel>().List();
				    foreach (var t in teams){
					    if (t.Type == TeamType.Subordinates && t.DeleteTime ==null){
						    var mid = t.ManagedBy;
						    var m= s.Get<UserOrganizationModel>(mid);
						    if (m.DeleteTime != null ){
							    t.DeleteTime = m.DeleteTime;
								s.Update(t);
							    count++;
						    }
					    }
				    }
					tx.Commit();
					s.Flush();
			    }
		    }
		    return count;

	    }

	    [Access(AccessLevel.Radial)]
	    public string UndoRandomReview(long id)
	    {
		    var count = 0;
		    using (var s = HibernateSession.GetCurrentSession()){
			    using (var tx = s.BeginTransaction()){
					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime==DateTime.MinValue).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();
					var feedbacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id && x.Complete && x.CompleteTime == DateTime.MinValue).List().ToList();

					foreach (var v in values){
						v.Exhibits = PositiveNegativeNeutral.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in roles){
						v.GetIt = FiveState.Indeterminate;
						v.WantIt = FiveState.Indeterminate;
						v.HasCapacity = FiveState.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v); 
						count+=3;
					}
					foreach (var v in rocks)
					{
						v.Finished = Tristate.Indeterminate;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					foreach (var v in feedbacks)
					{
						v.Feedback = null;
						v.Complete = false;
						v.CompleteTime = null;
						s.Update(v);
						count++;
					}
					tx.Commit();
					s.Flush();

			    }
		    }
		    return "Undo Random Review. Update: " + count;

	    }


	    [Access(AccessLevel.Radial)]
		public string RandomReview(long id)
	    {
		    var count = 0;
		    using (var s = HibernateSession.GetCurrentSession()){
			    using (var tx = s.BeginTransaction()){

				    var desenterPercent = .1;
				    var standardPercent = .8;
				    var standardPercentDeviation = .2;
					
				    var unstartedPercent = .05;
				    var incompletePercent = .1;

				    var rockPercent = .88;

					var values = s.QueryOver<CompanyValueAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var roles = s.QueryOver<GetWantCapacityAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var rocks = s.QueryOver<RockAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();
					var feebacks = s.QueryOver<FeedbackAnswer>().Where(x => x.ForReviewContainerId == id).List().ToList();

					var about2 = new HashSet<long>(values.Select(x => x.AboutUserId));
					roles.Select(x => x.AboutUserId).ForEach(x=>about2.Add(x));
					rocks.Select(x => x.AboutUserId).ForEach(x=>about2.Add(x));

				    var reviewIds = new HashSet<long>();

				    var about = about2.ToList();

				    var r =new Random();
				    var unstartedList = new List<long>();
				    var incompleteList = new List<long>();
					
				    for (var i = 0; i <= unstartedPercent*about.Count; i++)
					    unstartedList.Add(about[r.Next(about.Count)]);
				    for (var i = 0; i <= incompletePercent*about.Count; i++)
					    incompleteList.Add(about[r.Next(about.Count)]);


					var lookup = about.ToDictionary(x => x, x =>
					{
						//BadEgg
						var luckA = 0.3;
						var luckB = 0.3;

					    if (r.NextDouble() > desenterPercent){//Standard
						    luckA = Math.Max(Math.Min((r.NextDouble() - .5)*standardPercentDeviation + standardPercent, 1), 0);
					    }
						if (r.NextDouble() > desenterPercent){//Standard
							luckB = Math.Max(Math.Min((r.NextDouble() - .5) * standardPercentDeviation + standardPercent, 1), 0);
					    }

					    return new {luckA,luckB};
				    });
					
				    foreach (var v in values){
					    var a = lookup[v.AboutUserId];
					    if (!v.Complete){
							if (unstartedList.Contains(v.ByUserId))
							    continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;
						    count++;
							v.CompleteTime = DateTime.MinValue;
						    v.Complete = true;
						    if (r.NextDouble() < a.luckA){
								v.Exhibits = PositiveNegativeNeutral.Positive;
						    }else{
							    if (r.NextDouble() < a.luckA/2){
								    v.Exhibits = PositiveNegativeNeutral.Negative;
							    }else{
									v.Exhibits = PositiveNegativeNeutral.Neutral;
							    }
						    }
							s.Update(v);
					    }
				    }

					foreach (var v in roles)
					{
						var a = lookup[v.AboutUserId];
						if (!v.Complete)
						{
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;

							count += 3;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							if (r.NextDouble() < a.luckB)
							{
								v.GetIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.WantIt = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
								v.HasCapacity = (r.NextDouble() > .1) ? FiveState.Always : FiveState.Mostly;
							}
							else
							{
								v.GetIt = (r.NextDouble() > .25) ? FiveState.Rarely : FiveState.Never;
								v.WantIt = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
								v.HasCapacity = (r.NextDouble() > 25) ? FiveState.Rarely : FiveState.Never;
							}
							s.Update(v);
						}
					}

					foreach (var v in rocks){
						var a = lookup[v.AboutUserId];
						if (!v.Complete)
						{
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;

							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							v.Finished = (r.NextDouble() < rockPercent) ? Tristate.True : Tristate.False;
							s.Update(v);
						}
					}

				    var allFeedbacks = new[]{"No comment.", "Good progress.", "Could use some work","Excellent","A pleasure to work with"};

					foreach (var v in feebacks)
					{
						var a = lookup[v.AboutUserId];
						if (!v.Complete)
						{
							if (unstartedList.Contains(v.ByUserId))
								continue;
							if (incompleteList.Contains(v.ByUserId) && r.NextDouble() > .5)
								continue;
							if (!v.Required)
								continue;
							count++;
							v.CompleteTime = DateTime.MinValue;
							v.Complete = true;
							
							v.Feedback = (r.NextDouble() < .05) ? allFeedbacks[r.Next(allFeedbacks.Length)] : "";
							s.Update(v);
						}
					}

					tx.Commit();
					s.Flush();
			    }
		    }
		    return "Completed Randomize. Updated:"+count;
	    }


	    [Access(AccessLevel.Radial)]
	    public JsonResult AdminAllUsers(string search)
	    {
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var users = s.QueryOver<UserOrganizationModel>()
						.Where(x => x.DeleteTime == null)
						.WhereRestrictionOn(c => c.EmailAtOrganization).IsLike("%" + search + "%")
						.Select(x=>x.EmailAtOrganization, x=>x.Id)
						.List<object[]>().ToList();
					return Json(new{
						results = users.Select(x =>new {
							text=""+x[0],
							value= ""+ x[1]
						}).ToArray()
					}, JsonRequestBehavior.AllowGet);
				}
			}
	    }


	    [Access(AccessLevel.Radial)]
        public String FixScatterChart(bool delete=false)
        {
            var i = 0;
            using (var s = HibernateSession.GetCurrentSession()){
                using (var tx = s.BeginTransaction()){
                    var scatters = s.QueryOver<ClientReviewModel>().List();
                    foreach (var sc in scatters){
                        if (sc.ScatterChart == null || delete){
                            i++;
                            sc.ScatterChart = new LongTuple();
                            if (sc.Charts.Any())
                            {
                                sc.ScatterChart.Filters = sc.Charts.First().Filters;
                                sc.ScatterChart.Groups = sc.Charts.First().Groups;
                                sc.ScatterChart.Title = sc.Charts.First().Title;
                            }
                            s.Update(sc);
                        }
                    } 
                    tx.Commit();
                    s.Flush();
                }
            }

            return ""+i;
        }


        [Access(AccessLevel.Radial)]
        public String FixAnswers(long id)
        {
            var reviewContainerId = id;
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviewContainer = s.Get<ReviewsModel>(id);
                    var orgId=reviewContainer.ForOrganizationId;


                    var answers = s.QueryOver<AnswerModel>().Where(x => x.ForReviewContainerId == id).List().ToList();
                    var perms = PermissionsUtility.Create(s,GetUser());

                    int i = 0;

                    var dataInteraction=ReviewAccessor.GetReviewDataInteraction(s,orgId);
                    var qp = dataInteraction.GetQueryProvider();

                    foreach(var a in answers)
                    {
                        var relationship=RelationshipAccessor.GetRelationships(perms, qp, a.ByUserId, a.AboutUserId).First();
                        if (relationship == Models.Enums.AboutType.NoRelationship){
                            int b = 0;
                        }


                        if (relationship != a.AboutType)
                        {
                            a.AboutType = relationship;
                            s.Update(a);
                            i++;
                        }
                    }


                    tx.Commit();
                    s.Flush();
                    return ""+i;
                }
            }
        }


        [Access(AccessLevel.Radial)]
        public async Task<JsonResult> Emails(int id)
        {
            var emails = Enumerable.Range(0, id).Select(x => MailModel.To("clay.upton@gmail.com").Subject("TestBulk").Body("Email #{0}", "" + x));
            var result = (await Emailer.SendEmails(emails));
            result.Errors = null;

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [Access(AccessLevel.Radial)]
        public JsonResult FixReviewData()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                using (var tx = s.BeginTransaction())
                {
                    var reviews = s.QueryOver<ReviewModel>().List().ToList();
                    var allAnswers = s.QueryOver<AnswerModel>().List().ToList();
                    
                    foreach (var r in reviews)
                    {
                        var update = false;
                        if (r.DurationMinutes == null && r.Complete)
                        {
                            var ans = allAnswers.Where(x => x.ForReviewId == r.Id).ToList();
                            r.DurationMinutes = (decimal?)TimingUtility.ReviewDurationMinutes(ans, TimingUtility.ExcludeLongerThan);
                            update = true;
                        }

                        if (r.Started == false)
                        {
                            var started = allAnswers.Any(x => x.ForReviewId == r.Id && x.Complete);
                            r.Started = started;
                            update = true;
                        }
                        if (update)
                        {
                            s.Update(r);
                        }
                    }

                    tx.Commit();
                    s.Flush();
                }
            }

            return Json(true, JsonRequestBehavior.AllowGet);
        }

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
        public bool TestTask(long id)
        {
            var fire = DateTime.UtcNow.AddSeconds(id);
            _TaskAccessor.AddTask(new ScheduledTask() { Fire = fire, Url = "/Account/TestTaskRecieve" });
            log.Debug("TestTaskRecieve scheduled for: " + fire.ToString());
            return true;
        }

        [AllowAnonymous]
        [Access(AccessLevel.Any)]
        public bool TestTaskRecieve()
        {
            log.Debug("TestTaskRecieve hit: " + DateTime.UtcNow.ToString());
            return true;
        }

        [Access(AccessLevel.Any)]
        public ActionResult TestChart(long id, long reviewsId)
        {
            var review = _ReviewAccessor.GetReview(GetUser(), id);

            var model = GetReviewDetails(review);
            return View(model);
        }

	    [Access(AccessLevel.Radial)]
	    public String SendMessage(long id, string message)
	    {
			var hub = GlobalHost.ConnectionManager.GetHubContext<MeetingHub>();
			hub.Clients.Group(MeetingHub.GenerateMeetingGroupId(id)).status(message);
		    return "Sent: " + message;
	    }

		[Access(AccessLevel.Radial)]
		public String UpdateCache(long id)
		{
			using (var s = HibernateSession.GetCurrentSession())
			{
				using (var tx = s.BeginTransaction()){
					var org =s.Get<UserOrganizationModel>(id).UpdateCache(s);
					tx.Commit();
					s.Flush();
					return "Updated: " + org.GetName();
				}
			}
		}


        [Access(AccessLevel.Radial)]
        public async Task<JsonResult> ChargeToken(long id,decimal amt)
        {
            return Json(await _PaymentAccessor.ChargeOrganizationAmount(id, amt, true),JsonRequestBehavior.AllowGet);
        }

		[Access(AccessLevel.Radial)]
		public async Task<JsonResult> ClearCache()
		{
			var urlToRemove = Url.Action("UserScorecard", "TileData");
			HttpResponse.RemoveOutputCacheItem(urlToRemove);
			return Json("cleared", JsonRequestBehavior.AllowGet);
		}


    }
}