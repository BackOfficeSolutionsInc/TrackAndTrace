using NHibernate;
using NHibernate.Engine;
using NHibernate.Stat;
using NHibernate.Type;
using RadialReview.Models.Synchronize;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using RadialReview.Models;

namespace RadialReview.Utilities.NHibernate {

    public interface IOrderedSession : ISession {

    }

    public class OrderedSession : IOrderedSession {
        private ISession _backingSession;
        public bool FromSyncLock { get; set; }

        [Obsolete("only to be used in SyncUtil. Did you forget to wrap your request in an SyncUtil.EnsureStrictlyAfter")]
        private OrderedSession(ISession session,SyncLock _) {
            _backingSession = session;
            FromSyncLock = _ != null;
        }

        public static IOrderedSession Indifferent(ISession s) {
            return new OrderedSession(s, null);
        }
        public static IOrderedSession From(ISession s, SyncLock lck) {
            return new OrderedSession(s, lck);
        }

        public EntityMode ActiveEntityMode { get { return _backingSession.ActiveEntityMode; } }

        public CacheMode CacheMode { get { return _backingSession.CacheMode; } set { _backingSession.CacheMode = value; } }

        public IDbConnection Connection {
            get {
                return _backingSession.Connection;
            }
        }

        public bool DefaultReadOnly {
            get {
                return _backingSession.DefaultReadOnly;
            }

            set {
                _backingSession.DefaultReadOnly = value;
            }
        }

        public FlushMode FlushMode {
            get {
                return _backingSession.FlushMode;
            }

            set {
                _backingSession.FlushMode = value;
            }
        }

        public bool IsConnected {
            get {
                return _backingSession.IsConnected;
            }
        }

        public bool IsOpen {
            get {
                return _backingSession.IsOpen;
            }
        }

        public ISessionFactory SessionFactory {
            get {
                return _backingSession.SessionFactory;
            }
        }

        public ISessionStatistics Statistics {
            get {
                return _backingSession.Statistics;
            }
        }

        public ITransaction Transaction {
            get {
                return _backingSession.Transaction;
            }
        }

        public ITransaction BeginTransaction() {
            return _backingSession.BeginTransaction();
        }

        public ITransaction BeginTransaction(IsolationLevel isolationLevel) {
            return _backingSession.BeginTransaction(isolationLevel);
        }

        public void CancelQuery() {
            _backingSession.CancelQuery();
        }

        public void Clear() {
            _backingSession.Clear();
        }

        public IDbConnection Close() {
            return _backingSession.Close();
        }

        public bool Contains(object obj) {
            return _backingSession.Contains(obj);
        }

        public ICriteria CreateCriteria(string entityName) {
            return _backingSession.CreateCriteria(entityName);
        }

        public ICriteria CreateCriteria(Type persistentClass) {
            return _backingSession.CreateCriteria(persistentClass);
        }

        public ICriteria CreateCriteria(string entityName, string alias) {
            return _backingSession.CreateCriteria(entityName, alias);
        }

        public ICriteria CreateCriteria(Type persistentClass, string alias) {
            return _backingSession.CreateCriteria(persistentClass, alias);
        }

        public ICriteria CreateCriteria<T>() where T : class {
            return _backingSession.CreateCriteria<T>();
        }

        public ICriteria CreateCriteria<T>(string alias) where T : class {
            return _backingSession.CreateCriteria<T>(alias);
        }

        public IQuery CreateFilter(object collection, string queryString) {
            return _backingSession.CreateFilter(collection, queryString);
        }

        public IMultiCriteria CreateMultiCriteria() {
            return _backingSession.CreateMultiCriteria();
        }

        public IMultiQuery CreateMultiQuery() {
            return _backingSession.CreateMultiQuery();
        }

        public IQuery CreateQuery(string queryString) {
            return _backingSession.CreateQuery(queryString);
        }

        public ISQLQuery CreateSQLQuery(string queryString) {
            return _backingSession.CreateSQLQuery(queryString);
        }

        public int Delete(string query) {
            return _backingSession.Delete(query);
        }

        public void Delete(object obj) {
            _backingSession.Delete(obj);
        }

        public void Delete(string entityName, object obj) {
            _backingSession.Delete(entityName, obj);
        }

        public int Delete(string query, object[] values, IType[] types) {
            return _backingSession.Delete(query, values, types);
        }

        public int Delete(string query, object value, IType type) {
            return _backingSession.Delete(query, value, type);
        }

        public void DisableFilter(string filterName) {
            _backingSession.DisableFilter(filterName);
        }

        public IDbConnection Disconnect() {
            return _backingSession.Disconnect();
        }

        public void Dispose() {
            _backingSession.Dispose();
        }

        public IFilter EnableFilter(string filterName) {
            return _backingSession.EnableFilter(filterName);
        }

        public void Evict(object obj) {
            _backingSession.Evict(obj);
        }

        public void Flush() {
            _backingSession.Flush();
        }

        public object Get(string entityName, object id) {
            return _backingSession.Get(entityName, id);
        }

        public object Get(Type clazz, object id) {
            return _backingSession.Get(clazz, id);
        }

        public object Get(Type clazz, object id, LockMode lockMode) {
            return _backingSession.Get(clazz, id, lockMode);
        }

        public T Get<T>(object id) {
            return _backingSession.Get<T>(id);
        }

