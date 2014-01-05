using NHibernate;
using RadialReview.Models.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;

namespace RadialReview.Utilities.Query
{
    public interface IQuery
    {
        List<T> Where<T>(Expression<Func<T, bool>> pred) where T : class;

        T SingleOrDefault<T>(Expression<Func<T, bool>> pred) where T : class;

        T Get<T>(long id) where T : ILongIdentifiable;
        T Get<T>(String id) where T : IStringIdentifiable;
        T Get<T>(Guid id) where T : IGuidIdentifiable;
        ITransaction BeginTransaction();
    }


    public abstract class AbstractQuery : IQuery
    {
        protected bool OnlyAlive;

        public abstract List<T> Where<T>(Expression<Func<T, bool>> pred) where T : class;

        public abstract T SingleOrDefault<T>(Expression<Func<T, bool>> pred) where T : class;

        public abstract T Get<T>(long id) where T : ILongIdentifiable;
        public abstract T Get<T>(String id) where T : IStringIdentifiable;
        public abstract T Get<T>(Guid id) where T : IGuidIdentifiable;
        public abstract ITransaction BeginTransaction();

    }
}