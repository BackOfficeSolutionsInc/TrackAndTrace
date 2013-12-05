using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using RadialReview.Accessors;
using RadialReview.Models.Enums;
using RadialReview.Models.UserModels;
using RadialReview.Exceptions;

namespace RadialReview
{
    public static partial class UserOrganizationExtensions
    {
        public static UserHydration Hydrate(this UserOrganizationModel user)
        {
            return UserHydration.Hydrate(user);
        }
    }
}

namespace RadialReview
{
    public class UserHydration
    {
        private static UserAccessor _UserAccessor { get; set; }
        private UserOrganizationModel User { get; set; }
        private UserOrganizationModel _UnderlyingUser { get; set; }
        private ISession Session { get; set; }


        private UserHydration()
        {
        }

        public static UserHydration Hydrate(UserOrganizationModel user)
        {
            return new UserHydration()
            {
                User = user,
                Session = HibernateSession.GetCurrentSession()
            };
        }

        private UserOrganizationModel GetUnderlying()
        {
            if (_UnderlyingUser == null)
            {
                using (var tx = Session.BeginTransaction())
                {
                    _UnderlyingUser = Session.Get<UserOrganizationModel>(User.Id);
                }
            }
            return _UnderlyingUser;
        }

        public UserHydration ManagingGroups(Boolean questions = false)
        {
            List<GroupModel> groups = new List<GroupModel>();
            List<QuestionModel> questionsResolved = null;
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.ManagingGroups)
                {
                    var group = Session.Query<GroupModel>().Where(x => x.Id == g.Id).FetchMany(x => x.GroupUsers).FetchMany(x => x.CustomQuestions).SingleOrDefault();
                    if (questions)
                    {
                        questionsResolved = Session.QueryOver<QuestionModel>().Where(x => x.OriginType == OriginType.Group && x.OriginId == group.Id).List().ToList();
                        group.CustomQuestions = questionsResolved;
                        //orgQuery.Fetch(x => x.CustomQuestions).Eager.Future();
                        //orgQuery.Fetch(x => x.QuestionCategories).Eager.Future();
                    }

                    groups.Add(group);
                }
            }
            User.ManagingGroups = groups;
            return this;
        }

        public UserHydration Groups()
        {
            List<GroupModel> groups = new List<GroupModel>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.Groups)
                {
                    var group = Session.Query<GroupModel>().Where(x => x.Id == g.Id).FetchMany(x => x.GroupUsers).SingleOrDefault();
                    groups.Add(group);
                }
            }
            User.Groups = groups;
            return this;
        }
        
        public UserHydration ManagingUsers(Boolean subordinates = false)
        {
            List<ManagerDuration> managing = new List<ManagerDuration>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.ManagingUsers.ToListAlive())
                {
                    var user = Session.Query<ManagerDuration>().Where(x => x.Id == g.Id).SingleOrDefault();
                    managing.Add(user);
                }
            }
            if (subordinates)
            {
                using (var tx = Session.BeginTransaction())
                {
                    var user=GetUnderlying();
                    var children = Children(user, new List<String> { "" + user.Id });
                    User.AllSubordinates = children;
                }
            }
            User.ManagingUsers = managing.ToListAlive();
            return this;
        }


        private List<UserOrganizationModel> Children(UserOrganizationModel parent,List<String> parents)
        {
            var children = new List<UserOrganizationModel>();
            if (parent.ManagingUsers == null || parent.ManagingUsers.Count == 0)
                return children;
            foreach (var c in parent.ManagingUsers.ToListAlive().Select(x => x.Subordinate))
            {
                c.Properties["parents"] = parents;
                children.Add(c);
                var copy=parents.Select(x=>x).ToList();
                copy.Add(""+c.Id);
                children.AddRange(Children(c, copy));
            }
            return children;
        }


        public UserHydration Organization(Boolean questions = false, Boolean memebers = false/*, Boolean reviews = false*/)
        {
            OrganizationModel org = null;
            List<QuestionModel> questionsResolved = null;
            List<QuestionCategoryModel> categoriesResolved = null;
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();

                var orgQuery = Session.QueryOver<OrganizationModel>().Where(x => x.Id == uOrg.Organization.Id);
                if (questions)
                {
                    questionsResolved = Session.QueryOver<QuestionModel>()
                        .Where(x => x.OriginType == OriginType.Organization && x.OriginId == uOrg.Organization.Id)
                        .Fetch(x=>x.Question).Eager
                        .List()
                        .ToList();
                    //orgQuery.Fetch(x => x.CustomQuestions).Eager.Future();
                    var orgId = uOrg.Organization.Id;
                    categoriesResolved=Session.QueryOver<QuestionCategoryModel>()
                        .Where(x => x.OriginId == orgId && x.OriginType == OriginType.Organization)
                        .Fetch(x=>x.Category).Eager
                        .List().ToList();
                    //orgQuery
                   // orgQuery.Fetch(x => x.QuestionCategories).Eager.Future();
                }
                if (memebers)
                    orgQuery.Fetch(x => x.Members).Eager.Future();

                /*if (reviews)
                {
                    orgQuery.Fetch(x => x.Reviews).Eager.Future();

                }*/

                org = orgQuery.SingleOrDefault();
            }
            User.Organization = org;
            if (questionsResolved != null)
                User.Organization.CustomQuestions = questionsResolved;
            if (categoriesResolved != null)
                User.Organization.QuestionCategories = categoriesResolved;
            /*if(reviews)
            {
                using(var tx=Session.BeginTransaction())
                {
                    foreach(var r in org.Reviews)
                    {
                        r.Reviews=Session.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == r.Id).List().ToList();
                    }
                }
            }*/

            return this;
        }
        public UserHydration Nexus()
        {
            List<NexusModel> nexus = new List<NexusModel>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.CreatedNexuses)
                {
                    var nex = Session.Query<NexusModel>().Where(x => x.Id == g.Id).SingleOrDefault();
                    nexus.Add(nex);
                }
            }
            User.CreatedNexuses = nexus;
            return this;
        }

        public UserOrganizationModel Execute()
        {
            Session.Dispose();
            return User;
        }

        public UserHydration Reviews(bool answers=false)
        {
            List<ReviewsModel> reviews = new List<ReviewsModel>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();

                var uOrgId = uOrg.Id;

                var query = Session.QueryOver<ReviewsModel>().Where(x => x.CreatedById == uOrgId).List().ToList();
                reviews = query;
                foreach(var rs in reviews)
                {
                    var reviewList = Session.QueryOver<ReviewModel>().Where(x => x.ForReviewsId == rs.Id).List().ToList();
                    rs.Reviews = reviewList;
                    if (answers)
                    {
                        foreach (var r in reviewList)
                        {
                            var ans = Session.QueryOver<AnswerModel>().Where(x => x.ForReviewId == r.Id).List().ToList();
                            r.Answers = ans;
                        }
                    }
                }

                /*foreach (var g in uOrg.CreatedReviews)
                {
                    var nex = Session.Query<ReviewsModel>().Where(x => x.Id == g.Id).SingleOrDefault();
                    reviews.Add(nex);
                }*/
            }
            User.CreatedReviews = reviews;
            return this;
        }
        public UserHydration SetTeams(List<TeamDurationModel> teams)
        {
            /*using(var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                User.Teams = uOrg.Teams.ToList();
            }    */
            User.Teams = teams;
            return this;
        }

        public UserHydration  Position()
        {
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                User.Positions = uOrg.Positions.ToList();
            }
            return this;
        }

        public UserHydration PersonallyManaging(UserOrganizationModel self)
        {
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                var uOrgId = uOrg.Id;
                bool owned = false;
                //Blah blah blah this is bad.. 
                try
                {
                    PermissionsUtility.Create(Session, self).OwnedBelowOrEqual(x => x.Id == uOrgId);
                    owned = true;
                }
                catch(PermissionsException e)
                {
                    owned = false;
                }

                User.SetPersonallyManaging(owned);
            }
            return this;
        }
    }
}