        public T Get<T>(object id, LockMode lockMode) {
            return _backingSession.Get<T>(id, lockMode);
        }

        public LockMode GetCurrentLockMode(object obj) {
            return _backingSession.GetCurrentLockMode(obj);
        }

        public IFilter GetEnabledFilter(string filterName) {
            return _backingSession.GetEnabledFilter(filterName);
        }

        public string GetEntityName(object obj) {
            return _backingSession.GetEntityName(obj);
        }

        public object GetIdentifier(object obj) {
            return _backingSession.GetIdentifier(obj);
        }

        public IQuery GetNamedQuery(string queryName) {
            return _backingSession.GetNamedQuery(queryName);
        }

        public ISession GetSession(EntityMode entityMode) {
            return _backingSession.GetSession(entityMode);
        }

        public ISessionImplementor GetSessionImplementation() {
            return _backingSession.GetSessionImplementation();
        }

        public bool IsDirty() {
            return _backingSession.IsDirty();
        }

        public bool IsReadOnly(object entityOrProxy) {
            return _backingSession.IsReadOnly(entityOrProxy);
        }

        public object Load(string entityName, object id) {
            return _backingSession.Load(entityName, id);
        }

        public void Load(object obj, object id) {
            _backingSession.Load(obj, id);
        }

        public object Load(Type theType, object id) {
            return _backingSession.Load(theType, id);
        }

        public object Load(string entityName, object id, LockMode lockMode) {
            return _backingSession.Load(entityName, id, lockMode);
        }

        public object Load(Type theType, object id, LockMode lockMode) {
            return _backingSession.Load(theType, id, lockMode);
        }

        public T Load<T>(object id) {
            return _backingSession.Load<T>(id);
        }

        public T Load<T>(object id, LockMode lockMode) {
            return _backingSession.Load<T>(id, lockMode);
        }

        public void Lock(object obj, LockMode lockMode) {
            _backingSession.Lock(obj, lockMode);
        }

        public void Lock(string entityName, object obj, LockMode lockMode) {
            _backingSession.Lock(entityName, obj, lockMode);
        }

        public object Merge(object obj) {
            return _backingSession.Merge(obj);
        }

        public object Merge(string entityName, object obj) {
            return _backingSession.Merge(entityName, obj);
        }

        public T Merge<T>(T entity) where T : class {
            return _backingSession.Merge<T>(entity);
        }

        public T Merge<T>(string entityName, T entity) where T : class {
            return _backingSession.Merge<T>(entityName, entity);
        }

        public void Persist(object obj) {
            _backingSession.Persist(obj);
        }

        public void Persist(string entityName, object obj) {
            _backingSession.Persist(entityName, obj);
        }

        public IQueryOver<T, T> QueryOver<T>() where T : class {
            return _backingSession.QueryOver<T>();
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName) where T : class {
            return _backingSession.QueryOver<T>(entityName);
        }

        public IQueryOver<T, T> QueryOver<T>(Expression<Func<T>> alias) where T : class {
            return _backingSession.QueryOver<T>(alias);
        }

        public IQueryOver<T, T> QueryOver<T>(string entityName, Expression<Func<T>> alias) where T : class {
            return _backingSession.QueryOver<T>(entityName, alias);
        }

        public void Reconnect() {
            _backingSession.Reconnect();
        }

        public void Reconnect(IDbConnection connection) {
            _backingSession.Reconnect(connection);
        }

        public void Refresh(object obj) {
            _backingSession.Refresh(obj);
        }

        public void Refresh(object obj, LockMode lockMode) {
            _backingSession.Refresh(obj, lockMode);
        }

        public void Replicate(object obj, ReplicationMode replicationMode) {
            _backingSession.Replicate(obj, replicationMode);
        }

        public void Replicate(string entityName, object obj, ReplicationMode replicationMode) {
            _backingSession.Replicate(entityName, obj, replicationMode);
        }

        public object Save(object obj) {
            return _backingSession.Save(obj);
        }

        public object Save(string entityName, object obj) {
            return _backingSession.Save(entityName, obj);
        }

        public void Save(object obj, object id) {
            _backingSession.Save(obj, id);
        }

        public void Save(string entityName, object obj, object id) {
            _backingSession.Save(entityName, obj, id);
        }
        
        public void SaveOrUpdate(object obj) {
            _backingSession.SaveOrUpdate(obj);
        }

        public void SaveOrUpdate(string entityName, object obj) {
            _backingSession.SaveOrUpdate(entityName, obj);
        }

        public void SaveOrUpdate(string entityName, object obj, object id) {
            _backingSession.SaveOrUpdate(entityName, obj, id);
        }

        public ISession SetBatchSize(int batchSize) {
            return _backingSession.SetBatchSize(batchSize);
        }

        public void SetReadOnly(object entityOrProxy, bool readOnly) {
            _backingSession.SetReadOnly(entityOrProxy, readOnly);
        }

        public void Update(object obj) {
            _backingSession.Update(obj);
        }

        public void Update(string entityName, object obj) {
            _backingSession.Update(entityName, obj);
        }

        public void Update(object obj, object id) {
            _backingSession.Update(obj, id);
        }

        public void Update(string entityName, object obj, object id) {
            _backingSession.Update(entityName, obj, id);
        }
    }
}
