using NHibernate;
using NHibernate.Linq;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Askables;
using RadialReview.Models.Dashboard;
using RadialReview.Models.Enums;
using RadialReview.Models.L10;
using RadialReview.Models.Log;
using RadialReview.Models.Responsibilities;
using RadialReview.Models.Tasks;
using RadialReview.Models.UserModels;
using RadialReview.Utilities;
using RadialReview.Utilities.Productivity;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace RadialReview.Accessors
{
    public class ApplicationAccessor : BaseAccessor
    {
        public const long APPLICATION_ID = 1;

		public const string FEEDBACK = "Feedback";
		public const string THUMBS = "Yes/No";
		//public const string GWC = "Get It/Want It/Capacity To Do It";
		//public const string COMPANY_VALUES = "Company Values";
		//public const string ROCKS = "Rocks";
		public const string EVALUATION = "Evaluation";
		public const string ROLES = "Roles";
		public const string COMPANY_VALUES = "Company Values";
		public const string COMPANY_QUESTION = "Company Questions";

        private static string[] ApplicationCategories ={ 
                "Performance",
                "Culture",
                FEEDBACK,
                THUMBS,
				//GWC,
				//COMPANY_VALUES,
				//ROCKS,
				EVALUATION,
				ROLES,
				COMPANY_VALUES,
				COMPANY_QUESTION
        };

        private class Q
        {
            public String Question;
            public QuestionType Type;
            public String Category;
            public bool Required;
            public Q(QuestionType type,String question,String category,bool required)
            {
                Question = question;
                Type = type;
                Category = category;
                Required = required;
            }
        }

        private static Q[] ApplicationQuestions = new Q[]{
            //new Q(QuestionType.Feedback,"Feedback","Feedback",false),
            //new Q(QuestionType.Thumbs,"Gets it","Feedback",true),
            //new Q(QuestionType.Thumbs,"Wants it","Feedback",true),
            //new Q(QuestionType.Thumbs,"Capacity to do it","Feedback",true),
        };


        private static string[] ApplicationPositions = new String[]{
                "Account Coordinator",
                "Account Manager",
                "Accountant",
                "Art Director",
                "Business Development",
                "CEO",
                "CFO",
                "Client Retention",
                "Content Marketing Strategist",
                "Content Writer",
                "COO",
                "Copywriter",
                "Creative Director",
                "Cross Media Programmer",
                "Cross Media Strategist",
                "Data Developer",
                "Database Administrator",
                "Delivery",
                "Developer",
                "Direct Sales",
                "Director",
                "Executive Assistant",
                "Executive Director",
                "Facilities",
                "Finance",
                "Graphic Designer",
                "Help Desk Technician",
                "Human Resources",
                "Information Technology",
                "Inside Sales",
                "Intern",
                "Mailing Services",
                "Manager",
                "Marketing ",
                "Marketing Coordinator",
                "Marketing Publications Writer",
                "Multimedia Strategist",
                "Online Marketing Strategist",
                "Operator",
                "President",
                "Production Manager",
                "Project Manager",
                "Project Strategist",
                "Quality Assurance Engineer",
                "Receptionist",
                "Relationship Manager",
                "Sales",
                "Seminar Coordinator",
                "Shift Supervisor",
                "Shipping",
                "Signage",
                "Social Media Strategist",
                "Software Engineer",
                "Solutions Coordinator",
                "Strategist",
                "Supervisor",
                "Support",
                "System Administrator",
                "Team Lead",
                "UI/UX Developer",
                "VP of Operations",
                "VP of Sales and Marketing",
                "VP of Support Services",
                "VP of Technology",
                "Web Application Engineer",
                "Web Developer",
                "Digital Print",

            };

        public Boolean EnsureApplicationExists()
        {
            using (var s = HibernateSession.GetCurrentSession())
            {
                /*using (var tx = s.BeginTransaction())
                {
                    Temp(s);
                    tx.Commit();
                    s.Flush();
                }*/

                using (var tx = s.BeginTransaction())
                {
                    ConstructPositions(s);
                    tx.Commit();
                    s.Flush();
                }
                List<QuestionCategoryModel> applicationCategories;
                using (var tx = s.BeginTransaction())
                {
                    applicationCategories=ConstructApplicationCategories(s);
                    tx.Commit();
                    s.Flush();
                }
                using (var tx = s.BeginTransaction())
                {
                    ConstructApplicationQuestions(s, applicationCategories);
                    tx.Commit();
                    s.Flush();
                }

				using (var tx = s.BeginTransaction()){
					ConstructApplicationTasks(s);
					tx.Commit();
					s.Flush();

				}
				using (var tx = s.BeginTransaction())
				{
					ConstructPhoneNumbers(s);
					tx.Commit();
					s.Flush();

				}
				
                using (var tx = s.BeginTransaction())
                {
                    var application = s.Get<ApplicationWideModel>(APPLICATION_ID);
                    if (application == null)
                    {
                        s.Save(new ApplicationWideModel(APPLICATION_ID));
                        tx.Commit();
                        s.Flush();
                        return true;
                    }
                    return false;
                }
	           

            }
        }

		public const string ACCOUNT_AGE = "ACCOUNT_AGE";
		public const string DAILY_EMAIL_TODO_TASK = "DAILY_EMAIL_TODO_TASK";
		public const string DAILY_TASK = "DAILY_TASK";
		//public const string HOURLY_TASK = "DAILY_TASK";

		public void ConstructApplicationTasks(ISession s)
	    {
		    var found =s.QueryOver<ScheduledTask>().Where(x => x.DeleteTime == null && x.Executed == null && x.TaskName == DAILY_EMAIL_TODO_TASK).List().ToList();
		    for (var i = 0; i < 24; i++){
			    var url = "/Scheduler/EmailTodos?currentTime=" + i;
			    var count = found.Count(x => x.Url == url);
			    if (count == 0){
					var b = DateTime.UtcNow.Date.AddHours(i).AddMinutes(3);
				    var task = new ScheduledTask(){
					    MaxException = 1,
					    Url = url,
					    NextSchedule = TimeSpan.FromDays(1),
					    Fire = b,
					    FirstFire = b,
					    TaskName = DAILY_EMAIL_TODO_TASK,

				    };
				    s.Save(task);
				    task.OriginalTaskId = task.Id;
				    s.Update(task);
			    }else if (count > 1){
				    foreach (var f in found.Where(x => x.Url == url).Skip(1)){
					    f.Executed = DateTime.MinValue;
						s.Update(f);
				    }
			    }
		    }


		    var foundDaily = s.QueryOver<ScheduledTask>().Where(x => x.DeleteTime == null && x.Executed == null && x.TaskName == DAILY_TASK).List().ToList();
			if (!foundDaily.Any())
			{
					var b = DateTime.UtcNow.Date.AddMinutes(3);
					var task = new ScheduledTask()
					{
						MaxException = 1,
						Url = "/Scheduler/Daily",
						NextSchedule = TimeSpan.FromDays(1),
						Fire = b,
						FirstFire = b,
						TaskName = DAILY_TASK,

					};
					s.Save(task);
					task.OriginalTaskId = task.Id;
					s.Update(task);
			}
	    }

		public List<long> AllowedPhoneNumbers = new List<long>(){
			6467599497, 6467599498, 6467599499,
			6467603167, 6467603168, 6467603169,
            441234480162L, 441234480352L,
			441544430044L, 441244470511L,
			61427681100L, 61439187501L

		};
		public void ConstructPhoneNumbers(ISession s)
		{
			var found = s.QueryOver<CallablePhoneNumber>().Where(x => x.DeleteTime == null).List().ToList();
			var set = SetUtility.AddRemove(found.Select(x => x.Number), AllowedPhoneNumbers);
			var now = DateTime.UtcNow;
			foreach(var a in set.AddedValues){
				s.Save(new CallablePhoneNumber(){
					CreateTime = now,
					Number = a
				});
			}

			foreach (var a in set.RemovedValues){
				var b=found.First(x => x.Number == a);
				b.DeleteTime = now;
				s.Update(b);
			}
		}
	    /*
        private static void Temp(ISession session)
        {
            var all = session.QueryOver<LocalizedStringModel>().List();

            File.WriteAllLines(@"C:\Users\Clay\Desktop\tempDB\newTable.csv",all.Select(x => String.Join(",", x.Id, x.Default.Value, x.Default.Locale)));
            
            /*foreach (var a in all)
            {
                //a.Standard = a.Default.Value;
                //a.StandardLocale = a.Default.Locale;
                //session.Update(a);
            }*

        }*/

        private static void ConstructApplicationQuestions(ISession session,List<QuestionCategoryModel> applicationCategories)
        {
            var found = session.QueryOver<QuestionModel>().Where(x=>x.OriginType==OriginType.Application && x.OriginId == APPLICATION_ID).List().ToList();
            foreach (var appQ in ApplicationQuestions)
            {
                if (!found.Any(x => x.GetQuestion() == appQ.Question && x.GetQuestionType()==appQ.Type))
                {
                    var newQuestion = new QuestionModel()
                    {
                        Category= applicationCategories.First(x=>x.Category.Standard==appQ.Category),
                        Question = new LocalizedStringModel(appQ.Question),
                        QuestionType = appQ.Type,
                        Weight = WeightType.No,
                        OriginId = APPLICATION_ID,
                        OriginType = OriginType.Application,
                        CreatedById = -1,
                        DateCreated =DateTime.UtcNow,
                        Required = appQ.Required,
                    };
                    session.Save(newQuestion);
                }
            }
        }

        public static IEnumerable<QuestionModel> GetApplicationQuestions(AbstractQuery session){
            return session.Where<QuestionModel>(x => x.OriginId == APPLICATION_ID && x.OriginType == OriginType.Application).ToList().Where(x=>ApplicationQuestions.Any(y=>y.Question==x.GetQuestion() && y.Type == x.GetQuestionType() ));
        } 

        private static QuestionModel GetApplicationQuestion(AbstractQuery session,String question)
        {
            var found = ApplicationQuestions.FirstOrDefault(x => x.Question.ToLower() == question.ToLower());
            if (found == null)
                throw new PermissionsException("No application category for " + question);
            var foundQList = session.Where<QuestionModel>(x =>
                    x.OriginId == APPLICATION_ID &&
                    x.OriginType == OriginType.Application
                ).ToList();
            var foundQ = foundQList.Where(x => x.Question.Standard == question).FirstOrDefault();
            if (foundQ == null)
                throw new PermissionsException("Application was not initialized. Category was missing. " + question);
            return foundQ;
        }

        public static QuestionCategoryModel GetRockCategory(ISession s)
        {
            return ApplicationAccessor.GetApplicationCategory(s, ApplicationAccessor.EVALUATION);
        }

        public static QuestionCategoryModel GetApplicationCategory(ISession session, String category)
        {
            var found = ApplicationCategories.FirstOrDefault(x => x.ToLower() == category.ToLower());
            if (found == null)
                throw new PermissionsException("No application category for " + category);
            var foundCatList = session.QueryOver<QuestionCategoryModel>().Where(x =>
                    x.OriginId == APPLICATION_ID &&
                    x.OriginType == OriginType.Application
                ).List().ToList();
            var foundCat = foundCatList.Where(x=>x.Category.Standard == found).FirstOrDefault();
            if (foundCat == null)
                throw new PermissionsException("Application was not initialized. Category was missing. " + category);
            return foundCat;
        }

        public static LocalizedStringModel GetApplicationLocalizedStringModel(ISession session, String deflt)
        {
            var found = ApplicationPositions.Union(ApplicationCategories).FirstOrDefault(x => x.ToLower() == deflt.ToLower());
            if (found == null)
                throw new PermissionsException("No application localized string for " + deflt);


            var foundLSMList = session.QueryOver<LocalizedStringModel>().Where(x => x.Standard == deflt).List().ToList();
            var foundLSM = foundLSMList.OrderBy(x => x.Id).FirstOrDefault();
            if (foundLSM == null)
                throw new PermissionsException("Application was not initialized. LocalizedStringModel was missing. " + deflt);
            return foundLSM;
        }


        private List<QuestionCategoryModel> ConstructApplicationCategories(ISession session)
        {
            var found = session.QueryOver<QuestionCategoryModel>().List().ToList();

            var complete = found.ToList();

            foreach (var cat in ApplicationCategories)
            {
                if (!found.Any(x => x.Category.Standard == cat))
                {
                    var newCat = new QuestionCategoryModel()
                    {
                        Active = true,
                        OriginId = APPLICATION_ID,
                        OriginType = OriginType.Application,
                        Category = new LocalizedStringModel(cat)
                    };
                    complete.Add(newCat);
                    session.Save(newCat);
                }
            }
            return complete;
        }

        private void ConstructPositions(ISession session)
        {
            var found = session.QueryOver<PositionModel>().List().ToList();
            foreach (var p in ApplicationPositions)
            {
                if (!found.Any(x => x.Name.Standard == p))
                {
                    session.Save(new PositionModel() { Name = new LocalizedStringModel(p) });
                }
            }
        }

        public static List<QuestionCategoryModel> GetApplicationCategories(ISession session)
        {
            return session.QueryOver<QuestionCategoryModel>().Where(x => x.OriginId == APPLICATION_ID && x.OriginType == OriginType.Application).List().ToList();
        }


	    public class AppStat
	    {
		    public decimal? DemoClientsHaveLoggedInThisWeek { get; set; }
		    public decimal? DemoClientsHaventLoggedInThisWeek { get; set; }
		    public decimal? SupportHoursTotal_Demo { get; set; }
		    public decimal? SupportHoursTotal_Paying { get; set; }
		    public decimal? CustomFeatureDevHoursTotal { get; set; }
		    public decimal? AverageSupportHoursPerClientInLast90Days { get; set; }
		    public decimal? SupportMinsPerDemoClient { get; set; }
		    public decimal? NumberOfSupportCalls { get; set; }
		    public decimal? AverageSupportCallTime_min { get; set; }
            public decimal? MRR { get; set; }
            public decimal? ImplementerLogins { get; set; }
            public decimal? ProspectInTrial1 { get; set; }
            public decimal? ProspectInTrial2 { get; set; }
            public decimal? ProspectInTrial3 { get; set; }
            public decimal? ProspectInTrial4 { get; set; }

            public int NumberL10s { get; set; }
            public int NumberPayingClients { get; set; }

			public int NumberNewTrialsStarted { get; set; }
	    }

		public static AppStat Stats()
	    {
		    using (var s = HibernateSession.GetCurrentSession())
			{
			    using (var tx = s.BeginTransaction()){
				    var thisWeek = DateTime.UtcNow; //.StartOfWeek(DayOfWeek.Sunday);
				    var lastWeek = thisWeek.AddDays(-7);
				    var nintyDays = DateTime.UtcNow.AddDays(-90);


				    var orgs = s.QueryOver<OrganizationModel>()
					    .Where(x => x.DeleteTime == null)
					    .List().ToList();
				    var demoOrgs = orgs.Where(x => x.AccountType == AccountType.Demo).ToList();

				    //UserLookup userLookup = null;
				    var usersLoggedInThisWeek = s.QueryOver<UserLookup>().Where(x => x.LastLogin != null && x.LastLogin >= lastWeek && x.LastLogin < thisWeek && x.IsRadialAdmin == false).List().ToList();

                    /*var loggedInThisWeekOrgs = usersLoggedInThisWeek.GroupBy(x => x.OrganizationId)
                        .Select(x => x.Key).Distinct()
                        .Intersect(demoOrgs.Select(y => y.Id));*/

                    var loggedInThisWeek = usersLoggedInThisWeek.GroupBy(x => x.OrganizationId)
                        .Select(x => x.Key).Distinct()
                        .Intersect(demoOrgs.Select(y => y.Id))
					    .Count();

				    var haventLoggedInThisWeek = demoOrgs.Count - loggedInThisWeek;


				    var interactionsLastNintyDays = s.QueryOver<InteractionLogItem>().Where(x => x.DeleteTime == null && x.LogDate >= nintyDays).List().ToList();
				    var interactions = interactionsLastNintyDays.Where(x => x.LogDate >= lastWeek && x.LogDate < thisWeek).ToList();

				    var interactionUsers = interactionsLastNintyDays.Where(x => x.UserId != null).Select(x => x.UserId).Distinct().ToList();

				    var interaction_userId_OrgId = s.QueryOver<UserOrganizationModel>()
					    .WhereRestrictionOn(x => x.Id).IsIn(interactionUsers)
					    .Select(x => x.Id, x => x.Organization.Id)
					    .List<object[]>().Select(x => new{
						    userId = (long) x[0],
						    orgId = (long) x[1]
					    }).ToList();

				    /*var orgLookup = s.QueryOver<OrganizationModel>()
						.WhereRestrictionOn(x => x.Id).IsIn(userId_OrgId.Select(x=>x.orgId).Distinct().ToList())
						.List().ToDictionary(x=>x.Id,x=>x);*/

				    var orgLookup = orgs.ToDictionary(x => x.Id, x => x);
				    var orgLookupByUser = interaction_userId_OrgId.ToDictionary(x => x.userId, x => orgLookup[x.orgId]);


				    var supportInteractions = interactions.Where(x => InteractionUtility.IsSupport(x.InteractionType)).ToList();
				    var devInteractions = interactions.Where(x => InteractionUtility.IsDev(x.InteractionType)).ToList();


				    var supportHoursTotal_Demo = supportInteractions.Where(x => x.UserId != null && InteractionUtility.IsDemo(orgLookupByUser[x.UserId.Value].AccountType))
					    .Select(x => x.Duration).Sum()/60.0m;

				    var customFeatureDevHoursTotal = devInteractions.Select(x => x.Duration).Sum()/60m;

#pragma warning disable CS0618 // Type or member is obsolete
					var MRR = PaymentAccessor.CalculateTotalCharge(s, PaymentAccessor.GetPayingOrganizations(s));
#pragma warning restore CS0618 // Type or member is obsolete


					var implementerLogins = usersLoggedInThisWeek.Where(x => orgLookup.ContainsKey(x.OrganizationId) &&  orgLookup[x.OrganizationId].AccountType == AccountType.Implementer).GroupBy(x=>x.OrganizationId).Count();
				    var numberOfDemoClients = demoOrgs.Count();


				    //var interactionsLastNintyDays = s.QueryOver<InteractionLogItem>().Where(x => x.DeleteTime == null && x.LogDate >= nintyDays && x.UserId!=null).List().ToList();


				    decimal? averageSupportCallTime = null;
				    if (supportInteractions.Any())
					    averageSupportCallTime = supportInteractions.Select(x => x.Duration).Average();

				    var numberOfSupportCalls = supportInteractions.Count();


				    var support_InteractionsLastNintyDays = interactionsLastNintyDays
					    .Where(x => x.UserId != null && orgLookupByUser[x.UserId.Value].CreationTime >= nintyDays && InteractionUtility.IsSupport(x.InteractionType))
					    //.Where(x => orgLookupByUser[x.UserId.Value].AccountType == AccountType.Demo)
					    .ToList();

				    decimal? avgSupportHours_last90Days_perClient = null;
				    if (support_InteractionsLastNintyDays.Any()){
					    var count = support_InteractionsLastNintyDays.Select(x => orgLookupByUser[x.UserId.Value].Id).Distinct().Count();
					    if (count > 0){
						    avgSupportHours_last90Days_perClient = support_InteractionsLastNintyDays.Sum(x => x.Duration)/count/60m;
					    }
				    }

				    var supportHoursTotal_Paying = interactions.Where(x => x.AccountType == AccountType.Paying && InteractionUtility.IsSupport(x.InteractionType)).Sum(x => x.Duration)/60m;

				    /*
					
					Number of prospects in demo period 6
					Number of prospects in demo period 5
					Number of prospects in demo period 4
					Number of prospects in demo period 3
					Number of prospects in demo period 2
					Number of prospects in demo period 1
					*/

                    var prospectInTrial = new List<int>();
                    var today = DateTime.UtcNow;

                    for(var i =0;i<6;i++){
                        prospectInTrial.Add(demoOrgs
                            .Where(x => 
                                today.AddDays(-7*(i+1))<x.CreationTime && x.CreationTime <= today.AddDays(-7*i)
                            ).Count());
                    }
				    /*
					Ave support hours per clients in first 90 days
					support hours total -- paying
					Cash on hand
					support hours/demo client
					custom feature dev time
					number of support calls
					Avg support call time
					MRR
					Demo clients that HAVEN'T logged in that week
					Demo clients that logged in that week
					Implementer Logins	
					Support Hours Total --Demo
					
					*/
				    decimal? supportMin_per_demoClient = null;
				    if (demoOrgs.Any())
					    supportMin_per_demoClient = supportHoursTotal_Demo*60m/demoOrgs.Count();

                    var recentL10s = s.QueryOver<L10Meeting>().Where(x=>
                        x.DeleteTime==null &&
                        x.CreateTime>DateTime.UtcNow.AddDays(-7) &&
                        x.CreateTime<DateTime.UtcNow

                       // &&(x.CompleteTime==null || x.CompleteTime-x.StartTime>TimeSpan.FromMinutes(20))
                        ).List().ToList();
                    var numberL10s = recentL10s.Where(x => x.MeetingLeaderId != 600 && x.OrganizationId != 592 && (x.CompleteTime == null || x.CompleteTime - x.StartTime > TimeSpan.FromMinutes(20))).Count();

                    var payingClients = s.QueryOver<OrganizationModel>().Where(x =>x.DeleteTime == null && x.AccountType == AccountType.Paying).RowCount();

					var newTrials = demoOrgs.Where(x => today.AddDays(-7) < x.CreationTime && x.CreationTime <= today).Count();



					return new AppStat{
					    DemoClientsHaveLoggedInThisWeek = loggedInThisWeek,
					    DemoClientsHaventLoggedInThisWeek = haventLoggedInThisWeek,
					    SupportHoursTotal_Demo = supportHoursTotal_Demo,
					    SupportHoursTotal_Paying = supportHoursTotal_Paying,
					    CustomFeatureDevHoursTotal = customFeatureDevHoursTotal,

					    AverageSupportHoursPerClientInLast90Days = avgSupportHours_last90Days_perClient,

					    SupportMinsPerDemoClient = supportMin_per_demoClient,
                        
					    NumberOfSupportCalls = numberOfSupportCalls,
					    AverageSupportCallTime_min = averageSupportCallTime,
					    MRR = MRR,
					    ImplementerLogins = implementerLogins,

                        ProspectInTrial1 = prospectInTrial[0],
                        ProspectInTrial2 = prospectInTrial[1],
                        ProspectInTrial3 = prospectInTrial[2],
                        ProspectInTrial4 = prospectInTrial[3],
                        NumberL10s = numberL10s,
                        NumberPayingClients = payingClients,

						NumberNewTrialsStarted = newTrials,
				    };
			    }
		    }
		}
    }
}