using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;

namespace RadialReview.Utilities.Query
{
    public class SessionQuery : AbstractQuery
    {
        private ISession Session;

        public SessionQuery(ISession session, bool onlyAlive = false)
        {
            Session = session;
            OnlyAlive = onlyAlive;
        }

        public override List<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> pred)
        {
            var query=Session.QueryOver<T>().Where(pred).List();
            if(OnlyAlive)
                return query.Alive().ToList();
            return query.ToList();
        }

        public override T SingleOrDefault<T>(System.Linq.Expressions.Expression<Func<T, bool>> pred)
        {
            return Session.QueryOver<T>().Where(pred).SingleOrDefault();
        }

        public override T Get<T>(long id)
        {
            return Session.Get<T>(id);
        }

        public override T Get<T>(string id)
        {
            return Session.Get<T>(id);
        }

        public override T Get<T>(Guid id)
        {
            return Session.Get<T>(id);
        }

        public override ITransaction BeginTransaction()
        {
            return Session.BeginTransaction();
        }

        public override List<T> All<T>()
        {
            return Session.QueryOver<T>().List().ToList();
        }

		public override T Load<T>(long id)
		{
			return Session.Load<T>(id);
		}

		public override T Load<T>(string id)
		{
			return Session.Load<T>(id);
		}

		public override T Load<T>(Guid id)
		{
			return Session.Load<T>(id);
		}

		public override List<T> WhereRestrictionOn<T>(Expression<Func<T, bool>> pred,Expression<Func<T, object>> selector, IEnumerable<object> isIn) {

			var q = Session.QueryOver<T>();

			if (pred != null)
				q = q.Where(pred);
			return q.WhereRestrictionOn(selector).IsIn(isIn.ToArray()).List().ToList();

		}

		public override bool Contains<T>() {
			return true;// Probably should check this...
		}
	}
}