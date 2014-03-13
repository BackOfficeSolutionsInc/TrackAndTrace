using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace RadialReview.Utilities.Query
{
    public class DataInteraction : IUpdate,IQuery
    {
        private AbstractQuery QueryProvider;
        private AbstractUpdate UpdateProvider;

        public DataInteraction(AbstractQuery query, AbstractUpdate update)
        {
            QueryProvider = query;
            UpdateProvider = update;
        }

        public AbstractQuery GetQueryProvider()
        {
            return QueryProvider;
        }
        public AbstractUpdate GetUpdateProvider()
        {
            return UpdateProvider;
        }

        public List<T> All<T>() where T : class
        {
            return QueryProvider.All<T>();
        }

        public List<T> Where<T>(Expression<Func<T, bool>> pred) where T : class
        {
            return QueryProvider.Where(pred);
        }

        public T SingleOrDefault<T>(Expression<Func<T, bool>> pred) where T : class
        {
            return QueryProvider.SingleOrDefault(pred);
        }

        public T Get<T>(long id) where T : ILongIdentifiable
        {
            return QueryProvider.Get<T>(id);
        }

        public T Get<T>(string id) where T : IStringIdentifiable
        {
            return QueryProvider.Get<T>(id);
        }

        public T Get<T>(Guid id) where T : IGuidIdentifiable
        {
            return QueryProvider.Get<T>(id);
        }

        public ITransaction BeginTransaction()
        {
            return QueryProvider.BeginTransaction();
        }
        
        public void Save(object obj)
        {
            UpdateProvider.Save(obj);
        }

        public void SaveOrUpdate(object obj)
        {
            UpdateProvider.SaveOrUpdate(obj);
        }

        public void Update(object obj)
        {
            UpdateProvider.Update(obj);
        }
    }
}