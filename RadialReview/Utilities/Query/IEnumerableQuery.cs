﻿using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Linq.Expressions;

namespace RadialReview.Utilities.Query
{
    public class IEnumerableQuery : AbstractQuery
    {
        private Dictionary<Type, object> Data { get; set; }

        public IEnumerableQuery(bool onlyAlive=false)
        {
            Data = new Dictionary<Type, object>();
            OnlyAlive = onlyAlive;
        }

        public void AddData<T>(IEnumerable<T> data)
        {
            var key=typeof(T);
            if (Data.ContainsKey(key))
                throw new InvalidOperationException("Already contains data for this type");
            Data[key] = data;
        }

        private IEnumerable<T> GetIEnumerable<T>()
        {
            var key =typeof(T);
            if (!Data.ContainsKey(key))
                throw new InvalidOperationException("Query provider doesn't contain: "+key);
            return ((IEnumerable<T>)Data[key]);
        }
        
        public override List<T> Where<T>(System.Linq.Expressions.Expression<Func<T, bool>> pred)
        {
            Func<T, bool> func = pred.Compile();
            var query=GetIEnumerable<T>().Where(func);
            if (OnlyAlive)
                query= query.Alive();
            return query.ToList();
        }

        public override T SingleOrDefault<T>(System.Linq.Expressions.Expression<Func<T, bool>> pred)
        {
            Func<T, bool> func = pred.Compile();
            return GetIEnumerable<T>().SingleOrDefault(func);
        }

        public override T Get<T>(long id)
        {
            return GetIEnumerable<T>().SingleOrDefault(x => x.Id == id);
        }

        public override T Get<T>(string id)
        {
            return GetIEnumerable<T>().SingleOrDefault(x => x.Id == id);
        }

        public override T Get<T>(Guid id)
        {
            return GetIEnumerable<T>().SingleOrDefault(x => x.Id == id);
        }

        public override ITransaction BeginTransaction()
        {
            return new IEnumerableTransaction();
        }


        public override List<T> All<T>()
        {
            return GetIEnumerable<T>().ToList();
        }

		public override T Load<T>(long id)
		{
			return Get<T>(id);
		}

		public override T Load<T>(string id)
		{
			return Get<T>(id);
		}

		public override T Load<T>(Guid id)
		{
			return Get<T>(id);
		}

		public override List<T> WhereRestrictionOn<T>(Expression<Func<T, bool>> pred, Expression<Func<T, object>> selector, IEnumerable<object> isIn) {
			Func<T, object> selectorC = selector.Compile();
			var enumer = GetIEnumerable<T>();
			if (pred != null)
				enumer = enumer.Where(pred.Compile());

			var arr = isIn.ToArray();

			return enumer.Where(x => {
				var transform = selectorC(x);
				return arr.Any(y => y.Equals(transform));
			}).ToList();
		}

		public override bool Contains<T>() {
			return Data.ContainsKey(typeof(T));
		}
	}
}