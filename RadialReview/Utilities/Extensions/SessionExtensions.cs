using System.Linq.Expressions;
using NHibernate;
using NHibernate.Criterion;
using RadialReview.Utilities;
using RadialReview.Utilities.Extensions;
using RadialReview.Utilities.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using RadialReview.Reflection;
using NHibernate.Proxy;

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


	    public class BackRef<T>
		{
			public Expression<Func<T, object>> Reference;

			public Expression<Func<object, T>> BackReference;


			public static BackRef<T> From<TRef, TList>(Expression<Func<T, TList>> reference, Expression<Func<TRef, T>> backReference) where TList : IList<TRef>
		    {
			    return new BackRef<T>(){
				    Reference = reference.AddBox(),
				    BackReference = x=>backReference.Compile()((TRef)x),
			    };
		    }
	    }

        public static T GetFresh<T>(this ISession s, object id)
        {
            var proxy = s.Load<T>(id);
            s.Evict(proxy);
            return s.Get<T>(id);

        }


		public static void SaveWithBackReference<T>(this ISession session, T obj, params BackRef<T>[] backRefs)
	    {
		    var temps = new List<object>();

		    foreach (var b in backRefs){
			    temps.Add(obj.Get(b.Reference));
				obj.Set(b.Reference, null);
		    }
		    session.Save(obj);

		    var p = NHibernateHelper.GetPropertyAndColumnNames(session.SessionFactory, obj.GetType());


			for (var i = 0; i < backRefs.Length; i++){
				obj.Set(backRefs[i].Reference, temps[i]);
			}




			session.Update(obj);
	    }
    }
}

namespace RadialReview.SessionExtension {
    public static class SessionExtension {
        public static T Deproxy<T>(this T model) {
            if (model is INHibernateProxy) {
                var lazyInitialiser = ((INHibernateProxy)model).HibernateLazyInitializer;
                model = (T)lazyInitialiser.GetImplementation();
            }
            return model;
        }
    }
}