using NHibernate;
using RadialReview.Exceptions;
using RadialReview.Models;
using RadialReview.Models.Enums;
using RadialReview.Models.Responsibilities;
using RadialReview.Utilities;
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

        private static string[] ApplicationCategories = new string[]{ 
                "Performance",
                "Culture",
                "Feedback",
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
            new Q(QuestionType.Feedback,"Feedback","Feedback",false),
            new Q(QuestionType.Thumbs,"Gets it","Feedback",true),
            new Q(QuestionType.Thumbs,"Wants it","Feedback",true),
            new Q(QuestionType.Thumbs,"Capacity to do it","Feedback",true),
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

        public static IEnumerable<QuestionModel> GetApplicationQuestions(AbstractQuery session)
        {
            return session.Where<QuestionModel>(x => x.OriginId == APPLICATION_ID && x.OriginType == OriginType.Application).ToList();
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
    }
}