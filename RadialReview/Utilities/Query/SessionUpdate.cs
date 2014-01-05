using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview.Utilities.Query
{
    public class SessionUpdate : AbstractUpdate
    {
        protected ISession Session {get;set;}
        public SessionUpdate(ISession session)
        {
            Session = session;
        }

        public override void Save(object obj)
        {
            Session.Save(obj);
        }

        public override void SaveOrUpdate(object obj)
        {
            Session.SaveOrUpdate(obj);
        }

        public override void Update(object obj)
        {
            Session.Update(obj);
        }
    }
}