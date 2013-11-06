using NHibernate;
using RadialReview.Models;
using RadialReview.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NHibernate.Linq;
using RadialReview.Accessors;

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
        private ISession Session { get;set; }
        

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
            if(_UnderlyingUser==null)
            {
                using (var tx = Session.BeginTransaction())
                {
                    _UnderlyingUser=Session.Get<UserOrganizationModel>(User.Id);
                }
            }
            return _UnderlyingUser;
        }

        public UserHydration ManagingGroups()
        {
            List<GroupModel> groups = new List<GroupModel>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.ManagingGroups)
                {
                    var group = Session.Query<GroupModel>().Where(x => x.Id == g.Id).FetchMany(x => x.GroupUsers).SingleOrDefault();
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

        public UserHydration ManagingUsers()
        {
            List<UserOrganizationModel> managing = new List<UserOrganizationModel>();
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();
                foreach (var g in uOrg.ManagingUsers)
                {
                    var user = Session.Query<UserOrganizationModel>().Where(x => x.Id == g.Id).Fetch(x => x.User).SingleOrDefault();
                    managing.Add(user);
                }
            }
            User.ManagingUsers = managing;
            return this;
        }
        public UserHydration Organization(Boolean questions=false,Boolean memebers=false)
        {
            OrganizationModel org = null;
            List<QuestionModel> questionsResolved = null;
            using (var tx = Session.BeginTransaction())
            {
                var uOrg = GetUnderlying();

                var orgQuery=Session.QueryOver<OrganizationModel>().Where(x=>x.Id==uOrg.Organization.Id);
                if (questions)
                {
                    questionsResolved=Session.QueryOver<QuestionModel>().Where(x => x.ForOrganization.Id == uOrg.Organization.Id).List().ToList();
                    //orgQuery.Fetch(x => x.CustomQuestions).Eager.Future();
                    orgQuery.Fetch(x => x.QuestionCategories).Eager.Future();
                }
                if (memebers)
                    orgQuery.Fetch(x => x.Members).Eager.Future();
              
                org = orgQuery.SingleOrDefault();
            }
            User.Organization = org;
            if (questionsResolved != null)
                User.Organization.CustomQuestions = questionsResolved;
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


        
    }
}