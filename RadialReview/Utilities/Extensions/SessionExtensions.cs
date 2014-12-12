using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class _SessionExtensions
    {
        public static IList<TEntity> GetByMultipleIds<TEntity>(this ISession Session, IEnumerable<long> ids)
        {
            var result = Session
              .CreateCriteria(typeof(TEntity))
              .Add(Restrictions.In("Id", ids.ToArray()))
              .List<TEntity>();
            return result;
        }

        public static AbstractQuery ToQueryProvider(this ISession session, bool onlyAlive)
        {
            return new SessionQuery(session, onlyAlive);
        }
        public static AbstractUpdate ToUpdateProvider(this ISession session)
        {
            return new SessionUpdate(session);
        }
        public static DataInteraction ToDataInteraction(this ISession session, bool onlyAlive)
        {
            return new DataInteraction(session.ToQueryProvider(onlyAlive),session.ToUpdateProvider());
        }
    }
}