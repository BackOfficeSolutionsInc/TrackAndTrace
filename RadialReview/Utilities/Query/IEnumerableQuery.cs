using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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

    }
}