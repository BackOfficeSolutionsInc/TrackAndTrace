using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RadialReview
{
    public static class SessionExtensions
    {
        public static IList<TEntity> GetByMultipleIds<TEntity>(this ISession Session, IEnumerable<long> ids)
        {
            var result = Session
              .CreateCriteria(typeof(TEntity))
              .Add(Restrictions.In("Id", ids.ToArray()))
              .List<TEntity>();
            return result;
        }
    }
}